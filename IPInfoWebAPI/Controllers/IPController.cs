using System;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using IPInfoWebAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;

namespace IPInfoWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IPController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private readonly MasterContext _dbContext;
        private readonly HttpClient _httpClient;

        public IPController(IMemoryCache cache, MasterContext dbContext, HttpClient httpClient)
        {
            _cache = cache;
            _dbContext = dbContext;
            _httpClient = httpClient;

            var cancellationToken = new CancellationToken();
            Task.Run(() => RunIPUpdateJob(cancellationToken), cancellationToken);
        }

        [HttpGet("{ipAddress}")]
        public async Task<IActionResult> GetIPInformation(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return BadRequest("IP Address is required.");
            }

            if (_cache.TryGetValue(ipAddress, out Ipaddress cachedIpInformation))
            {
                return Ok(cachedIpInformation);
            }

            var dbIPInformation = await _dbContext.Ipaddresses
                .Include(ip => ip.Country)
                .FirstOrDefaultAsync(ip => ip.Ip == ipAddress);

            if (dbIPInformation != null)
            {
                _cache.Set(ipAddress, dbIPInformation);

                var jsonSettings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                var serializedData = JsonConvert.SerializeObject(dbIPInformation, jsonSettings);

                return Content(serializedData, "application/json");
            }

            var ip2cUrl = $"https://ip2c.org/{ipAddress}";
            var ip2cResponse = await _httpClient.GetAsync(ip2cUrl);
            if (!ip2cResponse.IsSuccessStatusCode)
            {
                return StatusCode((int)ip2cResponse.StatusCode, "Failed to fetch IP information from the external service.");
            }

            var ip2cResult = await ip2cResponse.Content.ReadAsStringAsync();
            var ip2cData = ip2cResult.Split(';');

            if (ip2cData.Length < 4)
            {
                return BadRequest("Invalid IP information received from the external service.");
            }

            var countryName = ip2cData[3];
            var twoLetterCode = ip2cData[1];
            var threeLetterCode = ip2cData[2];

            if (string.IsNullOrEmpty(countryName))
            {
                return BadRequest("Invalid country name received from the external service.");
            }

            var country = _dbContext.Countries.FirstOrDefault(c => c.Name == countryName);
            if (country == null)
            {
                country = new Country { Name = countryName, TwoLetterCode = twoLetterCode, ThreeLetterCode = threeLetterCode };
                _dbContext.Countries.Add(country);
                await _dbContext.SaveChangesAsync();
            }

            //var newIPInformation = new Ipaddress();
            
            //if (country != null)
            //{
            var newIPInformation = new Ipaddress
                {
                    Ip = ipAddress,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    CountryId = country.Id,
                    //Country = countryName
                };
            //};
            
            _dbContext.Ipaddresses.Add(newIPInformation);

            await _dbContext.SaveChangesAsync();

            var dbCountry = await _dbContext.Countries.FindAsync(country.Id);
            newIPInformation.Country = dbCountry;

            _cache.Set(ipAddress, newIPInformation);

            return Ok(newIPInformation);
        }

        private async Task UpdateIPInformation()
        {
            var ipAddresses = await _dbContext.Ipaddresses
                .Include(ip => ip.Country)
                .OrderBy(ip => ip.Id)
                .Take(100)
                .ToListAsync();

            foreach (var ipAddress in ipAddresses)
            {
                var ip2cUrl = $"https://ip2c.org/{ipAddress.Ip}";
                var ip2cResponse = await _httpClient.GetAsync(ip2cUrl);
                if (ip2cResponse.IsSuccessStatusCode)
                {
                    var ip2cResult = await ip2cResponse.Content.ReadAsStringAsync();
                    var ip2cData = ip2cResult.Split(';');

                    if (ip2cData.Length >= 4)
                    {
                        var countryName = ip2cData[3];
                        var twoLetterCode = ip2cData[1];
                        var threeLetterCode = ip2cData[2];

                        if (!string.IsNullOrEmpty(countryName) &&
                            (countryName != ipAddress.Country.Name ||
                             twoLetterCode != ipAddress.Country.TwoLetterCode ||
                             threeLetterCode != ipAddress.Country.ThreeLetterCode))
                        {
                            ipAddress.Country.Name = countryName;
                            ipAddress.Country.TwoLetterCode = twoLetterCode;
                            ipAddress.Country.ThreeLetterCode = threeLetterCode;
                            ipAddress.Country.CreatedAt = DateTime.Now;

                            _cache.Remove(ipAddress.Ip);
                        }
                    }
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        private async Task RunIPUpdateJob(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UpdateIPInformation();

                await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
            }
        }

        [HttpPost("report")]
        public IActionResult GetReport([FromBody] string[] countryCodes)
        {
            string sqlQuery = @"
                SELECT
                    c.Name AS CountryName,
                    COUNT(ip.Ip) AS AddressCount,
                    MAX(ip.UpdatedAt) AS LastAddressUpdated
                FROM
                    Countries c
                    INNER JOIN IPAddresses ip ON c.Id = ip.CountryId";

            if (countryCodes != null && countryCodes.Length > 0)
            {
                string countryCodesString = string.Join(",", countryCodes.Select(code => $"'{code}'"));
                sqlQuery += $" WHERE c.TwoLetterCode IN ({countryCodesString})";
            }

            sqlQuery += @"
                GROUP BY
                    c.Name";

            List<IPReportItem> report = _dbContext.IPReportItems.FromSqlRaw(sqlQuery).ToList();

            return Ok(report);
        }
    }

}

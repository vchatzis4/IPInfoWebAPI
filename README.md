IPInfoWebAPI
IPInfoWebAPI is a RESTful web API that provides information about IP addresses based on country codes. It allows you to retrieve a report containing the number of IP addresses and the last updated date for each country. You can also get detailed information about a specific IP address.

Getting Started
These instructions will help you set up and run the IPInfoWebAPI project in Visual Studio.

Prerequisites
To run the project, make sure you have the following software installed:

Visual Studio (version XXXX or later)
.NET Core SDK (version X.X.X or later)
SQL Server (or another supported database provider)
Installation
Clone the repository or download the source code.

Open the solution file (IPInfoWebAPI.sln) in Visual Studio.

Configuration
Open the appsettings.json file.

Configure the database connection string under the ConnectionStrings section.

Save the file.

Database Setup
Open the Package Manager Console in Visual Studio (Tools > NuGet Package Manager > Package Manager Console).

Set the default project to the IPInfoWebAPI project.

Run the following command to apply the database migrations and create the necessary tables in the database:

mathematica
Copy code
Update-Database
Running the Application
Set the IPInfoWebAPI project as the startup project.

Press F5 or click on the "Start" button to run the application.

The API will be accessible at http://localhost:5000.

API Endpoints
Get IP Report
URL: /api/IP/report
Method: POST
Description: Retrieves a report containing IP address information for the specified country codes.
Request Body:
countryCodes (array of strings): An array of country codes for which to retrieve the report. Pass null to retrieve the report for all countries.
Response:
Status: 200 OK
Body: An array of objects representing the IP report. Each object contains the following properties:
countryName (string): The name of the country.
addressCount (integer): The number of IP addresses for the country.
lastAddressUpdated (string): The date and time when the IP addresses were last updated.
Get IP Address Details
URL: /api/IP/{ipAddress}
Method: GET
Description: Retrieves detailed information about a specific IP address.
URL Parameters:
ipAddress (string): The IP address to retrieve details for.
Response:
Status: 200 OK
Body: An object representing the IP address details. The object contains the following properties:
ipAddress (string): The IP address.
countryName (string): The name of the country where the IP address is located.
countryCode (string): The ISO two-letter country code.
regionName (string): The name of the region where the IP address is located.
cityName (string): The name of the city where the IP address is located.
zipCode (string): The postal code associated with the IP address.
latitude (decimal): The latitude coordinate of the IP address location.
longitude (decimal): The longitude coordinate of the IP address location.
Contributing
Contributions to the IPInfoWebAPI project are welcome. If you find any issues or have suggestions for improvements, please create a new issue or submit a pull request.

License
This project is licensed under the MIT License. Feel free to use, modify, and distribute the code as per the terms of the license.

Acknowledgments
ASP.NET Core
Entity Framework Core
Swashbuckle (Swagger integration)

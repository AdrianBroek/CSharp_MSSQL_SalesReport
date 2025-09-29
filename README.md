# C# + MS SQL Sales Report Generator

This is a demo project showing how to integrate **C# (.NET 8)** with **MS SQL Server** using the **AdventureWorks2022** sample database.  

The application:
- Connects to the database
- Queries customer sales for a specified date range
- Generates a summary report
- Inserts results into a `SalesReport` table
- Exports the report to a CSV file


Before running the project, create a table to store reports:

CREATE TABLE SalesReport (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BusinessEntityID INT NOT NULL,
    FirstName NVARCHAR(50),
    LastName NVARCHAR(50),
    TotalSpent DECIMAL(18,2),
    ReportDate DATETIME DEFAULT GETDATE()
);

The database connection string is stored in appsettings.json.
Replace YOUR_SERVER with your own SQL Server instance name before running the project.

Optionally, you can create an appsettings.Development.json file (ignored by Git) with your private connection string:

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_REAL_SERVER\\SQLEXPRESS;Database=AdventureWorks2022;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}

Enter start and end dates when prompted (yyyy-mm-dd).

The report will be:

Displayed in the console

Inserted into the SalesReport table

Saved to SalesReport.csv in the project folder

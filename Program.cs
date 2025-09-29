using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

class Program
{
    static void Main()
    {
        try
        {
            RunReport();
        }
        catch (SqlException ex)
        {
            Console.WriteLine("Database error: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unexpected error: " + ex.Message);
        }
    }
    static void RunReport()
    {
        // Load config from appsettings.json
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
            
        IConfiguration config = builder.Build();

        string connectionString = config.GetConnectionString("DefaultConnection");

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            // insert date range into console
            Console.Write("Enter start date (yyyy-mm-dd): ");
            string startInput = Console.ReadLine();
            DateTime startDate = DateTime.Parse(startInput);

            Console.Write("Enter end date (yyyy-mm-dd): ");
            string endInput = Console.ReadLine();
            DateTime endDate = DateTime.Parse(endInput);

            string Query = @"
                SELECT 
                    p.BusinessEntityID,
                    p.FirstName,
                    p.LastName,
                    SUM(soh.TotalDue) AS TotalSpent
                FROM Sales.SalesOrderHeader soh
                JOIN Sales.Customer c ON soh.CustomerID = c.CustomerID
                JOIN Person.Person p ON c.PersonID = p.BusinessEntityID
                WHERE soh.OrderDate BETWEEN @StartDate AND @EndDate
                GROUP BY p.BusinessEntityID, p.FirstName, p.LastName
                ORDER BY TotalSpent DESC;
                ";

            var results = new List<(int EntityId, string FirstName, string LastName, decimal TotalSpent)>();

            using (SqlCommand command = new SqlCommand(Query, connection))
            {
                command.Parameters.AddWithValue("@StartDate", startDate);
                command.Parameters.AddWithValue("@EndDate", endDate);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    Console.WriteLine($"{"ID",-6} {"Firstname",-12} {"Lastname",-15} {"Sales",15}");
                    Console.WriteLine(new string('-', 55));

                    while (reader.Read())
                    {
                        int entityId = reader.GetInt32(0);
                        string firstName = reader.GetString(1);
                        string lastName = reader.GetString(2);
                        decimal totalSpent = reader.GetDecimal(3);

                        Console.WriteLine($"{entityId,-6} {firstName,-12} {lastName,-15} {totalSpent,15:C}");
                        // adding result to the List
                        results.Add((entityId, firstName, lastName, totalSpent));
                    }
                } // sql reader closing
            }

            // clearing table SalesReport in db
            using (SqlCommand clearCmd = new SqlCommand("TRUNCATE TABLE SalesReport;", connection))
            {
                clearCmd.ExecuteNonQuery();
            }
            // inserting every row to the SalesReport table in db
            foreach (var r in results)
            {
                InsertRaport(connection, r.EntityId, r.FirstName, r.LastName, r.TotalSpent);
            }
            // Export results to CSV
            ExportToCSV(results);
        }

    }
    static void InsertRaport(SqlConnection connection, int entityId, string firstName, string lastName, decimal totalSpent)
    {
        string insertQuery = @"
            INSERT INTO SalesReport (BusinessEntityID, FirstName, LastName, TotalSpent)
            VALUES (@EntityId, @FirstName, @LastName, @TotalSpent);";

        using (SqlCommand insertCmd = new SqlCommand(insertQuery, connection))
        {
            insertCmd.Parameters.AddWithValue("@EntityId", entityId);
            insertCmd.Parameters.AddWithValue("@FirstName", firstName);
            insertCmd.Parameters.AddWithValue("@LastName", lastName);
            insertCmd.Parameters.AddWithValue("@TotalSpent", totalSpent);

            insertCmd.ExecuteNonQuery();
        }
    }
    static void ExportToCSV(List<(int EntityId, string FirstName, string LastName, decimal TotalSpent)> results)
    {
        string filePath = "SalesReport.csv";

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("EntityId, FirstName, LastName, TotalSpent");

            foreach (var r in results)
            {
                writer.WriteLine($"{r.EntityId},{r.FirstName},{r.LastName}, {r.TotalSpent}");
            }
        }
        Console.WriteLine($"Exported to {filePath}");
    }
}
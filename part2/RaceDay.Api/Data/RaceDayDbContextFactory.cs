
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RaceDay.Api.Data;

// Create the database context factory
public class RaceDayDbContextFactory : IDesignTimeDbContextFactory<RaceDayDbContext>
{
    // Set up the database connection
    public RaceDayDbContext CreateDbContext(string[] args)
    {
        // Connect to the SQL Server database
        var options=new DbContextOptionsBuilder<RaceDayDbContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=RaceDayDbPart2;Trusted_Connection=True;TrustServerCertificate=True").Options;
        return new RaceDayDbContext(options);
    }
}

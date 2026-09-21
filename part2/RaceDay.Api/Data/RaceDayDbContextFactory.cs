using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RaceDay.Api.Data;

public class RaceDayDbContextFactory : IDesignTimeDbContextFactory<RaceDayDbContext>
{
    public RaceDayDbContext CreateDbContext(string[] args)
    {
        var options=new DbContextOptionsBuilder<RaceDayDbContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=RaceDayDbPart2;Trusted_Connection=True;TrustServerCertificate=True").Options;
        return new RaceDayDbContext(options);
    }
}

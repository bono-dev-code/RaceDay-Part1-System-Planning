using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName=Guid.NewGuid().ToString();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services=>
        {
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();services.RemoveAll<RaceDayDbContext>();
            services.AddDbContext<RaceDayDbContext>(o=>o.UseInMemoryDatabase(_databaseName));
            using var provider=services.BuildServiceProvider();using var scope=provider.CreateScope();var db=scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();db.Database.EnsureCreated();Seed(db);
        });
    }
    private static void Seed(RaceDayDbContext db)
    {
        if(db.Users.Any())return;
        db.Users.AddRange(
            new User{UserID=1,RoleID=1,FirstName="Nenguda",LastName="Bono",Email="organiser@raceday.test",PasswordHash=BCrypt.Net.BCrypt.HashPassword("Password1!")},
            new User{UserID=2,RoleID=2,FirstName="Mbuyelo",LastName="Nkuna",Email="participant@raceday.test",PasswordHash=BCrypt.Net.BCrypt.HashPassword("Password1!")});
        db.SaveChanges();
    }
}

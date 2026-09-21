using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Role>(e => { e.ToTable("Role"); e.HasKey(x => x.RoleID); e.HasIndex(x => x.RoleName).IsUnique(); e.ToTable(t => t.HasCheckConstraint("CK_Role_RoleName", "[RoleName] IN ('Organiser','Participant')")); });
        b.Entity<User>(e => { e.ToTable("User"); e.HasKey(x => x.UserID); e.HasIndex(x => x.Email).IsUnique(); e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()"); e.HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleID).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<Event>(e => { e.ToTable("Event", t => { t.HasCheckConstraint("CK_Event_Distance", "[Distance] > 0"); t.HasCheckConstraint("CK_Event_EventType", "[EventType] IN ('Run','Walk','Cycle')"); }); e.HasKey(x => x.EventID); e.Property(x => x.Distance).HasPrecision(6,2); e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()"); e.HasOne(x => x.Organiser).WithMany(x => x.OrganisedEvents).HasForeignKey(x => x.OrganiserID).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<Category>(e => { e.ToTable("Category", t => t.HasCheckConstraint("CK_Category_CategoryType", "[CategoryType] IN ('Age','Distance')")); e.HasKey(x => x.CategoryID); e.HasIndex(x => new { x.EventID, x.CategoryName }).IsUnique(); e.HasOne(x => x.Event).WithMany(x => x.Categories).HasForeignKey(x => x.EventID).OnDelete(DeleteBehavior.Cascade); });
        b.Entity<Enrolment>(e => { e.ToTable("Enrolment", t => t.HasCheckConstraint("CK_Enrolment_Status", "[EnrolmentStatus] IN ('Pending','Confirmed','Cancelled')")); e.HasKey(x => x.EnrolmentID); e.HasIndex(x => new { x.ParticipantID, x.EventID }).IsUnique(); e.Property(x => x.EnrolmentDate).HasDefaultValueSql("SYSDATETIME()"); e.Property(x => x.EnrolmentStatus).HasDefaultValue("Pending"); e.HasOne(x => x.Participant).WithMany(x => x.Enrolments).HasForeignKey(x => x.ParticipantID).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.Event).WithMany(x => x.Enrolments).HasForeignKey(x => x.EventID).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.Category).WithMany(x => x.Enrolments).HasForeignKey(x => x.CategoryID).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<Result>(e => { e.ToTable("Result", t => { t.HasCheckConstraint("CK_Result_FinishTime", "[FinishTime] > '00:00:00'"); t.HasCheckConstraint("CK_Result_FinishingPosition", "[FinishingPosition] > 0"); }); e.HasKey(x => x.ResultID); e.HasIndex(x => x.EnrolmentID).IsUnique(); e.Property(x => x.PublishedAt).HasDefaultValueSql("SYSDATETIME()"); e.HasOne(x => x.Enrolment).WithOne(x => x.Result).HasForeignKey<Result>(x => x.EnrolmentID).OnDelete(DeleteBehavior.Cascade); });
        b.Entity<Role>().HasData(new Role { RoleID = 1, RoleName = "Organiser" }, new Role { RoleID = 2, RoleName = "Participant" });
    }
}

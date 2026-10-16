using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<RaceEvent> Events => Set<RaceEvent>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<RaceResult> Results => Set<RaceResult>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        // SQL functions/collations differ in the relational test provider; SQL Server is the application provider.
        var sqlServer = Database.IsSqlServer();
        var utcDefault = sqlServer ? "SYSUTCDATETIME()" : "CURRENT_TIMESTAMP";
        var users = model.Entity<User>();
        users.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "Role IN ('Organiser', 'Participant')"));
        users.HasKey(x => x.UserId);
        users.Property(x => x.FirstName).HasMaxLength(50).IsRequired();
        users.Property(x => x.LastName).HasMaxLength(50).IsRequired();
        users.Property(x => x.Email).HasMaxLength(255).IsRequired();
        users.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
        users.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        users.Property(x => x.Role).HasMaxLength(20).IsRequired();
        users.Property(x => x.Phone).HasMaxLength(30);
        users.Property(x => x.ProfilePictureUrl).HasMaxLength(500);
        users.Property(x => x.DateOfBirth).HasColumnType("date");
        users.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql(utcDefault);

        var types = model.Entity<EventType>();
        types.ToTable("EventTypes", t => t.HasCheckConstraint("CK_EventTypes_Name", "Name IN ('Run', 'Walk', 'Cycle')"));
        types.HasKey(x => x.EventTypeId);
        types.Property(x => x.Name).HasMaxLength(20).IsRequired();
        types.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UQ_EventTypes_Name");
        types.HasData(new EventType { EventTypeId = 1, Name = "Run" },
            new EventType { EventTypeId = 2, Name = "Walk" }, new EventType { EventTypeId = 3, Name = "Cycle" });

        var events = model.Entity<RaceEvent>();
        events.ToTable("Events", t => t.HasCheckConstraint("CK_Events_Distance", "DistanceKm > 0"));
        events.HasKey(x => x.EventId);
        events.Property(x => x.Name).HasMaxLength(150).IsRequired();
        events.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        events.Property(x => x.Location).HasMaxLength(200).IsRequired();
        events.Property(x => x.DistanceKm).HasPrecision(6, 2);
        events.Property(x => x.EventDate).HasColumnType("datetime2(0)");
        events.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql(utcDefault);
        events.HasOne(x => x.Organiser).WithMany().HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.NoAction);
        events.HasOne(x => x.EventType).WithMany().HasForeignKey(x => x.EventTypeId).OnDelete(DeleteBehavior.NoAction);

        var categories = model.Entity<Category>();
        categories.ToTable("Categories", t =>
        {
            t.HasCheckConstraint("CK_Categories_Type", "CategoryType IN ('Age', 'Distance')");
            t.HasCheckConstraint("CK_Categories_Age", "(CategoryType = 'Age' AND MinAge IS NOT NULL AND MaxAge IS NOT NULL AND MinAge >= 0 AND MaxAge >= MinAge) OR (CategoryType = 'Distance' AND DistanceKm IS NOT NULL AND DistanceKm > 0 AND MinAge IS NULL AND MaxAge IS NULL)");
        });
        categories.HasKey(x => x.CategoryId);
        categories.Property(x => x.CategoryType).HasMaxLength(20).IsRequired();
        categories.Property(x => x.Name).HasMaxLength(100).IsRequired();
        categories.Property(x => x.DistanceKm).HasPrecision(6, 2);
        categories.HasOne(x => x.Event).WithMany(x => x.Categories).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);

        var enrolments = model.Entity<Enrolment>();
        enrolments.ToTable("Enrolments", t => t.HasCheckConstraint("CK_Enrolments_Status", "EnrolmentStatus IN ('Pending', 'Confirmed', 'Cancelled')"));
        enrolments.HasKey(x => x.EnrolmentId);
        enrolments.Property(x => x.EnrolmentStatus).HasMaxLength(20).HasDefaultValue("Confirmed").IsRequired();
        enrolments.Property(x => x.EnrolledAt).HasColumnType("datetime2(0)").HasDefaultValueSql(utcDefault);
        enrolments.HasIndex(x => new { x.EventId, x.ParticipantId }).IsUnique().HasDatabaseName("UQ_Enrolments_Event_Participant");
        enrolments.HasOne(x => x.Event).WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        enrolments.HasOne(x => x.Participant).WithMany().HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.NoAction);
        enrolments.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.NoAction);

        var results = model.Entity<RaceResult>();
        results.ToTable("Results", t => t.HasCheckConstraint("CK_Results_Position", "FinishingPosition > 0"));
        results.HasKey(x => x.ResultId);
        results.Property(x => x.FinishTime).HasColumnType("time(0)");
        results.Property(x => x.RecordedAt).HasColumnType("datetime2(0)").HasDefaultValueSql(utcDefault);
        results.HasIndex(x => x.EnrolmentId).IsUnique().HasDatabaseName("UQ_Results_Enrolment");
        results.HasOne(x => x.Enrolment).WithOne(x => x.Result).HasForeignKey<RaceResult>(x => x.EnrolmentId).OnDelete(DeleteBehavior.Cascade);
    }
}

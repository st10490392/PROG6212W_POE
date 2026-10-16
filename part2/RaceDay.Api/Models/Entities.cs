namespace RaceDay.Api.Models;

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public class User
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EventType
{
    public int EventTypeId { get; set; }
    public string Name { get; set; } = "";
}

public class RaceEvent
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public User Organiser { get; set; } = null!;
    public int EventTypeId { get; set; }
    public EventType EventType { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<Category> Categories { get; set; } = [];
}

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public RaceEvent Event { get; set; } = null!;
    public string CategoryType { get; set; } = "";
    public string Name { get; set; } = "";
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }
}

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public RaceEvent Event { get; set; } = null!;
    public int ParticipantId { get; set; }
    public User Participant { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string EnrolmentStatus { get; set; } = "Confirmed";
    public DateTime EnrolledAt { get; set; }
    public RaceResult? Result { get; set; }
}

public class RaceResult
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public Enrolment Enrolment { get; set; } = null!;
    public TimeSpan FinishTime { get; set; }
    public int FinishingPosition { get; set; }
    public DateTime RecordedAt { get; set; }
}

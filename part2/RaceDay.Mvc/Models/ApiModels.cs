namespace RaceDay.Mvc.Models;

public sealed class ApiProfile
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public sealed class ApiEvent
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public int EventTypeId { get; set; }
    public string EventType { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public string? BannerImageUrl { get; set; }
    public List<ApiCategory> Categories { get; set; } = [];
}

public sealed class ApiCategory
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryType { get; set; } = "";
    public string Name { get; set; } = "";
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }
}

public sealed class ApiEventType { public int EventTypeId { get; set; } public string Name { get; set; } = ""; }
public sealed class ApiEnrolment
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string EnrolmentStatus { get; set; } = "";
}
public sealed class ApiResult
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = "";
    public string FinishTime { get; set; } = "";
    public int FinishingPosition { get; set; }
}

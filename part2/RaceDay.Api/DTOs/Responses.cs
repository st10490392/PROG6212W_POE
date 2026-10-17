using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

// Explicit response projections keep password hashes and navigation cycles out of JSON.
public static class Responses
{
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
    public static object Profile(User u) => new { u.UserId, u.FirstName, u.LastName, u.Email, u.Role,
        u.Phone, u.DateOfBirth, u.ProfilePictureUrl, CreatedAt = Utc(u.CreatedAt) };
    public static object Event(RaceEvent e) => new { e.EventId, e.OrganiserId, e.EventTypeId,
        EventType = e.EventType.Name, e.Name, e.Description, EventDate = Utc(e.EventDate),
        e.Location, e.DistanceKm, CreatedAt = Utc(e.CreatedAt), Categories = e.Categories.Select(Category) };
    public static object Category(Category c) => new { c.CategoryId, c.EventId, c.CategoryType,
        c.Name, c.MinAge, c.MaxAge, c.DistanceKm };
    public static object Enrolment(Enrolment e) => new { e.EnrolmentId, e.EventId, e.ParticipantId,
        ParticipantName = $"{e.Participant.FirstName} {e.Participant.LastName}", EventName = e.Event.Name,
        EventDate = Utc(e.Event.EventDate), e.CategoryId, CategoryName = e.Category.Name,
        e.EnrolmentStatus, EnrolledAt = Utc(e.EnrolledAt) };
    public static object Result(RaceResult r) => new { r.ResultId, r.EnrolmentId,
        r.Enrolment.ParticipantId, r.Enrolment.EventId, EventName = r.Enrolment.Event.Name,
        EventDate = Utc(r.Enrolment.Event.EventDate), r.Enrolment.CategoryId,
        CategoryName = r.Enrolment.Category.Name, FinishTime = r.FinishTime.ToString(@"hh\:mm\:ss"),
        r.FinishingPosition, RecordedAt = Utc(r.RecordedAt) };
}

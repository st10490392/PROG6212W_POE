using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

public class RegisterRequest : IValidatableObject
{
    [Required, StringLength(50)] public string FirstName { get; set; } = "";
    [Required, StringLength(50)] public string LastName { get; set; } = "";
    [Required, EmailAddress, StringLength(255)] public string Email { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 12)] public string Password { get; set; } = "";
    [Required, RegularExpression("^(Organiser|Participant)$")] public string Role { get; set; } = "";
    [Phone, StringLength(30)] public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
            yield return new("Date of birth cannot be in the future.", [nameof(DateOfBirth)]);
    }
}

public class LoginRequest
{
    [Required, EmailAddress, StringLength(255)] public string Email { get; set; } = "";
    [Required, StringLength(128)] public string Password { get; set; } = "";
}

public class ProfileRequest : IValidatableObject
{
    [Required, StringLength(50)] public string FirstName { get; set; } = "";
    [Required, StringLength(50)] public string LastName { get; set; } = "";
    [Phone, StringLength(30)] public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    [Url, StringLength(500)] public string? ProfilePictureUrl { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
            yield return new("Date of birth cannot be in the future.", [nameof(DateOfBirth)]);
        if (ProfilePictureUrl is not null && Uri.TryCreate(ProfilePictureUrl, UriKind.Absolute, out var uri)
            && uri.Scheme != "http" && uri.Scheme != "https")
            yield return new("Profile picture URL must use HTTP or HTTPS.", [nameof(ProfilePictureUrl)]);
    }
}

public class EventRequest : IValidatableObject
{
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [Required, StringLength(1000)] public string Description { get; set; } = "";
    public DateTimeOffset EventDate { get; set; }
    [Required, StringLength(200)] public string Location { get; set; } = "";
    [Range(typeof(decimal), "0.01", "9999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)] public decimal DistanceKm { get; set; }
    [Range(1, int.MaxValue)] public int EventTypeId { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (EventDate == default) yield return new("Event date is required.", [nameof(EventDate)]);
        if (decimal.Round(DistanceKm, 2) != DistanceKm)
            yield return new("Distance supports at most two decimal places.", [nameof(DistanceKm)]);
    }
}

public class CategoryRequest : IValidatableObject
{
    [Required, RegularExpression("^(Age|Distance)$")] public string CategoryType { get; set; } = "";
    [Required, StringLength(100)] public string Name { get; set; } = "";
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    [Range(typeof(decimal), "0.01", "9999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)] public decimal? DistanceKm { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (CategoryType == "Age" && (MinAge is null || MaxAge is null || MinAge < 0 || MaxAge < MinAge))
            yield return new("Age categories require 0 <= minAge <= maxAge.", [nameof(MinAge), nameof(MaxAge)]);
        if (CategoryType == "Distance" && (DistanceKm is null || MinAge is not null || MaxAge is not null))
            yield return new("Distance categories require distanceKm and no age bounds.", [nameof(DistanceKm)]);
        if (DistanceKm is decimal distance && decimal.Round(distance, 2) != distance)
            yield return new("Distance supports at most two decimal places.", [nameof(DistanceKm)]);
    }
}

public class EnrolmentRequest
{
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
}

public class ResultRequest : IValidatableObject
{
    public TimeSpan? FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int FinishingPosition { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (FinishTime is null || FinishTime < TimeSpan.Zero || FinishTime >= TimeSpan.FromDays(1)
            || FinishTime.Value.Ticks % TimeSpan.TicksPerSecond != 0)
            yield return new("Finish time must be HH:mm:ss from 00:00:00 to 23:59:59 (SQL TIME(0)).", [nameof(FinishTime)]);
    }
}

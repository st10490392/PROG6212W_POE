using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.ViewModels;

public sealed class LoginVm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
}

public sealed class RegisterVm
{
    [Required, StringLength(50)] public string FirstName { get; set; } = "";
    [Required, StringLength(50)] public string LastName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 12), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";
    [Phone] public string? Phone { get; set; }
    [DataType(DataType.Date)] public DateOnly? DateOfBirth { get; set; }
}

public sealed class EventFormVm
{
    public int EventId { get; set; }
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [Required, StringLength(1000)] public string Description { get; set; } = "";
    [Required, DataType(DataType.DateTime)] public DateTime EventDate { get; set; } = DateTime.Today.AddDays(14).AddHours(8);
    [Required, StringLength(200)] public string Location { get; set; } = "";
    [Range(0.01, 9999.99)] public decimal DistanceKm { get; set; }
    [Range(1, int.MaxValue)] public int EventTypeId { get; set; }
    public IFormFile? Banner { get; set; }
}

public sealed class CategoryFormVm
{
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    [Required] public string CategoryType { get; set; } = "Age";
    [Required, StringLength(100)] public string Name { get; set; } = "";
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }
}

public sealed class ResultFormVm
{
    public int EnrolmentId { get; set; }
    [Required] public TimeSpan? FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int FinishingPosition { get; set; }
}

public sealed class ProfileVm
{
    public int UserId { get; set; }
    [Required] public string FirstName { get; set; } = "";
    [Required] public string LastName { get; set; } = "";
    [EmailAddress] public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    [Phone] public string? Phone { get; set; }
    [DataType(DataType.Date)] public DateOnly? DateOfBirth { get; set; }
    [Url] public string? ProfilePictureUrl { get; set; }
    public IFormFile? ProfilePicture { get; set; }
}

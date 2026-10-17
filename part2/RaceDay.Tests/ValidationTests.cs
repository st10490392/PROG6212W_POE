using System.ComponentModel.DataAnnotations;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData("en-ZA")]
    [InlineData("af-ZA")]
    [InlineData("fr-FR")]
    public void JSON_decimal_validation_is_independent_of_server_number_format(string culture)
    {
        var original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(culture);
            var request = new EventRequest { Name = "Run", Description = "Road run", Location = "Polokwane",
                EventTypeId = 1, EventDate = DateTimeOffset.UtcNow, DistanceKm = 10.25m };
            Assert.True(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
            var category = new CategoryRequest { CategoryType = "Distance", Name = "Open", DistanceKm = 10.25m };
            Assert.True(Validator.TryValidateObject(category, new ValidationContext(category), [], true));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = original; }
    }

    [Theory]
    [InlineData(0, 19, true)]
    [InlineData(20, 39, true)]
    [InlineData(20, 20, true)]
    [InlineData(-1, 19, false)]
    [InlineData(40, 20, false)]
    public void Age_category_validates_inclusive_bounds(int minAge, int maxAge, bool valid)
    {
        var request = new CategoryRequest { CategoryType = "Age", Name = "Senior", MinAge = minAge, MaxAge = maxAge };
        Assert.Equal(valid, Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }

    [Theory]
    [InlineData("00:00:00", true)]
    [InlineData("23:59:59", true)]
    [InlineData("1.00:00:00", false)]
    [InlineData("-00:00:01", false)]
    [InlineData("00:00:01.5", false)]
    public void Finish_time_respects_SQL_Server_time_range_and_second_precision(string time, bool valid)
    {
        var request = new ResultRequest { FinishTime = TimeSpan.Parse(time, System.Globalization.CultureInfo.InvariantCulture), FinishingPosition = 1 };
        Assert.Equal(valid, Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }

    [Theory]
    [InlineData("0.01", true)]
    [InlineData("9999.99", true)]
    [InlineData("10000", false)]
    [InlineData("-1", false)]
    [InlineData("10.001", false)]
    public void Event_distance_fits_database_precision(string distance, bool valid)
    {
        var request = new EventRequest { Name = "Run", Description = "Road run", Location = "Polokwane",
            EventTypeId = 1, EventDate = DateTimeOffset.UtcNow, DistanceKm = decimal.Parse(distance, System.Globalization.CultureInfo.InvariantCulture) };
        Assert.Equal(valid, Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }
}

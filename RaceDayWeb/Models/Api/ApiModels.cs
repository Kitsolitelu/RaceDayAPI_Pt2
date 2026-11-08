namespace RaceDayWeb.Models.Api
{
    // These classes only describe the JSON the Part 2 API sends and receives.
    // There is no database code and no business logic in this project.

    public class RegisterDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? OrganiserCode { get; set; }
    }

    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
    }

    public class UpdateProfileDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class EventDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public int? ElevationGainM { get; set; }
        public string? RouteDescript { get; set; }
    }

    public class EventResponseDto
    {
        public int EventId { get; set; }
        public int OrganiserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public decimal? Distance { get; set; }
        public int? ElevationGainM { get; set; }
        public string? RouteDescript { get; set; }
        public string? BannerUrl { get; set; }
        public int EnrolmentCount { get; set; }
    }

    public class CategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int? MaxParticipants { get; set; }
    }

    public class CategoryResponseDto
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int? MaxParticipants { get; set; }
    }

    public class EnrolmentResponseDto
    {
        public int EnrolmentId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public DateTime EnrolmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ResultDto
    {
        public TimeSpan FinishTime { get; set; }
        public int Position { get; set; }
    }

    public class ResultResponseDto
    {
        public int ResultId { get; set; }
        public int EnrolmentId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime? EventDate { get; set; }
        public int? TotalFinishers { get; set; }
    }

    public class ImageUploadResponseDto
    {
        public string Url { get; set; } = string.Empty;
    }
}

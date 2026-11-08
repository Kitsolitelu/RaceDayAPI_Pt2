using System.ComponentModel.DataAnnotations;
using RaceDayWeb.Models.Api;

namespace RaceDayWeb.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "That does not look like an email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Please enter your full name."), MaxLength(100)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email address."), MaxLength(150)]
        [EmailAddress(ErrorMessage = "That does not look like an email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a password.")]
        [MinLength(8, ErrorMessage = "The password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please repeat your password.")]
        [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a role.")]
        public string Role { get; set; } = "Participant";

        [Display(Name = "Organiser access code")]
        public string? OrganiserCode { get; set; }
    }

    public class EventListViewModel
    {
        public List<EventResponseDto> Events { get; set; } = new();
        public string? Search { get; set; }
        public string? EventType { get; set; }
        public string? Location { get; set; }
        public string? Error { get; set; }
    }

    public class EventDetailsViewModel
    {
        public EventResponseDto Event { get; set; } = new();
        public List<CategoryResponseDto> Categories { get; set; } = new();
    }

    public class EventFormViewModel
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Please enter the event name."), MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "The description can be 500 characters at most.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please choose the event date.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Event date")]
        public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30);

        [Required(ErrorMessage = "Please enter the location."), MaxLength(150)]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose an event type.")]
        [Display(Name = "Event type")]
        public string EventType { get; set; } = "Run";

        [Range(0.01, 999.99, ErrorMessage = "The distance must be between 0.01 and 999.99 km.")]
        [Display(Name = "Distance (km)")]
        public decimal Distance { get; set; }

        [Range(0, 9000, ErrorMessage = "Elevation gain must be between 0 and 9000 metres.")]
        [Display(Name = "Elevation gain (m)")]
        public int? ElevationGainM { get; set; }

        [MaxLength(500, ErrorMessage = "The route description can be 500 characters at most.")]
        [Display(Name = "Route description")]
        public string? RouteDescript { get; set; }

        public string? CurrentBannerUrl { get; set; }

        [Display(Name = "Banner image")]
        public IFormFile? Banner { get; set; }
    }

    public class DashboardViewModel
    {
        public List<EventResponseDto> Events { get; set; } = new();
        public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
        public int UpcomingCount => Events.Count(e => e.EventDate.Date >= DateTime.Today);
        public string? Error { get; set; }
    }

    public class CategoryFormViewModel
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Please enter a category name, for example Under 20 or 10km."), MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 99999.99, ErrorMessage = "The price must be between 0 and 99999.99.")]
        [Display(Name = "Price (R)")]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "The maximum must be at least 1.")]
        [Display(Name = "Maximum participants")]
        public int? MaxParticipants { get; set; }
    }

    public class ManageCategoriesViewModel
    {
        public EventResponseDto Event { get; set; } = new();
        public List<CategoryResponseDto> Categories { get; set; } = new();
        public CategoryFormViewModel NewCategory { get; set; } = new();
    }

    public class ManageEnrolmentsViewModel
    {
        public EventResponseDto Event { get; set; } = new();
        public List<EnrolmentResponseDto> Enrolments { get; set; } = new();
    }

    public class ResultFormViewModel
    {
        public int EnrolmentId { get; set; }
        public int EventId { get; set; }
        public string? FinishTime { get; set; }
        public int Position { get; set; }
    }

    public class ManageResultsViewModel
    {
        public EventResponseDto Event { get; set; } = new();
        public List<EnrolmentResponseDto> Enrolments { get; set; } = new();
        public List<ResultResponseDto> Results { get; set; } = new();
    }

    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Please enter your full name."), MaxLength(100)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email address."), MaxLength(150)]
        [EmailAddress(ErrorMessage = "That does not look like an email address.")]
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
    }
}

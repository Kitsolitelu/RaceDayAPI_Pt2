using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.Models
{
    public class Event
    {
        public int EventId { get; set; }

        public int OrganiserId { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime EventDate { get; set; }

        [Required, MaxLength(150)]
        public string Location { get; set; } = string.Empty;

        // added because the Part 2 brief asks for it: Run, Walk or Cycle
        [Required, MaxLength(20)]
        public string EventType { get; set; } = "Run";

        // link to the banner image saved in Azure Blob Storage (Part 3)
        [MaxLength(500)]
        public string? BannerUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        public User Organiser { get; set; } = null!;

        public RouteInfo? RouteInfo { get; set; }

        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}

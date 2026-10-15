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

      
        [Required, MaxLength(20)]
        public string EventType { get; set; } = "Run";

        public DateTime CreatedAt { get; set; }

        public User Organiser { get; set; } = null!;

        public RouteInfo? RouteInfo { get; set; }

        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}

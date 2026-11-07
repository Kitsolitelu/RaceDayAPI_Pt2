using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.DTOs
{
    // used when an organiser creates or updates an event
    public class EventDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime EventDate { get; set; }

        [Required, MaxLength(150)]
        public string Location { get; set; } = string.Empty;

        // Run, Walk or Cycle
        [Required]
        public string EventType { get; set; } = string.Empty;

        // distance in km, this is saved in the RouteInfo table
        [Range(0.01, 999.99)]
        public decimal Distance { get; set; }

        public int? ElevationGainM { get; set; }

        [MaxLength(500)]
        public string? RouteDescript { get; set; }
    }

    // what we send back for an event
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

        // only filled in by GET /api/events/mine (the organiser dashboard)
        public int EnrolmentCount { get; set; }
    }
}

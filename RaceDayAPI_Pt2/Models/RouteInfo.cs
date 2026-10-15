using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.Models
{
    public class RouteInfo
    {
        
        [Key]
        public int RouteId { get; set; }

        public int EventId { get; set; }

        public decimal Distance { get; set; }

        public int? ElevationGainM { get; set; }

        [MaxLength(500)]
        public string? RouteDescript { get; set; }

        public Event Event { get; set; } = null!;
    }
}

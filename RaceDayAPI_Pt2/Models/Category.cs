using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.Models
{
    public class Category
    {
        public int CategoryId { get; set; }

        public int EventId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int? MaxParticipants { get; set; }

        public Event Event { get; set; } = null!;

        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}

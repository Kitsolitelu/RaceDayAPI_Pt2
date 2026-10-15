using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.DTOs
{
    public class CategoryDto
    {
       
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 99999.99)]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue)]
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
}

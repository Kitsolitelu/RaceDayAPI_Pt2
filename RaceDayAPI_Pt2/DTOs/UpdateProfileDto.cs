using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.DTOs
{
    public class UpdateProfileDto
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;
    }
}

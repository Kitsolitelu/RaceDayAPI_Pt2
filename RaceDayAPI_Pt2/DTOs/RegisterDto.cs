using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.DTOs
{
    public class RegisterDto
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string Password { get; set; } = string.Empty;

      
        [Required]
        public string Role { get; set; } = string.Empty;

     
        public string? OrganiserCode { get; set; }
    }
}

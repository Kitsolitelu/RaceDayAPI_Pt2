using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

       
        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

    
        [Required, MaxLength(20)]
        public string Role { get; set; } = "Participant";

        public DateTime CreatedAt { get; set; }


        public ICollection<Event> Events { get; set; } = new List<Event>();

        
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}

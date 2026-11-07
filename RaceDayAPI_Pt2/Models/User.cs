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

        // we never store the real password, only the hash
        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        // Organiser or Participant
        [Required, MaxLength(20)]
        public string Role { get; set; } = "Participant";

        // link to the profile picture saved in Azure Blob Storage (Part 3)
        [MaxLength(500)]
        public string? ProfilePictureUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        // events this user organises
        public ICollection<Event> Events { get; set; } = new List<Event>();

        // enrolments this user has made as a participant
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}

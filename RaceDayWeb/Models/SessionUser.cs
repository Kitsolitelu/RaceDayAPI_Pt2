namespace RaceDayWeb.Models
{
    // the logged in user, kept in the session so every page knows who they are and what role they have
    public class SessionUser
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }

        public bool IsOrganiser => Role == "Organiser";
        public bool IsParticipant => Role == "Participant";
    }
}

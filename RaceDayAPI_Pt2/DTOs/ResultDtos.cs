using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.DTOs
{
    public class ResultDto
    {
        // sent as text like "01:45:30" (hours:minutes:seconds)
        public TimeSpan FinishTime { get; set; }

        [Range(1, int.MaxValue)]
        public int Position { get; set; }
    }

    public class ResultResponseDto
    {
        public int ResultId { get; set; }
        public int EnrolmentId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }

        // extra details used by the My Results page
        public string EventName { get; set; } = string.Empty;
        public DateTime? EventDate { get; set; }

        // how many people have a captured result for the same event, e.g. 47th out of 312
        public int? TotalFinishers { get; set; }
    }
}

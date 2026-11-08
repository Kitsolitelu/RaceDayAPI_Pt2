namespace RaceDayWeb.Helpers
{
    // turns numbers and times into the text the pages show
    public static class Formatters
    {
        // 1 -> 1st, 2 -> 2nd, 47 -> 47th
        public static string Ordinal(int number)
        {
            var lastTwo = number % 100;
            if (lastTwo >= 11 && lastTwo <= 13)
            {
                return number + "th";
            }

            return (number % 10) switch
            {
                1 => number + "st",
                2 => number + "nd",
                3 => number + "rd",
                _ => number + "th"
            };
        }

        public static string Time(TimeSpan? time)
        {
            return time.HasValue ? time.Value.ToString(@"h\:mm\:ss") : "-";
        }

        public static string Money(decimal amount)
        {
            return amount == 0 ? "Free" : "R " + amount.ToString("N2");
        }
    }
}

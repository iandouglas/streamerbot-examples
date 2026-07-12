using System;

namespace iandouglas736
{
    /// <summary>
    /// Helpers for working with Unix epoch times and relative human-readable durations.
    /// No Streamer.bot context is required.
    /// </summary>
    public static class Time
    {
        /// <summary>
        /// Returns the current Unix epoch time in seconds.
        /// </summary>
        public static long NowEpoch()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Returns the current Unix epoch time in milliseconds.
        /// </summary>
        public static long NowEpochMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Returns the current UTC DateTimeOffset.
        /// </summary>
        public static DateTimeOffset NowUtc()
        {
            return DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Converts a DateTimeOffset to Unix epoch seconds.
        /// </summary>
        public static long ToEpoch(DateTimeOffset dateTime)
        {
            return dateTime.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Converts a Unix epoch time in seconds to a UTC DateTimeOffset.
        /// </summary>
        public static DateTimeOffset FromEpoch(long epochSeconds)
        {
            return DateTimeOffset.FromUnixTimeSeconds(epochSeconds);
        }

        /// <summary>
        /// Strict equality check between two epoch times.
        /// </summary>
        public static bool EpochsEqual(long epoch1, long epoch2)
        {
            return epoch1 == epoch2;
        }

        /// <summary>
        /// Returns true if two epoch times are within +/- drift seconds of each other.
        /// </summary>
        public static bool EpochsSimilar(long epoch1, long epoch2, int driftSeconds)
        {
            if (driftSeconds < 0)
                driftSeconds = 0;
            return Math.Abs(epoch1 - epoch2) <= driftSeconds;
        }

        /// <summary>
        /// Compares two epoch times and returns a human-readable string describing
        /// where epoch1 sits relative to epoch2.
        /// If epoch1 is earlier than epoch2, returns something like "2 minutes ago".
        /// If epoch1 is later than epoch2, returns something like "in 3 minutes".
        /// If they are equal, returns "now".
        /// </summary>
        public static string CompareEpoch(long epoch1, long epoch2)
        {
            long diffSeconds = epoch1 - epoch2;
            return FormatRelative(diffSeconds);
        }

        /// <summary>
        /// Returns a human-readable string describing how long ago the given epoch was.
        /// Example: "5 minutes ago" or "just now".
        /// </summary>
        public static string Ago(long epochSeconds)
        {
            return FormatRelative(NowEpoch() - epochSeconds);
        }

        /// <summary>
        /// Returns a human-readable string describing how long until the given epoch.
        /// Example: "in 5 minutes" or "now".
        /// </summary>
        public static string Until(long epochSeconds)
        {
            return FormatRelative(epochSeconds - NowEpoch());
        }

        /// <summary>
        /// Formats a number of seconds as a relative human-readable string.
        /// Positive values are "in X", negative values are "X ago".
        /// </summary>
        private static string FormatRelative(long seconds)
        {
            if (seconds == 0)
                return "now";

            bool future = seconds > 0;
            long absSeconds = Math.Abs(seconds);

            string text;
            if (absSeconds < 60)
            {
                text = $"{absSeconds} second{(absSeconds == 1 ? "" : "s")}";
            }
            else if (absSeconds < 3600)
            {
                long minutes = absSeconds / 60;
                text = $"{minutes} minute{(minutes == 1 ? "" : "s")}";
            }
            else if (absSeconds < 86400)
            {
                long hours = absSeconds / 3600;
                long minutes = (absSeconds % 3600) / 60;
                if (minutes == 0)
                    text = $"{hours} hour{(hours == 1 ? "" : "s")}";
                else
                    text = $"{hours} hour{(hours == 1 ? "" : "s")} {minutes} minute{(minutes == 1 ? "" : "s")}";
            }
            else
            {
                long days = absSeconds / 86400;
                long hours = (absSeconds % 86400) / 3600;
                if (hours == 0)
                    text = $"{days} day{(days == 1 ? "" : "s")}";
                else
                    text = $"{days} day{(days == 1 ? "" : "s")} {hours} hour{(hours == 1 ? "" : "s")}";
            }

            return future ? $"in {text}" : $"{text} ago";
        }
    }
}

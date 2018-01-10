using System;

namespace Nyan.Modules.Web.Tools.Extensions {
    public static class DateTimeComparison
    {
        public enum EDataCompare
        {
            Past,
            Present,
            Future
        }

        public static EDataCompare CompareDate(this DateTime comparedDateTime, DateTime? reference = null)
        {
            if (reference == null) reference = DateTime.Now;

            return comparedDateTime.Date == reference.Value.Date ? EDataCompare.Present : (comparedDateTime.Date < reference.Value.Date ? EDataCompare.Past : EDataCompare.Future);
        }

        public static string FriendlyComparedDate(this DateTime comparedDateTime, DateTime? reference = null)
        {
            const int SECOND = 1;
            const int MINUTE = 60 * SECOND;
            const int HOUR = 60 * MINUTE;
            const int DAY = 24 * HOUR;
            const int MONTH = 30 * DAY;

            if (reference == null) reference = DateTime.Now;

            reference = reference.Value.Date;
            comparedDateTime = comparedDateTime.Date;

            var ts = new TimeSpan(reference.Value.Ticks - comparedDateTime.Ticks);
            var delta = ts.TotalSeconds;

            // Past

            if (reference.Value.Date == comparedDateTime.Date) return "today";
            if (reference.Value.Date == comparedDateTime.Date.AddDays(1)) return "yesterday";
            if (reference.Value.Date == comparedDateTime.Date.AddDays(-1)) return "tomorrow";

            if (delta > 0)
            {
                delta = Math.Abs(delta);

                if (delta < 30 * DAY) return ts.Days + " days ago";

                if (delta < 12 * MONTH)
                {
                    var months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                    return months <= 1 ? "one month ago" : months + " months ago";
                }
                var years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year ago" : years + " years ago";
            }
            
            // Future
            if (delta < 30 * DAY) return Math.Abs(ts.Days) + " days from now";

            if (delta < 12 * MONTH)
            {
                var months = Math.Abs(Convert.ToInt32(Math.Floor((double)ts.Days / 30)));
                return months <= 1 ? "one month from now" : months + " months from now";
            }
            {
                var years = Math.Abs(Convert.ToInt32(Math.Floor((double)ts.Days / 365)));
                return years <= 1 ? "one year form now" : years + " years from now";
            }
        }
    }
}
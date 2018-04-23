using System;

namespace Nyan.Modules.Web.Tools.Extensions
{
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
            const int WEEK = 7 * DAY;
            const int MONTH = 30 * DAY;

            if (reference == null) reference = DateTime.Now;



            reference = reference.Value.Date;
            var comparedDate = comparedDateTime.Date;

            var ts = new TimeSpan(reference.Value.Ticks - comparedDate.Ticks);
            var delta = ts.TotalSeconds;

            // Past

            if (reference.Value.Date == comparedDate.Date) return "today";
            if (reference.Value.Date == comparedDate.Date.AddDays(1)) return "yesterday";
            if (reference.Value.Date == comparedDate.Date.AddDays(-1)) return "tomorrow";

            var ret = "";

            if (delta > 0)
            {
                delta = Math.Abs(delta);

                if (delta < 30 * DAY) { ret = ts.Days + " days ago"; }
                else if (delta < 1 * MONTH)
                {
                    var weeks = Convert.ToInt32(Math.Floor((double)ts.Days / 7));
                    ret = weeks <= 1 ? "one week ago" : weeks + " weeks ago";
                }

                else if (delta < 12 * MONTH)
                {
                    var months = Convert.ToInt32(Math.Floor((double) ts.Days / 30));
                    ret = months <= 1 ? "one month ago" : months + " months ago";
                }
                else
                {
                    var years = Convert.ToInt32(Math.Floor((double) ts.Days / 365));
                    ret = years <= 1 ? "one year ago" : years + " years ago";
                }
            }
            else
            {
                // Future
                if (delta < 30 * DAY)
                {
                    ret = Math.Abs(ts.Days) + " days from now";
                }
                else
                {
                    if (delta < 1 * MONTH)
                    {
                        var weeks = Math.Abs(Convert.ToInt32(Math.Floor((double)ts.Days / 7)));
                        ret = weeks <= 1 ? "one week from now" : weeks + " weeks from now";
                    }
                    if (delta < 12 * MONTH)
                    {
                        var months = Math.Abs(Convert.ToInt32(Math.Floor((double)ts.Days / 30)));
                        ret = months <= 1 ? "one month from now" : months + " months from now";
                    }
                    else
                    {
                        var years = Math.Abs(Convert.ToInt32(Math.Floor((double) ts.Days / 365)));
                        ret = years <= 1 ? "one year from now" : years + " years from now";
                    }
                }
            }

            if (comparedDateTime.TimeOfDay.TotalSeconds > 0) // So we have a time part.
            {
                ret += " at " + comparedDateTime.ToString("hh:mm tt");
            }

            return ret;
        }
    }
}
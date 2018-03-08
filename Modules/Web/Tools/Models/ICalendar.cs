using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.Tools.Models
{
    // ReSharper disable once InconsistentNaming
    public class iCalendar : IiCalendar
    {
        // ReSharper disable once InconsistentNaming
        public string TimeZoneID { get; set; }
        // ReSharper disable once InconsistentNaming
        public string UID { get; set; } = Guid.NewGuid().ToString();
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.Now;
        public DateTime CreatedDateTime { get; set; } = DateTime.Now;
        public DateTime LastModifiedTimeStamp { get; set; } = DateTime.Now;
        public string Description { get; set; }
        public string Location { get; set; }
        public string Summary { get; set; }
        public string AlarmTrigger { get; set; }
        public string AlarmRepeat { get; set; }
        public string AlarmDuration { get; set; }
        public string AlarmDescription { get; set; }

        public string Contact { get; set; }

        public string Categories { get; set; }

        public static List<IiCalendar> FromString(string source)
        {
            string line = null;

            try
            {

                var ret = new List<IiCalendar>();

                var temp = new iCalendar();

                using (var reader = new StringReader(source))
                {

                    while ((line = reader.ReadLine()) != null)
                    {
                        var idx = line.IndexOf(":", StringComparison.InvariantCultureIgnoreCase);

                        if (idx == -1) continue;

                        var pre = line.Substring(0, idx);
                        var pos = line.Substring(idx + 1);

                        pre = pre.Split(';')[0];

                        DateTime dateValue = DateTime.MinValue;


                        switch (pre)
                        {
                            case "BEGIN":
                                if (pos == "VEVENT") temp = new iCalendar();
                                break;
                            case "DTSTART":

                                DateTime.TryParseExact(pos, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue);
                                if (dateValue == DateTime.MinValue) DateTime.TryParseExact(pos, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue);

                                temp.StartDateTime = dateValue;
                                break;
                            case "DTEND":
                                DateTime.TryParseExact(pos, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue);
                                if (dateValue == DateTime.MinValue) DateTime.TryParseExact(pos, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue);

                                temp.EndDateTime = dateValue;
                                break;
                            case "DTSTAMP":
                                DateTime.TryParseExact(pos, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue);
                                if (dateValue == DateTime.MinValue) DateTime.TryParseExact(pos, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue);

                                temp.TimeStamp = dateValue;
                                break;
                            case "SUMMARY":
                                temp.Summary = pos;
                                break;
                            case "CATEGORIES":
                                temp.Categories = pos;
                                break;
                            case "DESCRIPTION":
                                temp.Description = pos;
                                break;
                            case "TZID":
                                temp.TimeZoneID = pos;
                                break;
                            case "UID":
                                temp.UID = pos;
                                break;
                            case "LOCATION":
                                temp.Location = pos;
                                break;
                            case "CONTACT":
                                temp.Contact = pos;
                                break;
                            case "END":
                                if (pos == "VEVENT") ret.Add(temp);
                                break;
                        }
                    }
                }

                return ret;
            }
            catch (Exception e)
            {
                Current.Log.Add(e.Message + ": " + line, Message.EContentType.Warning);

                Current.Log.Add(e);
                return null;
            }
        }
    }
}
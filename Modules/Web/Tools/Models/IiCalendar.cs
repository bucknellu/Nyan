using System;

namespace Nyan.Modules.Web.Tools.Models
{
    // ReSharper disable once InconsistentNaming
    public interface IiCalendar
    {
        string AlarmDescription { get; set; }
        string AlarmDuration { get; set; }
        string AlarmRepeat { get; set; }
        string AlarmTrigger { get; set; }
        string Categories { get; set; }
        string Contact { get; set; }
        DateTime CreatedDateTime { get; set; }
        string Description { get; set; }
        DateTime EndDateTime { get; set; }
        DateTime LastModifiedTimeStamp { get; set; }
        string Location { get; set; }
        DateTime StartDateTime { get; set; }
        string Summary { get; set; }
        DateTime TimeStamp { get; set; }
        // ReSharper disable once InconsistentNaming
        string TimeZoneID { get; set; }
        // ReSharper disable once InconsistentNaming
        string UID { get; set; }
    }
}
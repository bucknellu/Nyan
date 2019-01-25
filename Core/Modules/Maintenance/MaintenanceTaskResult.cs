using System;
using System.Collections.Generic;
using Nyan.Core.Extensions;
using Nyan.Core.Settings;

namespace Nyan.Core.Modules.Maintenance
{
    public class MaintenanceTaskResult
    {
        public enum EResultStatus
        {
            Success,
            Undefined,
            Failed,
            Warning,
            Skipped
        }

        public EResultStatus Status { get; set; } = EResultStatus.Undefined;
        public string Message { get; set; }
        public TagClicker Counters { get; set; }
        public string Id { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.Now;
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public int Priority { get; set; }

        public List<ChangeEntry> Changes { get; set; }

        public ChangeEntry AddChange(ChangeEntry.EChangeType type, string subject, string locator, string valueName = null, string originalValue = null, string newValue = null, string comments = null)
        {
            var entry = new ChangeEntry { Comments = comments, Locator = locator, Subject = subject, Type = type, NewValue = newValue, OriginalValue = originalValue, ValueName = valueName };

            //switch (type) {
            //    case ChangeEntry.EChangeType.CREATE:
            //    case ChangeEntry.EChangeType.REMOVE: Current.Log.Add($"{type.ToString()} : {subject} [{locator}] {comments}");
            //        break;
            //    case ChangeEntry.EChangeType.MODIFY: Current.Log.Add($"{type.ToString()} : {subject} [{locator}] - {valueName} '{originalValue}' > '{newValue}'  {comments}");
            //        break;
            //    case ChangeEntry.EChangeType.OTHER: break;
            //    default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            //}

            if (Changes == null) Changes = new List<ChangeEntry>();
            Changes.Add(entry);

            return entry;
        }

        public class ChangeEntry
        {
            public enum EChangeType
            {
                CREATE,
                MODIFY,
                REMOVE,
                OTHER
            }

            public string Subject { get; set; }
            public EChangeType Type { get; set; }
            public string Locator { get; set; }
            public string Comments { get; set; }
            public string ValueName { get; set; }
            public string OriginalValue { get; set; }
            public string NewValue { get; set; }
        }

        public class DebugInfoBlock
        {
            public string Step { get; set; }
            public string TraceInfo { get; set; }
            public string Target { get; set; }
        }

        public DebugInfoBlock DebugInfo { get; set; } = new DebugInfoBlock();

        public void SetStep(string step)
        {
            Current.Log.Add(step, Log.Message.EContentType.Info);
            DebugInfo.Step = step;
        }
        public void SetDebugTarget(string target, Exception e = null)
        {
            Current.Log.Add(target, Log.Message.EContentType.Warning);
            DebugInfo.Target = target;

            if (e == null) return;

            Current.Log.Add(e);
            DebugInfo.TraceInfo = e.ToSummary();

        }
    }
}
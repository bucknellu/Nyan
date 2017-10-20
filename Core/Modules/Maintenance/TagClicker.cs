using System.Collections.Generic;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Core.Modules.Maintenance
{
    public class TagClicker : SortedDictionary<string, long>
    {
        public TagClicker()
        {

        }

        public TagClicker(string suffix) { _suffix = suffix; }

        private string _suffix;
        private int _maxLength;

        public new long this[string tag]
        {
            get
            {
                if (ContainsKey(tag)) return base[tag];

                Add(tag, 0);
                if (tag.Length > _maxLength) _maxLength = tag.Length;
                return base[tag];
            }
            set
            {
                if (!ContainsKey(tag))
                {
                    Add(tag, 0);
                    if (tag.Length > _maxLength) _maxLength = tag.Length;
                }
                base[tag] = value;
            }
        }

        public void Click(string tag) { this[tag]++; }

        public void ToLog()
        {
            if (Keys.Count <= 0) return;

            foreach (var key in Keys)
                Current.Log.Add(key.PadLeft(_maxLength + 4) + (_suffix != null ? " " + _suffix : "") + ": " + base[key], Message.EContentType.MoreInfo);
        }
    }
}
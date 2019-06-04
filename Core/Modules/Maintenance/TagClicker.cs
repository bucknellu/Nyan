using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Core.Modules.Maintenance
{
    public class TagClicker : ConcurrentDictionary<string, long>
    {
        private int _maxLength;

        private readonly string _suffix;

        public TagClicker() { }

        public TagClicker(string suffix) { _suffix = suffix; }

        public new long this[string tag]
        {
            get
            {
                if (ContainsKey(tag)) return base[tag];

                TryAdd(tag, 0);
                if (tag.Length > _maxLength) _maxLength = tag.Length;
                return base[tag];
            }
            set
            {
                if (!ContainsKey(tag))
                {
                    TryAdd(tag, 0);
                    if (tag.Length > _maxLength) _maxLength = tag.Length;
                }

                base[tag] = value;
            }
        }
        public void Click(string tag) { this[tag]++; }

        public void Click<T>(string tag, IEnumerable<T> source)
        {
            Click(tag, source.Count());
        }

        public void Click(string tag, long count)
        {
            this[tag] += count;

            if (count > 1) Current.Log.Add($"{tag}: {count}");
        }

        public void ToLog(Message.EContentType type = Message.EContentType.MoreInfo)
        {
            if (Keys.Count <= 0) return;

            foreach (var key in Keys) Current.Log.Add(key.PadLeft(_maxLength + 4) + (_suffix != null ? " " + _suffix : "") + ": " + base[key], type);
        }
    }
}
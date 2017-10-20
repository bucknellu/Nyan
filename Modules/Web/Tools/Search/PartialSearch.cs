using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nyan.Modules.Web.Tools.Search
{
    public class PartialSearch : Dictionary<string, PartialSearch.Entry>
    {
        public int Total { get; set; }

        public class Entry
        {
            public List<object> Items = new List<object>();
            public Entry() { }

            public Entry(object oValue, int itemCount)
            {
                try
                {
                    var col0 = (IList)oValue;
                    var col = col0.Cast<object>().ToList();
                    Count = col.Count;
                    Items = col.Take(itemCount).ToList();
                }
                catch
                {
                    //Message = oValue.ToString();

                    //var isNumeric = int.TryParse("123", out int n);
                    //Count = isNumeric ? n : 0;
                }
            }

            public int Count { get; set; }
            public string Message { get; set; }
        }
    }

    public static class Extensions
    {
        public static PartialSearch ToPartialSearch(this Dictionary<string, object> src, int itemCount = 32)
        {
            var ret = new PartialSearch();

            foreach (var o in src)
            {
                var i = new PartialSearch.Entry(o.Value, itemCount);

                if (i.Count > 0)
                {
                    ret.Add(o.Key, i);
                    ret.Total += i.Count;
                }
            }

            return ret;
        }
    }
}
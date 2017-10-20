using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nyan.Core.Modules.Data.Contracts
{
    public interface ISearchDisabled { } // Marker for deprecated Search entities.

    public interface ISearch
    {
        string SearchResultMoniker { get; }
        List<SearchResult> SimpleQuery(string term);
    }

    public class SearchResult
    {
        public string Id { get; set; }
        public string Locator { get; set; }
        public string Description { get; set; }
        public string Body { get; set; }
        public double Score { get; set; }
        public bool IsKeyMatch { get; set; }
    }


}

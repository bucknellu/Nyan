using System.Collections.Generic;

namespace Nyan.Core.Modules.Data.Contracts
{
    // Marker for deprecated Search entities.
    public interface ISearch
    {
        string SearchResultMoniker { get; }
        List<SearchResult> SimpleQuery(string term);
    }
}
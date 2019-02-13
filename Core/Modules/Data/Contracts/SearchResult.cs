using System.Collections.Generic;

namespace Nyan.Core.Modules.Data.Contracts {
    public class SearchResult
    {
        public string Id { get; set; }
        public string Locator { get; set; }
        public string Description { get; set; }
        public string Body { get; set; }
        public double? Score { get; set; }
        public bool IsKeyMatch { get; set; }

        public class Comparer : IEqualityComparer<SearchResult>
        {
            public bool Equals(SearchResult x, SearchResult y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;

                return
                    x.Locator == y.Locator &&
                    x.Description == y.Description &&
                    x.IsKeyMatch == y.IsKeyMatch;
            }

            public int GetHashCode(SearchResult product)
            {
                var hashLocatorName = product.Locator == null ? 0 : product.Locator.GetHashCode();
                var hashDescriptionCode = product.Description == null ? 0 : product.Description.GetHashCode();

                return hashLocatorName ^ hashDescriptionCode ^ product.IsKeyMatch.GetHashCode();
            }
        }
    }
}
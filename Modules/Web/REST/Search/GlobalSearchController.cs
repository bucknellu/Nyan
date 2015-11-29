using Nyan.Core.Modules.Data.Contracts;
using Nyan.Core.Settings;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace Nyan.Modules.Web.REST.Search
{
    [RoutePrefix("stack")]
    public class SearchController : ApiController
    {
        [Route("search/{term}")]
        [HttpGet]
        public Dictionary<string, List<SearchResult>> Search(string term)
        {
            try
            {
                if (term == null) return null;
                term = term.Trim().ToLower();

                var ret = Core.Modules.Cache.Helper.FetchCacheableSingleResultByKey(doSearch, term, "GlobalSearch");

                return ret;
            }
            catch (Exception e)
            {
                Current.Log.Add(e);
                throw;
            }
        }

        private Dictionary<string, List<SearchResult>> doSearch(string term)
        {
            return Global.Run(term);
        }
    }
}

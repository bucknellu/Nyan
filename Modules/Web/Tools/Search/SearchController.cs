﻿using System;
using System.Collections.Generic;
using System.Web.Http;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Cache;
using Nyan.Core.Settings;

namespace Nyan.Modules.Web.Tools.Search
{
    [RoutePrefix("stack/tools/search")]
    public class SearchController : ApiController
    {
        [Route("{term}"), HttpGet]
        public object Search(string term, [FromUri] int? limit = 32)
        {
            try
            {
                if (term == null) return null;
                term = term.Trim().ToLower();

                var a = new ParmSet { Term = term };

                var ret = Helper.FetchCacheableSingleResultByKey(doSearch, a.ToJson(), "GlobalSearch");

                if (limit.HasValue) return ret.ToPartialSearch(limit.Value);

                return ret;
            }
            catch (Exception e)
            {
                Current.Log.Add(e);
                throw;
            }
        }

        [Route("{term}/{categories}"), HttpGet]
        public Dictionary<string, object> Search(string term, string categories)
        {
            try
            {
                if (term == null) return null;
                term = term.Trim().ToLower();

                var a = new ParmSet { Categories = categories, Term = term };

                var ret = Helper.FetchCacheableSingleResultByKey(doSearch, a.ToJson(), "GlobalSearch");

                return ret;
            }
            catch (Exception e)
            {
                Current.Log.Add(e);
                throw;
            }
        }

        private Dictionary<string, object> doSearch(string terms)
        {
            var term = terms.FromJson<ParmSet>();
            var ret = Global.Run(term.Term, term.Categories);

            return ret;
        }

        public class ParmSet
        {
            public string Term { get; set; }
            public string Categories { get; set; }
        }
    }
}
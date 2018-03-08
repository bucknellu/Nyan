using System.Collections.Generic;
using System.Linq;

namespace Nyan.Modules.Web.REST.RSS
{
    // https://www.strathweb.com/2012/04/rss-atom-mediatypeformatter-for-asp-net-webapi/
    public class UrlRepository : IUrlRepository
    {
        private int _nextId = 1;


        public string Title { get; set; }
        public List<Url> Items { get; } = new List<Url>();
        public IQueryable<Url> GetAll() { return Items.AsQueryable(); }

        public Url Get(int id) { return Items.Find(i => i.UrlId == id); }

        public Url Add(Url url)
        {
            url.UrlId = _nextId++;
            Items.Add(url);
            return url;
        }
    }
}
using System.Collections.Generic;
using System.Linq;

namespace Nyan.Modules.Web.REST.RSS {
    public interface IUrlRepository
    {
        string Title { get; set; }
        List<Url> Items { get; }

        IQueryable<Url> GetAll();
        Url Get(int id);
        Url Add(Url url);
    }
}
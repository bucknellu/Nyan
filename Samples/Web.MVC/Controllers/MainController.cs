using Nyan.WebSample.Models;
using System.Web.Mvc;

namespace Nyan.WebSample.Controllers
{
    public class MainController : Controller
    {
        //
        // GET: /Main/
        public ActionResult Index()
        {
            var a = Models.User.Get();
            return View(a);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(User user)
        {
            return RedirectToAction("Index");
        }
	}
}
﻿using System.Web.Mvc;

namespace Nyan.WebSample.Controllers
{
    public class MainController : Controller
    {
        //
        // GET: /Main/
        public ActionResult Index()
        {
            var a = Models.User.GetAll();
            return View(a);
        }
	}
}
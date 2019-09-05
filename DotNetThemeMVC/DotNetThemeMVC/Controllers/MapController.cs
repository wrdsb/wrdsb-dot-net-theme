using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DotNetThemeMVC.Controllers
{
    public class MapController : Controller
    {
        // GET: Map

        public ActionResult BeaverCreek()
        {
            return View();
        }

        public ActionResult Lockout()
        {
            return View();
        }
    }
}
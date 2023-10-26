using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace WebApplication1.Controllers
{
    public class MapController : Controller
    {
        // GET: Map
        public ActionResult Index()
        {
            return View();
        }

        // public ActionResult ComputeConture(double x, double y, string type, double trTime, double trEner, string typeCountreMethod)
        [HttpPost]
        public ActionResult ComputeConture(string longitude, string latitude)
        {
            Console.WriteLine();

            return Content(JsonConvert.SerializeObject("Uspjesno"), "application/json"); ;
        }
    }
}
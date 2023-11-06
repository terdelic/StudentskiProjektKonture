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
        public ActionResult ComputeConture(string longitude, string latitude, string selectedType, string selectedVarTime, string selectedVarEnergy)
        {
            Console.WriteLine();

            string primjer = "POLYGON ((15.9769105911255 45.8049086168488, 15.9787023067474 45.8049983661053, 15.9798556566238 45.8050731570458, 15.9815776348114 45.805331185026, 15.9851396083832 45.8070363789435, 15.987269282341 45.8094856448173, 15.9864270687103 45.8122376635526, 15.9853273630142 45.8129592927882, 15.976824760437 45.8113552375854, 15.9721308946609 45.8097698215335, 15.9706717729568 45.8089509297441, 15.9709185361862 45.8084162069347, 15.9769105911255 45.8049086168488))";
            Console.WriteLine();
            return Content(JsonConvert.SerializeObject(primjer), "application/json"); ;
        }
    }
}
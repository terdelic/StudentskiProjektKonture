using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using FibonacciHeap;
using System.Drawing;
using NetTopologySuite.Algorithm.Hull;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.Union;
using static WebApplication1.Controllers.MapController;
using System.Globalization;

namespace WebApplication1.Controllers
{
    public class MapController : Controller
    {        
        static Dictionary<int, Vrh> listaVrhova;
        FibonacciHeap<Vrh, double> heap;
        // Get the physical path of the App_Data folder
        string appDataPath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data");
        double vrijemePocetkaUSekundama=0;
        Vrh vEnergija = new Vrh();
        double n;
        Vrh pocetak;        
        string izbor;        
        bool podatciUcitani=false;

        StreamReader citanje;
        //Klasa za vrh
        public class Vrh
        {
            public int linkID { get; set; }
            public double xPocetak { get; set; }
            public double yPocetak { get; set; }
            public double xZavrsetak { get; set; }
            public double yZavrsetak { get; set; }
            public int duljinaLinka { get; set; }
            public int brzinaLinka { get; set; }
            public int ogranicenjeBrzine { get; set; }
            public int tipLinka { get; set; }
            public int zastavicaSmjera { get; set; }
            public List<int> listaSusjednihLinkova { get; set; }
            public double brzinaSlobodnogToka { get; set; }
            public double prosjecnaBrzina { get; set; }
            public double[] profilBrzine { get; set; }
            public double prosjecnaEnergija { get; set; }
            public double[] profilEnergije { get; set; }

            //Dio za graf
            public double tezina { get; set; }
            public Vrh prethodni { get; set; }
            public double tezinaEnergija { get; set; }
            public double ukupnaEnergija { get; set; }
            public double ukupnaDuljina { get; set; }
            public double vrijemePolaskaUSekundama { get; set; }
            public bool obraden { get; set; }

            public void reset()
            {
                tezina = double.MaxValue;
                prethodni = null;
                tezinaEnergija = double.MaxValue;
                vrijemePolaskaUSekundama= double.MaxValue;
            }
        }
        // GET: Map
        public ActionResult Index()
        {
            listaVrhova = new Dictionary<int, Vrh>();
            citanje = new StreamReader(appDataPath + "/updatedGraphZg.txt");
            UcitajPodatke(); //Ne znam jel ovo moze bit tu
            return View();
        }

        // public ActionResult ComputeConture(double x, double y, string type, double trTime, double trEner, string typeCountreMethod)
        [HttpPost]
        public ActionResult ComputeConture(string longitude, string latitude, string selectedType, string selectedVarTime, string selectedVarEnergy, string KontureType, string strVrijemePocetka)
        {
            //double dlongitude = Convert.ToDouble(longitude.Replace('.', ','));
            //double dlatitude = Convert.ToDouble(latitude.Replace('.', ','));

            double dlongitude = Convert.ToDouble(longitude, CultureInfo.InvariantCulture);
            double dlatitude = Convert.ToDouble(latitude, CultureInfo.InvariantCulture);
            selectedVarEnergy = selectedVarEnergy.Replace(',', '.');
            selectedVarTime= selectedVarTime.Replace(',', '.');
            string[] d = strVrijemePocetka.Split(':');
            int sati = Convert.ToInt32(d[0]);
            int min = Convert.ToInt32(d[1]);
            int sek = Convert.ToInt32(d[2]);
            vrijemePocetkaUSekundama = ((sati * 60) + min + (sek / 60.0))*60;
            //Pronadi najblizi vrh
            pocetak = PronadiNajbliziLink(dlongitude, dlatitude);

            if (vrijemePocetkaUSekundama == 0)
            {
                if (selectedType == "energy")
                {
                    //BellmanFord();
                    izbor = "energija prosjecna";
                    FibonacciHeapMetoda(izbor, vrijemePocetkaUSekundama);
                    n = Convert.ToDouble(selectedVarEnergy, CultureInfo.InvariantCulture);
                }
                else if (selectedType == "time")
                {                    
                    izbor = "vrijeme prosjecno";
                    FibonacciHeapMetoda(izbor, vrijemePocetkaUSekundama);
                    n = Convert.ToDouble(selectedVarTime, CultureInfo.InvariantCulture) * 60;
                }
            }
            else
            {
                if (selectedType == "energy")
                {
                    //BellmanFord();
                    izbor = "energija profil";
                    FibonacciHeapMetoda(izbor, vrijemePocetkaUSekundama);
                    n = Convert.ToDouble(selectedVarEnergy, CultureInfo.InvariantCulture);
                }
                else if (selectedType == "time")
                {
                    izbor = "vrijeme profil";
                    FibonacciHeapMetoda(izbor, vrijemePocetkaUSekundama);
                    n = vrijemePocetkaUSekundama+Convert.ToDouble(selectedVarTime, CultureInfo.InvariantCulture) * 60;
                }
            }

            

            //Filtiranje vrhova
            var listaVrhovaZaFilter = listaVrhova.ToList();
            //NE tezina nego 
            listaVrhovaZaFilter=listaVrhovaZaFilter.FindAll(vrh => vrh.Value.tezina < n);
            listaVrhovaZaFilter = listaVrhovaZaFilter.FindAll(vrh => vrh.Value.tezina > 0);
            
            // Create a list of coordinates from the filtered data
            var coordinates = new List<Coordinate>();
            foreach (KeyValuePair<int,Vrh> kvPair in listaVrhovaZaFilter)
            {
                Vrh tlink = kvPair.Value;
                int duljinaL = tlink.duljinaLinka;
                int numIntervals = (int)(duljinaL / 50);

                if (duljinaL > 50)
                {
                    for (int i = 0; i <= numIntervals; i++)
                    {
                        double ratio = (double)i / numIntervals;
                        double newLat = tlink.yPocetak + ratio * (tlink.yZavrsetak - tlink.yPocetak);
                        double newLon = tlink.xPocetak + ratio * (tlink.xZavrsetak - tlink.xPocetak);
                        var coord = new Coordinate(newLon, newLat);
                        coordinates.Add(coord);
                    }
                }
                else
                {
                    var coord = new Coordinate(kvPair.Value.xZavrsetak, kvPair.Value.yZavrsetak);
                    coordinates.Add(coord);
                    coord = new Coordinate(kvPair.Value.xPocetak, kvPair.Value.yPocetak);
                    coordinates.Add(coord);
                    coord = new Coordinate((kvPair.Value.xPocetak + kvPair.Value.xZavrsetak) / 2, (kvPair.Value.yPocetak + kvPair.Value.yZavrsetak) / 2);
                    coordinates.Add(coord);
                }
            }

            string contourWKT = null;

            // Create a geometry from the coordinates
            GeometryFactory geomFactory = new GeometryFactory();
            MultiPoint geometry = geomFactory.CreateMultiPointFromCoords(coordinates.ToArray());


            // Calculate the alpha shape
            //var alphaBufferDisc = 0.0000001; // Adjust this value based on your data and desired shape
            //var result = BufferDisc(geometry, alphaBufferDisc, geomFactory);


            if (KontureType=="Concave Hull")
            {
                var result = ConcaveHullv2(geometry);
                WKTWriter writer = new WKTWriter();
                contourWKT = writer.Write(result);
            }
            else if (KontureType == "Convex Hull")
            {
                var result = ConvexHull(geometry);
                WKTWriter writer = new WKTWriter();
                contourWKT = writer.Write(result);
            }
            return Content(JsonConvert.SerializeObject(contourWKT), "application/json");




            //Console.WriteLine("Alpha Shape Contour for VALUE < 1000:");
            //StreamWriter pisanje2 = new StreamWriter("konture.txt");
            //pisanje2.WriteLine(contourWKT);
            //pisanje2.Close();
            //Console.WriteLine(contourWKT);



            //Promjenit
            //string primjer = "POLYGON ((15.9769105911255 45.8049086168488, 15.9787023067474 45.8049983661053, 15.9798556566238 45.8050731570458, 15.9815776348114 45.805331185026, 15.9851396083832 45.8070363789435, 15.987269282341 45.8094856448173, 15.9864270687103 45.8122376635526, 15.9853273630142 45.8129592927882, 15.976824760437 45.8113552375854, 15.9721308946609 45.8097698215335, 15.9706717729568 45.8089509297441, 15.9709185361862 45.8084162069347, 15.9769105911255 45.8049086168488))";
            //Console.WriteLine();

        }

        //Ucitavanje podataka iz datoteke
        private void UcitajPodatke()
        {            
            //Ucitavanje podataka u listu vrhova 
            try
            {
                vEnergija.listaSusjednihLinkova = new List<int>();
                vEnergija.linkID = 1;
                vEnergija.tezinaEnergija = 0;
                vEnergija.duljinaLinka = 0;
                vEnergija.vrijemePolaskaUSekundama = 0;

                while (!citanje.EndOfStream)
                {
                    //string s = citanje.ReadLine().Replace('.', ',');
                    string s = citanje.ReadLine();
                    string[] d = s.Split(';');
                    int linkID = Convert.ToInt32(d[0]);
                    double xPocetak = Convert.ToDouble(d[1], CultureInfo.InvariantCulture);
                    double yPocetak = Convert.ToDouble(d[2], CultureInfo.InvariantCulture);
                    double xZavrsetak = Convert.ToDouble(d[3], CultureInfo.InvariantCulture);
                    double yZavrsetak = Convert.ToDouble(d[4], CultureInfo.InvariantCulture);
                    int duljinaLinka = Convert.ToInt32(d[5]);
                    int brzinaLinka = Convert.ToInt32(d[6]);
                    int ogranicenjeBrzine = Convert.ToInt32(d[7]);
                    int tipLinka = Convert.ToInt32(d[8]);
                    int zastavicaSmjera = Convert.ToInt32(d[9]);
                    double brzinaSlobodnogToka = Convert.ToDouble(d[11], CultureInfo.InvariantCulture);
                    double prosjecnaBrzina = Convert.ToDouble(d[12], CultureInfo.InvariantCulture);
                    double prosjecnaEnergija = Convert.ToDouble(d[15], CultureInfo.InvariantCulture);
                    Vrh vSvi = new Vrh
                    {
                        linkID = linkID,
                        xPocetak = xPocetak,
                        yPocetak = yPocetak,
                        xZavrsetak = xZavrsetak,
                        yZavrsetak = yZavrsetak,
                        duljinaLinka = duljinaLinka,
                        brzinaLinka = brzinaLinka,
                        ogranicenjeBrzine = ogranicenjeBrzine,
                        tipLinka = tipLinka,
                        zastavicaSmjera = zastavicaSmjera,
                        brzinaSlobodnogToka = brzinaSlobodnogToka,
                        prosjecnaBrzina = prosjecnaBrzina,
                        prosjecnaEnergija = prosjecnaEnergija
                    };
                    vSvi.listaSusjednihLinkova = new List<int>();
                    string[] susjedniID = d[10].Split('|');
                    for (int i = 0; i < susjedniID.Length; i++)
                    {
                        vSvi.listaSusjednihLinkova.Add(Convert.ToInt32(susjedniID[i]));
                        
                    }
                    vSvi.profilBrzine = new double[288];
                    string[] brzina = d[13].Split('|');
                    for (int i = 0; i < brzina.Length; i++)
                    {
                        vSvi.profilBrzine[i] = Convert.ToDouble(brzina[i], CultureInfo.InvariantCulture);
                    }
                    vSvi.profilEnergije = new double[288];
                    string[] energija = d[17].Split('|');
                    for (int i = 0; i < energija.Length; i++)
                    {
                        vSvi.profilEnergije[i] = Convert.ToDouble(energija[i], CultureInfo.InvariantCulture);
                    }
                    vEnergija.listaSusjednihLinkova.Add(vSvi.linkID);
                    vSvi.reset();
                    listaVrhova[linkID] = vSvi;
                    
                }
                vEnergija.profilBrzine = listaVrhova[233703].profilBrzine;

                //Bellman Ford
                listaVrhova.Add(vEnergija.linkID, vEnergija);
                pocetak= vEnergija;
                izbor = "BellmanFord";
                FibonacciHeapMetoda(izbor, vrijemePocetkaUSekundama);
                listaVrhova.Remove(vEnergija.linkID);


                citanje.Close();                
                podatciUcitani = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greska "+ ex.Message);
            }
        }
        public void BellmanFord()
        {
            listaVrhova.Add(vEnergija.linkID, vEnergija);
            //pocetakEnergija = vEnergija;
            izbor = "BellmanFord";
            FibonacciHeapMetoda(izbor, vrijemePocetkaUSekundama);
            listaVrhova.Remove(vEnergija.linkID);
        }

        public void FibonacciHeapMetoda(string izbor, double vrijemePocetkaUSekundama)
        {
            if (izbor != "BellmanFord")
            {
                Dictionary<int, FibonacciHeapNode<Vrh, double>> sviUHeapu = new Dictionary<int, FibonacciHeapNode<Vrh, double>>();
                heap = new FibonacciHeap<Vrh, double>(double.MaxValue);
                FibonacciHeapNode<Vrh, double> pocetakHeapNode = null;
                foreach (Vrh vReset in listaVrhova.Values)
                {
                    vReset.tezina = double.MaxValue;
                    vReset.obraden = false;
                    vReset.prethodni = null;
                    vReset.vrijemePolaskaUSekundama = double.MaxValue;
                    vReset.ukupnaDuljina = 0;
                    vReset.ukupnaEnergija = 0;
                    FibonacciHeapNode<Vrh, double> node = new FibonacciHeapNode<Vrh, double>(vReset, vReset.tezina);



                    if (vReset == pocetak)
                    {
                        vReset.tezina = 0;
                        vReset.vrijemePolaskaUSekundama = vrijemePocetkaUSekundama;
                        pocetakHeapNode = node;
                    }
                    else
                    {
                        heap.Insert(node);
                    }
                    sviUHeapu[vReset.linkID] = node;
                }
                int i = 0;
                Vrh v1;
                if (izbor == "energija prosjecna")
                {
                    while (!heap.IsEmpty())
                    {
                        FibonacciHeapNode<Vrh, double> najbolji = null;
                        if (i == 0)
                        {
                            najbolji = pocetakHeapNode;
                            i++;
                        }
                        else
                        {
                            najbolji = heap.RemoveMin();
                        }
                        najbolji.Data.obraden = true;
                        if (!heap.IsEmpty())
                        {
                            v1 = najbolji.Data;
                            foreach (int linkID in v1.listaSusjednihLinkova)
                            {
                                if (listaVrhova.ContainsKey(linkID))
                                {
                                    Vrh v2 = listaVrhova[linkID];
                                    double vrijemePutovanjaIzmeduV1iV2 = (v1.duljinaLinka) / (v1.prosjecnaBrzina) * 3.6;
                                    //double vrijemePutovanjaIzmeduV1iV2 = (v1.duljinaLinka) / (v1.prosjecnaBrzina * (1000 / 60));
                                    double vrijemeDolaskaDoV2 = v1.vrijemePolaskaUSekundama + vrijemePutovanjaIzmeduV1iV2;
                                    if (v2.obraden)
                                    {
                                        continue;
                                    }
                                    if (v2.tezina > v1.tezina + v2.prosjecnaEnergija + v1.tezinaEnergija - v2.tezinaEnergija)
                                    {
                                        FibonacciHeapNode<Vrh, double> nodeToUpdate = sviUHeapu[v2.linkID];
                                        v2.tezina = v1.tezina + v2.prosjecnaEnergija + v1.tezinaEnergija - v2.tezinaEnergija;
                                        v2.prethodni = v1;
                                        v2.vrijemePolaskaUSekundama = vrijemeDolaskaDoV2;
                                        heap.DecreaseKey(nodeToUpdate, v2.tezina);
                                        //za ispis
                                        v2.ukupnaDuljina = v1.ukupnaDuljina + v1.duljinaLinka;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (izbor == "vrijeme prosjecno")
                {
                    while (!heap.IsEmpty())
                    {
                        FibonacciHeapNode<Vrh, double> najbolji = null;
                        if (i == 0)
                        {
                            najbolji = pocetakHeapNode;
                            i++;
                        }
                        else
                        {
                            najbolji = heap.RemoveMin();
                        }
                        najbolji.Data.obraden = true;
                        if (!heap.IsEmpty())
                        {
                            v1 = najbolji.Data;
                            foreach (int linkID in v1.listaSusjednihLinkova)
                            {
                                if (listaVrhova.ContainsKey(linkID))
                                {
                                    Vrh v2 = listaVrhova[linkID];
                                    double vrijemePutovanjaIzmeduV1iV2 = (v1.duljinaLinka) / (v1.prosjecnaBrzina)*3.6;
                                    //double vrijemePutovanjaIzmeduV1iV2 = (v1.duljinaLinka /v1.prosjecnaBrzina)*60/1000;
                                    double vrijemeDolaskaDoV2 = v1.vrijemePolaskaUSekundama + vrijemePutovanjaIzmeduV1iV2;
                                    if (v2.obraden)
                                    {
                                        continue;
                                    }
                                    //I ovo mi se čini da je bilo krivo
                                    //if (v2.tezina > v1.tezina + vrijemeDolaskaDoV2)
                                    if (v2.tezina > vrijemeDolaskaDoV2)
                                    {
                                        FibonacciHeapNode<Vrh, double> nodeToUpdate = sviUHeapu[v2.linkID];
                                        v2.tezina = vrijemeDolaskaDoV2;
                                        v2.prethodni = v1;
                                        v2.vrijemePolaskaUSekundama = vrijemeDolaskaDoV2;
                                        heap.DecreaseKey(nodeToUpdate, v2.tezina);
                                        //za ispis
                                        v2.ukupnaEnergija = v1.ukupnaEnergija + v1.prosjecnaEnergija;
                                        v2.ukupnaDuljina = v1.ukupnaDuljina + v1.duljinaLinka;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (izbor == "energija profil")
                {
                    while (!heap.IsEmpty())
                    {
                        FibonacciHeapNode<Vrh, double> najbolji = null;
                        if (i == 0)
                        {
                            najbolji = pocetakHeapNode;
                            i++;
                        }
                        else
                        {
                            najbolji = heap.RemoveMin();
                        }
                        najbolji.Data.obraden = true;
                        if (!heap.IsEmpty())
                        {
                            v1 = najbolji.Data;
                            foreach (int linkID in v1.listaSusjednihLinkova)
                            {
                                if (listaVrhova.ContainsKey(linkID))
                                {
                                    Vrh v2 = listaVrhova[linkID];
                                    double vrijemePutovanjaIzmeduV1iV2 = double.MaxValue;
                                    int indeksProfila=0;
                                    if (v1.vrijemePolaskaUSekundama != double.MaxValue)
                                    {
                                        indeksProfila = Convert.ToInt32((v1.vrijemePolaskaUSekundama / 60.0) / 5.0);
                                        vrijemePutovanjaIzmeduV1iV2 = (v1.duljinaLinka) / (v1.profilBrzine[indeksProfila]) * 3.6;
                                    }

                                    double vrijemeDolaskaDoV2 = v1.vrijemePolaskaUSekundama + vrijemePutovanjaIzmeduV1iV2;

                                    if (v2.obraden)
                                    {
                                        continue;
                                    }
                                    if (v2.tezina > v1.tezina + v2.profilEnergije[indeksProfila] + v1.tezinaEnergija - v2.tezinaEnergija)
                                    {
                                        FibonacciHeapNode<Vrh, double> nodeToUpdate = sviUHeapu[v2.linkID];
                                        v2.tezina = v1.tezina + v2.profilEnergije[indeksProfila] + v1.tezinaEnergija - v2.tezinaEnergija;
                                        v2.prethodni = v1;
                                        v2.vrijemePolaskaUSekundama = vrijemeDolaskaDoV2;
                                        heap.DecreaseKey(nodeToUpdate, v2.tezina);
                                        //za ispis
                                        v2.ukupnaDuljina = v1.ukupnaDuljina + v1.duljinaLinka;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (izbor == "vrijeme profil")
                {
                    int cntHeap = 0;
                    while (!heap.IsEmpty())
                    {
                        FibonacciHeapNode<Vrh, double> najbolji = null;
                        cntHeap++;
                        if (i == 0)
                        {
                            najbolji = pocetakHeapNode;
                            i++;
                        }
                        else
                        {
                            najbolji = heap.RemoveMin();
                        }
                        najbolji.Data.obraden = true;
                        if (!heap.IsEmpty())
                        {
                            v1 = najbolji.Data;
                            foreach (int linkID in v1.listaSusjednihLinkova)
                            {
                                if (listaVrhova.ContainsKey(linkID))
                                {
                                    Vrh v2 = listaVrhova[linkID];
                                    double vrijemePutovanjaIzmeduV1iV2 = double.MaxValue;
                                    if (v1.vrijemePolaskaUSekundama != double.MaxValue)
                                    {
                                        int indeksProfila = Convert.ToInt32((v1.vrijemePolaskaUSekundama / 60.0) / 5.0);
                                        vrijemePutovanjaIzmeduV1iV2 = (v1.duljinaLinka) / (v1.profilBrzine[indeksProfila]) * 3.6;
                                    }


                                     
                                    double vrijemeDolaskaDoV2 = v1.vrijemePolaskaUSekundama + vrijemePutovanjaIzmeduV1iV2;

                                    if (v2.obraden)
                                    {
                                        continue;
                                    }
                                    //I ovo mi se čini da je bilo krivo
                                    //if (v2.tezina > v1.tezina + vrijemeDolaskaDoV2)
                                    if (v2.tezina > vrijemeDolaskaDoV2)
                                    {
                                        FibonacciHeapNode<Vrh, double> nodeToUpdate = sviUHeapu[v2.linkID];
                                        v2.tezina = vrijemeDolaskaDoV2;
                                        v2.prethodni = v1;
                                        v2.vrijemePolaskaUSekundama = vrijemeDolaskaDoV2;
                                        heap.DecreaseKey(nodeToUpdate, v2.tezina);
                                        //za ispis
                                        v2.ukupnaEnergija = v1.ukupnaEnergija + v1.prosjecnaEnergija;
                                        v2.ukupnaDuljina = v1.ukupnaDuljina + v1.duljinaLinka;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Dictionary<int, FibonacciHeapNode<Vrh, double>> sviUHeapu = new Dictionary<int, FibonacciHeapNode<Vrh, double>>();
                heap = new FibonacciHeap<Vrh, double>(double.MaxValue);
                FibonacciHeapNode<Vrh, double> pocetakHeapNode = null;
                foreach (Vrh vReset in listaVrhova.Values)
                {
                    vReset.tezina = double.MaxValue;
                    vReset.obraden = false;
                    vReset.prethodni = null;
                    vReset.vrijemePolaskaUSekundama = 0;
                    FibonacciHeapNode<Vrh, double> node = new FibonacciHeapNode<Vrh, double>(vReset, vReset.tezina);
                    if (vReset == vEnergija)
                    {
                        vReset.tezina = 0;
                        vReset.vrijemePolaskaUSekundama = vrijemePocetkaUSekundama;
                        pocetakHeapNode = node;
                    }
                    else
                    {
                        heap.Insert(node);
                    }
                    sviUHeapu[vReset.linkID] = node;
                }
                int i = 0;
                Vrh v1;
                if (vrijemePocetkaUSekundama == 0)
                {
                    while (!heap.IsEmpty())
                    {
                        FibonacciHeapNode<Vrh, double> najbolji = null;
                        if (i == 0)
                        {
                            najbolji = pocetakHeapNode;
                            i++;
                        }
                        else
                        {
                            najbolji = heap.RemoveMin();
                        }
                        najbolji.Data.obraden = true;
                        if (!heap.IsEmpty())
                        {
                            v1 = najbolji.Data;
                            foreach (int linkID in v1.listaSusjednihLinkova)
                            {
                                if (listaVrhova.ContainsKey(linkID))
                                {
                                    Vrh v2 = listaVrhova[linkID];
                                    if (v2.tezinaEnergija > v1.tezinaEnergija + v2.prosjecnaEnergija)
                                    {
                                        FibonacciHeapNode<Vrh, double> nodeToUpdate = sviUHeapu[v2.linkID];
                                        v2.tezinaEnergija = v1.tezinaEnergija + v2.prosjecnaEnergija;
                                        heap.DecreaseKey(nodeToUpdate, v2.tezina);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    while (!heap.IsEmpty())
                    {
                        FibonacciHeapNode<Vrh, double> najbolji = null;
                        if (i == 0)
                        {
                            najbolji = pocetakHeapNode;
                            i++;
                        }
                        else
                        {
                            najbolji = heap.RemoveMin();
                        }
                        najbolji.Data.obraden = true;
                        if (!heap.IsEmpty())
                        {
                            v1 = najbolji.Data;
                            foreach (int linkID in v1.listaSusjednihLinkova)
                            {
                                if (listaVrhova.ContainsKey(linkID))
                                {                                
                                    Vrh v2 = listaVrhova[linkID];
                                    double vrijemePutovanjaIzmeduV1iV2 = double.MaxValue;
                                    int indeksProfila = 0;
                                    if (v1.vrijemePolaskaUSekundama != double.MaxValue)
                                    {
                                        indeksProfila = Convert.ToInt32((v1.vrijemePolaskaUSekundama / 60.0) / 5.0);
                                        vrijemePutovanjaIzmeduV1iV2 = (v1.duljinaLinka) / (v1.profilBrzine[indeksProfila]) * 3.6;
                                    }

                                    double vrijemeDolaskaDoV2 = v1.vrijemePolaskaUSekundama + vrijemePutovanjaIzmeduV1iV2;


                                    if (v2.tezinaEnergija > v1.tezinaEnergija + v2.profilEnergije[indeksProfila])
                                    {
                                        FibonacciHeapNode<Vrh, double> nodeToUpdate = sviUHeapu[v2.linkID];
                                        v2.tezinaEnergija = v1.tezinaEnergija + v2.profilEnergije[indeksProfila];
                                        v2.vrijemePolaskaUSekundama = vrijemeDolaskaDoV2;
                                        heap.DecreaseKey(nodeToUpdate, v2.tezina);
                                    }


                                }
                            }
                        }
                    }
                }
            }
        }


        //Metode za pronalazenje najblizeg vrha
        private Vrh PronadiNajbliziLink(double longitude, double latitude)
        {
            Vrh v3 = null;            
            double minUdaljenost = double.MaxValue;
            foreach (Vrh v4 in listaVrhova.Values)
            {
                double udaljenost = getDistanceFromPointToClosestPointOnLine(v4.xPocetak, v4.yPocetak, v4.xZavrsetak, v4.yZavrsetak, longitude, latitude);
                if (udaljenost < minUdaljenost)
                {
                    minUdaljenost = udaljenost;
                    v3 = v4;
                }
            }
            return v3;
        }        
        public static double getDistanceFromPointToClosestPointOnLine(double lx1, double ly1, double lx2, double ly2, double px, double py)
        {
            //Vektorski racuna najblizu posctku
            double[] vec_l1P = new double[2] { px - lx1, py - ly1 };
            double[] vec_l1l2 = new double[2] { lx2 - lx1, ly2 - ly1 };

            double mag = Math.Pow(vec_l1l2[0], 2) + Math.Pow(vec_l1l2[1], 2);
            double prod = vec_l1P[0] * vec_l1l2[0] + vec_l1P[1] * vec_l1l2[1];
            double normDist = prod / mag;

            double clX = lx1 + vec_l1l2[0] * normDist;
            double clY = ly1 + vec_l1l2[1] * normDist;

            //Ukoliko je projkekcije izvan granica linije, korigirati na najblizu tocku na liniji
            double minLX = Math.Min(lx1, lx2);
            double minLY = Math.Min(ly1, ly2);
            double maxLX = Math.Max(lx1, lx2);
            double maxLY = Math.Max(ly1, ly2);
            if (clX < minLX)
            {
                clX = minLX;
            }
            if (clY < minLY)
            {
                clY = minLY;
            }

            if (clX > maxLX)
            {
                clX = maxLX;
            }
            if (clY > maxLY)
            {
                clY = maxLY;
            }
            //Vrati zracnu udaljenost u metrima izmedu najblize tocke na liniji i zadane tocke
            return airalDistHaversine(px, py, clX, clY);
        }

        //Pomocna metoda koja racuna zračunu udaljenost u metrima između dvije lokacije koristeći model zemlje kao kugla
        public static double airalDistHaversine(double lon1, double lat1, double lon2, double lat2)
        {

            double R = 6371000; // metres
            double phi1 = lat1 * Math.PI / 180; // φ, λ in radians
            double phi2 = lat2 * Math.PI / 180;
            double deltaphi = (lat2 - lat1) * Math.PI / 180;
            double deltaLambda = (lon2 - lon1) * Math.PI / 180;

            double a = Math.Sin(deltaphi / 2) * Math.Sin(deltaphi / 2) +
                      Math.Cos(phi1) * Math.Cos(phi2) *
                      Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            double d = R * c;
            return d;
        }

        //Crtanje???

        // Calculate the alpha shape
        private static Geometry AlphaShape(Geometry input, double alpha, GeometryFactory geomFactory)
        {
            var convexHull = input.ConvexHull();
            var buffer = convexHull.Buffer(alpha);
            var intersection = convexHull.Intersection(buffer);
            return intersection;
        }

        // Calculate the buffer disc
        private static Geometry BufferDisc(Geometry input, double alpha, GeometryFactory geomFactory)
        {
            var buffer = input.Buffer(alpha);
            return buffer;
        }

        private static Geometry ConcaveHullv2(Geometry input)
        {
            return ConcaveHull.ConcaveHullByLength(input, 0.0015);
        }

        // Calculate the ConvexHull hull (alpha shape with larger alpha)
        private static Geometry ConvexHull(Geometry input)
        {
            var buffer = input.ConvexHull();
            return buffer;
        }
    }
}
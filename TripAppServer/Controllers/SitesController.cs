using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TripAppServer;
using TripAppServer.Handlers;
using TripAppServer.Models;
using System.Text;
using Newtonsoft.Json;
using System.Web.Helpers;
using Newtonsoft.Json.Linq;

namespace TripAppServer.Controllers
{
    public class SitesController : ApiController
    {
        ResponseHandler rh = new ResponseHandler();
        private const int NUMBER_OF_SITES_IN_ROUTE = 5;
        private const String PLACE_ID_REPLACEMENT = "<place_id>";
        private const String GOOGLE_PLACES_API_KEY = "AIzaSyApCIPpdSZZHQ6E7EFp9SpnJ1OGF8bLRrQ";
        private const String PLACE_DETAILS_REQUST = "https://maps.googleapis.com/maps/api/place/details/json?placeid=" + PLACE_ID_REPLACEMENT + "&key=" + GOOGLE_PLACES_API_KEY;
        private const String START_TRIP_TIME = "1000";
        private const String END_TRIP_TIME = "2000";
        private const int DAY_LENGTH = 2400;    
        private const int VISIT_LENGTH_IN_HOURS = 200; // 2 hours.

        // Returns all sites in Smart Trip DB.
        [Route("api/sites/getAllSites")]
        [HttpGet]
        public HttpResponseMessage GetSites()
        {
            using (smart_trip_db se = new smart_trip_db())
            {
                var sites = se.sites.ToList();
                return rh.HandleResponse(new { sites = sites });
            }
        }

        // Returns all sites types in Smart Trip DB.
        [Route("api/sites/getAllSitesTypes")]
        [HttpGet]
        public HttpResponseMessage GetSitesTypes()
        {
            using (smart_trip_db se = new smart_trip_db())
            {
                var sitesTypes = se.sites_types.ToList();
                return rh.HandleResponse(new { sitesTypes = sitesTypes });
            }
        }

        // Returns full site info by the site google ID.
        [Route("api/sites/getSiteFullInfo")]
        [HttpPost]
        public HttpResponseMessage GetSiteFullInfo(SiteModel siteId)
        {
            String url = PLACE_DETAILS_REQUST;
            url = url.Replace(PLACE_ID_REPLACEMENT, siteId.id);
            SiteJasonDetails site = getSiteHttpsRequest(url);
            return rh.HandleResponse(new { site = site });     
        }

        // Returns DB site info by the site google ID.
        [Route("api/sites/getSiteDbInfo")]
        [HttpPost]
        public HttpResponseMessage GetSiteDbInfo(SiteModel siteId)
        {
            using (smart_trip_db se = new smart_trip_db())
            {
                var site = se.sites
                   .Where(s => s.google_id != null && s.google_id.Contains(siteId.id))
                   .ToList();

                return rh.HandleResponse(new { site = site });
            }
        }

        // Returns site info ID.
        [Route("api/sites/getSitesByType")]
        [HttpPost]
        public HttpResponseMessage GetSitesByType(SiteModel typeId)
        {
            using (smart_trip_db se = new smart_trip_db())
            {
                var sites = se.sites
                   .Where(s => s.types != null && s.types.Contains(typeId.id.ToString()))
                   .ToList();

                return rh.HandleResponse(new { sites = sites });
            }
        }

        // Returns random route of sites from the DB.
        [Route("api/sites/getRandomRoute")]
        [HttpGet]
        public HttpResponseMessage GetRandomRoute()
        {
            using (smart_trip_db se = new smart_trip_db())
            {
                var sites = se.sites.ToList();
                List<sites> newRoute = new List<sites>();
                Random rnd = new Random();

                int i;
                for (i = 0; i < NUMBER_OF_SITES_IN_ROUTE; i++)
                {
                    int siteIndex = rnd.Next(0, sites.Count);
                    newRoute.Add(sites.ElementAt(siteIndex));
                    sites.RemoveAt(siteIndex);
                }

                return rh.HandleResponse(new { route = newRoute });
            }
        }

        // Returns smart route sites from the DB.
        [Route("api/sites/getSmartRoute")]
        [HttpGet]
        public HttpResponseMessage GetSmartRoute()
        {
            String currentTime = START_TRIP_TIME;
            Dictionary<String, SiteJasonDetails> allSitesFullInfo = getAllSitesFullInfo();
            List<sites> allSitesDbInfo = getAllSitesDbInfo();
            List<sites> newRoute = new List<sites>();
            Random rnd = new Random();

            int i;
            for (i = 0; i < NUMBER_OF_SITES_IN_ROUTE; i++)
            {                
                // Get all available sites full info.
                List<sites> availableSites = new List<sites>(allSitesDbInfo);
                availableSites = getAvailableSitesFullInfo(allSitesDbInfo, allSitesFullInfo, newRoute, currentTime);

                // Rand a site from avaialable sites for a visit and add for the new route.
                int siteIndex = rnd.Next(0, availableSites.Count);
                newRoute.Add(availableSites.ElementAt(siteIndex));
                currentTime = getTimeForNextSite(currentTime);
            }

            return rh.HandleResponse(new { route = newRoute });
        }

        // Get all sites full info from google.
        private Dictionary<String, SiteJasonDetails> getAllSitesFullInfo()
        {
            using (smart_trip_db se = new smart_trip_db())
            {
                List<sites> sites = se.sites.ToList();
                Dictionary<String, SiteJasonDetails> res = new Dictionary<String, SiteJasonDetails>();

                foreach (sites currentSite in sites)
                {
                    // Perform https get request to get place details.
                    String url = PLACE_DETAILS_REQUST;
                    url = url.Replace(PLACE_ID_REPLACEMENT, currentSite.google_id);
                    SiteJasonDetails detailedSite = getSiteHttpsRequest(url);
                    res.Add(detailedSite.result.place_id, detailedSite);
                }

                return res;
            }
        }

        // Get all sites info from the DB.
        private List<sites> getAllSitesDbInfo()
        {
            using (smart_trip_db se = new smart_trip_db())
            {
                var sites = se.sites.ToList();
                return sites;
            }
        }

        // Get all available sites for the current time and the were not selected yet for the new route.
        private List<sites> getAvailableSitesFullInfo(List<sites> sitesDbInfo, Dictionary<String, SiteJasonDetails> sitesFullInfo, List<sites> newRoute, string currentTime)
        {
            // Remove all sites that were already assigned to previous hours in the new route.
            foreach (sites siteInNewRoute in newRoute)
            {
                foreach (sites siteInDb in sitesDbInfo.ToList())
                {
                    if (siteInDb.google_id.Equals(siteInNewRoute.google_id))
                    {
                        sitesDbInfo.Remove(siteInDb);
                    }
                }
            }

            // Remove all closed sites for the current time.
            foreach (sites siteInDb in sitesDbInfo.ToList())
            {
                bool isOpen = checkIfSiteIsOpen(siteInDb, sitesFullInfo, currentTime);
                if (!isOpen)
                {
                    sitesDbInfo.Remove(siteInDb);

                }
            }
            return sitesDbInfo;          
        }

        // Check if site is open for a visit in the current time.
        private bool checkIfSiteIsOpen(sites siteInDb, Dictionary<String, SiteJasonDetails> sitesFullInfo, String currentTime)
        {
            // Get current day number.
            DateTime ClockInfoFromSystem = DateTime.Now;
            int day = (int)ClockInfoFromSystem.DayOfWeek;

            // Set open and close times for the site in the specific day.
            try
            {
                Close siteCloseTime = sitesFullInfo[siteInDb.google_id].result.opening_hours.periods[day].close;
                Open siteOpenTime = sitesFullInfo[siteInDb.google_id].result.opening_hours.periods[day].open;
                int openTime = int.Parse(siteOpenTime.time);
                int closedTime = int.Parse(siteCloseTime.time);

                // We check if close time is below 1000, because it means that the site is closed after midnight.
                if (closedTime<=999)
                {
                    closedTime += DAY_LENGTH;
                }

                // Set current time and check if the current time and visi length of 2 hours is matched for the site.
                int current = int.Parse(currentTime);
                if (current >= openTime && (current + VISIT_LENGTH_IN_HOURS) <= closedTime)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                // Sites with not opening hours are always open - "central park" for example.
                return true;
            }
        }

        // Get site https get place details request.
        private SiteJasonDetails getSiteHttpsRequest(string url)
        {
            var request = WebRequest.Create(url);
            request.ContentType = "application/json; charset=utf-8";
            string text;
            var response = (HttpWebResponse)request.GetResponse();
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                text = sr.ReadToEnd();
                var result = JsonConvert.DeserializeObject<SiteJasonDetails>(text);
                return result;
            }
        }

        // Get time for next site visit.
        private String getTimeForNextSite(String time)
        {
            String nextTime = time;
            int digit = (int)Char.GetNumericValue(time[1]) + 2;
            time = time.Remove(2, 1);
            time = time.Insert(1, digit.ToString());
            return time;
        }
    }
}


/*using (smart_trip_db se = new smart_trip_db())
{
    var sites = se.sites.ToList();

    foreach (sites site in sites)
    {
        String url = PLACE_DETAILS_REQUST;
        url = url.Replace(PLACE_ID_REPLACEMENT, site.google_id);
        SiteJasonDetails detailedSite = getSiteHttpsRequest(url);
    }

    return rh.HandleResponse(new { sites = sites });
}*/

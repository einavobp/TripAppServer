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
        private const int VISIT_LENGTH_IN_HOURS = 2;

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
                   .Where(s => s.types != null && s.google_id.Contains(siteId.id))
                   .ToList();

                return rh.HandleResponse(new { site = site });
            }
        }

        // Returns site info ID.
        [Route("api/sites/getSitesByType")]
        [HttpPost]
        public HttpResponseMessage GetSitesByType(SiteModel siteId)
        {
            using (smart_trip_db se = new smart_trip_db())
            {
                var sites = se.sites
                   .Where(s => s.types != null && s.types.Contains(siteId.id.ToString()))
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

            List<sites> newRoute = new List<sites>();
            Random rnd = new Random();

            int i;
            for (i = 0; i < NUMBER_OF_SITES_IN_ROUTE; i++)
            {
                List<sites> sites = getAvailableSites(newRoute, currentTime);
                int siteIndex = rnd.Next(0, sites.Count);
                newRoute.Add(sites.ElementAt(siteIndex));
                currentTime = getTimeForNextSite(currentTime);
            }

            return rh.HandleResponse(new { route = newRoute });
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

        // Get all available sites for the current time and the were not selected yet for the new route.
        private List<sites> getAvailableSites(List<sites> newRoute, string currentTime)
        {
            using (smart_trip_db se = new smart_trip_db())
            {
                var sites = se.sites.ToList();
                
                // Remove all sites that were already assigned to previous hours in the new route.
                foreach(sites siteInNewRoute in newRoute)
                {
                    foreach (sites siteInDb in sites.ToList())
                    {
                        if(siteInDb.google_id.Equals(siteInNewRoute.google_id))
                        {
                            sites.Remove(siteInDb);
                        }
                    }
                }

                // Remove all closed sites for the current time.
                foreach (sites siteInDb in sites.ToList())
                {
                    bool isOpen = checkIfSiteIsOpen(siteInDb, currentTime);
                    if(!isOpen)
                    {
                        sites.Remove(siteInDb);
                    }    
                }

                return sites;
            }
        }

        // Check if site is open for a visit in the current time.
        private bool checkIfSiteIsOpen(sites siteInDb, string currentTime)
        {
            // Get current day number.
            DateTime ClockInfoFromSystem = DateTime.Now;
            int day = (int)ClockInfoFromSystem.DayOfWeek;

            // Perform https get request to get place details.
            String url = PLACE_DETAILS_REQUST;
            url = url.Replace(PLACE_ID_REPLACEMENT, siteInDb.google_id);
            SiteJasonDetails detailedSite = getSiteHttpsRequest(url);

            // Set open and close times for the site in the specific day.
            try
            {
                Close siteCloseTime = detailedSite.result.opening_hours.periods[day].close;
                Open siteOpenTime = detailedSite.result.opening_hours.periods[day].open;
                int openTime = int.Parse(siteOpenTime.time);
                int closedTime = int.Parse(siteCloseTime.time);

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
            catch(Exception)
            {
                // Sites with not opening hours are always open - "central park" for example.
                return true;
            }
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

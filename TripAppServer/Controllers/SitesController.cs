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
using System.Text.RegularExpressions;

namespace TripAppServer.Controllers
{
    public class SitesController : ApiController
    {
        ResponseHandler rh = new ResponseHandler();
        private const int NUMBER_OF_SITES_IN_ROUTE = 5;
        private const String PLACE_ID_REPLACEMENT = "<place_id>";
        private const String GOOGLE_PLACES_API_KEY = "AIzaSyApCIPpdSZZHQ6E7EFp9SpnJ1OGF8bLRrQ";
        private const String PLACE_DETAILS_REQUST = "https://maps.googleapis.com/maps/api/place/details/json?placeid=" + PLACE_ID_REPLACEMENT + "&key=" + GOOGLE_PLACES_API_KEY;
        private const String START_TRIP_TIME = "10:00";
        private const String END_TRIP_TIME = "20:00";
        private const String CLOSED = "Closed";
        private const String OPEN_24_HOURS = "Open 24 hours";
        private const int MINUTES_IN_ONE_HOUR = 1 * 60;
        private const int MINUTES_IN_ONE_DAY = 24 * 60;
        private const int VISIT_LENGTH_IN_MINUTES = 2 * 60;

        // --------------------------------- Requests from client --------------------------------- //

        // Returns all sites in Smart Trip DB.
        [Route("api/sites/getAllSites")]
        [HttpGet]
        public HttpResponseMessage getAllSites()
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                var sites = se.sites.ToList();
                return rh.HandleResponse(new { sites = sites });
            }
        }

        // Returns all sites types in Smart Trip DB.
        [Route("api/sites/getAllSitesTypes")]
        [HttpGet]
        public HttpResponseMessage getSitesTypes()
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                var sitesTypes = se.sites_types.ToList();
                return rh.HandleResponse(new { sitesTypes = sitesTypes });
            }
        }

        // Returns DB site info by the site DB id.
        [Route("api/sites/getSiteGeneralInfo")]
        [HttpPost]
        public HttpResponseMessage getSiteGeneralInfo(int id)
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                var site = se.sites
                   .Where(s => s.id != null && s.id == id)
                   .ToList();

                return rh.HandleResponse(new { site = site });
            }
        }

        // Returns site opening hours by the site DB id.
        [Route("api/sites/getSiteOpeningHours")]
        [HttpPost]
        public HttpResponseMessage getSiteOpeningHours(int id)
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                var site = se.sites_opening_hours
                   .Where(s => s.id == id)
                   .ToList();

                return rh.HandleResponse(new { site = site });
            }
        }

        // Returns sites by type id.
        [Route("api/sites/getSitesByTypeId")]
        [HttpPost]
        public HttpResponseMessage getSitesByTypeId(int id)
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                var sites = se.sites
                   .Where(s => s.types != null && s.types.Contains(id.ToString()))
                   .ToList();

                return rh.HandleResponse(new { sites = sites });
            }
        }

        // Returns full site info by the site google ID.
        [Route("api/sites/getSiteFullInfoByGoogleId")]
        [HttpPost]
        public HttpResponseMessage getSiteFullInfoByGoogleId(String googleId)
        {
            String url = PLACE_DETAILS_REQUST;
            url = url.Replace(PLACE_ID_REPLACEMENT, googleId);
            SiteJasonDetails site = getSiteHttpsRequest(url);
            return rh.HandleResponse(new { site = site });
        }

        // Returns smart route sites from the DB.
        [Route("api/sites/getSmartRoute")]
        [HttpGet]
        public HttpResponseMessage GetSmartRoute()
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                String currentTime = START_TRIP_TIME;
                List<sites> allSitesDbInfo = se.sites.ToList();
                List<sites> newRoute = new List<sites>();
                Random rnd = new Random();

                int i;
                for (i = 0; i < NUMBER_OF_SITES_IN_ROUTE; i++)
                {
                    // Get all available sites full info.
                    List<sites> availableSites = new List<sites>(allSitesDbInfo);
                    availableSites = getAvailableSites(se, availableSites, newRoute, currentTime);

                    // Rand a site from avaialable sites for a visit and add for the new route.
                    int siteIndex = rnd.Next(0, availableSites.Count);
                    newRoute.Add(availableSites.ElementAt(siteIndex));
                    currentTime = getTimeForNextSite(currentTime);
                }

                return rh.HandleResponse(new { route = newRoute });
            }
        }

        // --------------------------------- Server internal methods --------------------------------- //

        // Get all sites full info from google.
        private Dictionary<String, SiteJasonDetails> getAllSitesFullInfo()
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
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
            String pattern = "\\d+";
            Regex rgx = new Regex(pattern);
            MatchCollection mc = Regex.Matches(time, pattern);
            String hour = mc[0].Value;
            String minutes = mc[1].Value;
            String newHour = (int.Parse(hour) + 2).ToString();
            return newHour+":"+ minutes;
        }

        // Get all available sites for the current time and those who were not selected yet for the new route.
        private List<sites> getAvailableSites(smart_trip_dbEntities se, List<sites> sitesDbInfo, List<sites> newRoute, string currentTime)
        {
            // Remove all sites that were already assigned to previous hours in the new route.
            foreach (sites siteInNewRoute in newRoute)
            {
                foreach (sites siteInDb in sitesDbInfo.ToList())
                {
                    if (siteInDb.id == siteInNewRoute.id)
                    {
                        sitesDbInfo.Remove(siteInDb);
                    }
                }
            }

            // Remove all closed sites for the current time.
            foreach (sites siteInDb in sitesDbInfo.ToList())
            {
                bool isOpen = checkIfSiteIsOpen(se, siteInDb, currentTime);
                if (!isOpen)
                {
                    sitesDbInfo.Remove(siteInDb);
                }
            }
            return sitesDbInfo;
        }

        // Check if site is open for a visit in the current time.
        private bool checkIfSiteIsOpen(smart_trip_dbEntities se, sites siteInDb, String currentTime)
        {
            // Get current day number.
            String openingHours = getSiteHours(se, (int)siteInDb.id);
            
            if(openingHours.Equals(OPEN_24_HOURS))
            {
                return true;
            }
            else if(openingHours.Equals(CLOSED))
            {
                return false;
            }
            else
            {
                return currentTimeInRange(currentTime, openingHours);
            }
        }

        // Get site opeming hours from today.
        private string getSiteHours(smart_trip_dbEntities se, int id)
        {

            DateTime ClockInfoFromSystem = DateTime.Now;
            int day = (int)ClockInfoFromSystem.DayOfWeek;
            string openingHours = null;

            switch(day)
            {
                case 0: // Sunday.
                    openingHours = (from h in se.sites_opening_hours where h.id == id select h.sunday).FirstOrDefault();
                    break;
                case 1: // Monday.
                    openingHours = (from h in se.sites_opening_hours where h.id == id select h.monday).FirstOrDefault();
                    break;
                case 2: // Tueday.
                    openingHours = (from h in se.sites_opening_hours where h.id == id select h.tuesday).FirstOrDefault();
                    break;
                case 3: // Wednesday. 
                    openingHours = (from h in se.sites_opening_hours where h.id == id select h.wednesday).FirstOrDefault();
                    break;
                case 4: // Thursday.
                    openingHours = (from h in se.sites_opening_hours where h.id == id select h.thursday).FirstOrDefault();
                    break;
                case 5: // Friday.
                    openingHours = (from h in se.sites_opening_hours where h.id == id select h.friday).FirstOrDefault();
                    break;
                case 6: // Saturday.
                    openingHours = (from h in se.sites_opening_hours where h.id == id select h.saturday).FirstOrDefault();
                    break;
            }

            return openingHours;
        }

        // Get opening hours and check if the current time is in the range (opening hours can containts a number of ranges sepearated)
        private bool currentTimeInRange(String currentTime, String openingHours)
        {
            String[] openRanges = openingHours.Split(',');
            bool endOfCheck = false, result = false;

            for(int i=0; i<openRanges.Length && !endOfCheck; i++)
            {
                result = checkIfCurrentTimeInSingleRange(currentTime, openRanges[i]);
                if(result)
                {
                    endOfCheck = true;
                }
            }

            return result;     
        }

        // Get opening hours single range and check if the current time is in the range.
        private bool checkIfCurrentTimeInSingleRange(String currentTime, String singleHoursRange)
        {
            String pattern = "\\d+:\\d+";
            Regex rgx = new Regex(pattern);
            MatchCollection mc = Regex.Matches(singleHoursRange, pattern);
            String start = mc[0].Value;
            String end = mc[1].Value;

            // Get counter for in minutes for the relevant times.
            int startTimeCounter = int.Parse(start.Split(':')[0]) * MINUTES_IN_ONE_HOUR + int.Parse(start.Split(':')[1]);
            int endTimeCounter = int.Parse(end.Split(':')[0]) * MINUTES_IN_ONE_HOUR + int.Parse(end.Split(':')[1]);
            int currentTimeCounter = int.Parse(currentTime.Split(':')[0]) * MINUTES_IN_ONE_HOUR + int.Parse(currentTime.Split(':')[1]);

            if(endTimeCounter<startTimeCounter)
            {
                endTimeCounter += MINUTES_IN_ONE_DAY;
            }

            // Check if a visit is possible.
            if((startTimeCounter <= currentTimeCounter) && ((currentTimeCounter + VISIT_LENGTH_IN_MINUTES) <= endTimeCounter))
            {
                return true;
            }
            return false;
        }
    }
}


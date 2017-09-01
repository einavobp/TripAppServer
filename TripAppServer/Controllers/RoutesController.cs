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
    public class RoutesController : ApiController
    {
        ResponseHandler rh = new ResponseHandler();
        private const int NUMBER_OF_SITES_IN_ROUTE = 5;
        private const String START_TRIP_TIME = "10:00";
        private const String END_TRIP_TIME = "20:00";
        private const String CLOSED = "Closed";
        private const String OPEN_24_HOURS = "Open 24 hours";
        private const String ROUTE_FINDER_ERROR_MEASSAGE = "Could not calculate route with the user perfernces.";
        private const int MINUTES_IN_ONE_HOUR = 1 * 60;
        private const int MINUTES_IN_ONE_DAY = 24 * 60;
        private const int VISIT_LENGTH_IN_MINUTES = 2 * 60;

        // --------------------------------- Requests from client --------------------------------- //      

        // Returns route by route id.
        [Route("api/routes/getRoute/{routeId}")]
        [HttpPost]
        public HttpResponseMessage getRoute(int routeId)
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                var route = se.routes.Where(s => s.id == routeId).ToList();
                return rh.HandleResponse(new { route = route });
            }
        }

        // Returns smart route sites from the DB.
        [Route("api/routes/calcualte")]
        [HttpPost]
        public HttpResponseMessage calcualte(RouteRequest userRequest)
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                String currentTime = userRequest.startTime;
                int numberOfSitesInRoute = getNumberOfSites(currentTime, userRequest.endTime);
                List<sites> sitesByUserPerferances = se.sites.Where(s => (s.compositions != null && s.compositions.Contains(userRequest.compositionId.ToString())) && (s.seasons != null && s.seasons.Contains(userRequest.seasonId.ToString())) && (s.city_id != null && s.city_id== userRequest.cityId)).ToList();
                List<String> hoursPerSite = new List<String>();
                if (sitesByUserPerferances.Count == 0)
                {
                    return rh.HandleResponse(new { HttpStatusCode.BadRequest, failed_calculation = ROUTE_FINDER_ERROR_MEASSAGE } );
                }
                else
                {
                    List<sites> newRoute = new List<sites>();
                    Random rnd = new Random();
                    bool endOfCalculation=false;
                    hoursPerSite.Add("Starts: " + currentTime);

                    int i;
                    for (i = 0; i < numberOfSitesInRoute && !endOfCalculation; i++)
                    {
                        // Get all available sites full info.
                        List<sites> availableSites = new List<sites>(sitesByUserPerferances);
                        availableSites = getAvailableSites(se, availableSites, newRoute, currentTime);

                        if (availableSites.Count == 0)
                        {
                            endOfCalculation = true;
                        }
                        else
                        {
                            // Rand a site from avaialable sites for a visit and add for the new route.
                            int siteIndex = rnd.Next(0, availableSites.Count);
                            hoursPerSite.Add(availableSites.ElementAt(siteIndex).name + ": " + currentTime);
                            newRoute.Add(availableSites.ElementAt(siteIndex));
                            currentTime = getTimeForNextSite(currentTime);
                        }
                    }

                    hoursPerSite.Add("Ends: " + currentTime);
                    return rh.HandleResponse(new { route = newRoute, hours = hoursPerSite });
                }      
            }
        }

        // --------------------------------- Server internal methods --------------------------------- //

        // Get time for next site visit.
        private String getTimeForNextSite(String time)
        {
            String pattern = "\\d+";
            Regex rgx = new Regex(pattern);
            MatchCollection mc = Regex.Matches(time, pattern);
            String hour = mc[0].Value;
            String minutes = mc[1].Value;
            String newHour = (int.Parse(hour) + 2).ToString();
            return newHour + ":" + minutes;
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

            if (openingHours.Equals(OPEN_24_HOURS))
            {
                return true;
            }
            else if (openingHours.Equals(CLOSED))
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

            switch (day)
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

            for (int i = 0; i < openRanges.Length && !endOfCheck; i++)
            {
                result = checkIfCurrentTimeInSingleRange(currentTime, openRanges[i]);
                if (result)
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

            if (endTimeCounter < startTimeCounter)
            {
                endTimeCounter += MINUTES_IN_ONE_DAY;
            }

            // Check if a visit is possible.
            if ((startTimeCounter <= currentTimeCounter) && ((currentTimeCounter + VISIT_LENGTH_IN_MINUTES) <= endTimeCounter))
            {
                return true;
            }
            return false;
        }

        // Calculate the a number of sites for the new route.
        private int getNumberOfSites(string startTime, string endTime)
        {
            String pattern = "\\d+";
            Regex rgx = new Regex(pattern);
            MatchCollection mc;
            
            mc = Regex.Matches(startTime, pattern);
            String startHour = mc[0].Value;
            String startMinutes = mc[1].Value;

            // Get counter for the start time.
            int startTimeCounter = int.Parse(startHour) * MINUTES_IN_ONE_HOUR + int.Parse(startMinutes);

            mc = Regex.Matches(endTime, pattern);
            String endHour = mc[0].Value;
            String endMinutes = mc[1].Value;

            // Get counter for the end time
            int endTimeCounter = int.Parse(endHour) * MINUTES_IN_ONE_HOUR + int.Parse(endMinutes);

            return (endTimeCounter - startTimeCounter) / VISIT_LENGTH_IN_MINUTES;
        }
    }
}
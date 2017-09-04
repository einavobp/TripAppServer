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
using System.Globalization;
using System.Device;
using System.Device.Location;

namespace TripAppServer.Controllers
{
    public class RoutesController : ApiController
    {
        ResponseHandler rh = new ResponseHandler();
        private const String CLOSED = "Closed";
        private const String OPEN_24_HOURS = "Open 24 hours";
        private const String ROUTE_FINDER_ERROR_MEASSAGE = "Could not calculate route with the user perfernces.";
        private const String DEFAULT_DESCRIPTION = "Created by admin";
        private const String NEW_YORK_IMAGE_URL = "https://www.studentflights.com.au/sites/studentflights.com.au/files/new-york.jpg";
        private const int ADMIN_ID = -1;
        private const double DEFAULT_RATE = 3;
        private const int MINUTES_IN_ONE_HOUR = 1 * 60;
        private const int MINUTES_IN_ONE_DAY = 24 * 60;
        private const int VISIT_LENGTH_IN_MINUTES = 2 * 60;
        private const int VISIT_LENGTH_IN_HOURS = 2;
        private const double START_LATITUDE = 40.758895;
        private const double START_LONGITUDE = -73.985131;

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
                    bool endOfCalculation=false;
                    Position location = new Position(START_LATITUDE, START_LONGITUDE);

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
                            int siteIndex = getClosestSiteIndex(availableSites, location);
                            hoursPerSite.Add(currentTime);
                            newRoute.Add(availableSites.ElementAt(siteIndex));

                            // Set location and time for the next site finding.
                            currentTime = getTimeForNextSite(currentTime);
                            location.lat = (double)availableSites.ElementAt(siteIndex).location_lat;
                            location.lng = (double)availableSites.ElementAt(siteIndex).location_lng;
                        }
                    }
                    addToSavedRoute(se, newRoute, userRequest.cityId);
                    return rh.HandleResponse(new { route = newRoute, hours = hoursPerSite });
                }      
            }
        }

        // Save route in smart trip DB.
        [Route("api/routes/saveRoute")]
        [HttpPost]
        public HttpResponseMessage saveRoute(RouteSave route)
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                var route_ = new routes();
                route_.name = route.name;
                route_.city_id = route.cityId;
                route_.user_id = route.userId;
                route_.sites = route.sites;
                route_.image_url = route.imageUrl;
                route_.rate = route.rate;
                route_.description = route.description;
                se.routes.Add(route_);
                se.SaveChanges();
            }
            return rh.HandleResponse(new { HttpStatusCode.Accepted });
        }

        // --------------------------------- Server internal methods --------------------------------- //

        // Get closest site index.
        private int getClosestSiteIndex(List<sites> availableSites, Position currentPosition)
        {
            int minIndex = 0;
            double distance;

            // Set first element as closest point.
            var firstCordinate = new GeoCoordinate(currentPosition.lat, currentPosition.lng);
            var secondCordinate = new GeoCoordinate((double)availableSites.ElementAt(0).location_lat, (double)availableSites.ElementAt(0).location_lng);
            double minDistance = firstCordinate.GetDistanceTo(secondCordinate);

            // Find the closest site by calcualting the minuimum distance.
            firstCordinate = new GeoCoordinate(currentPosition.lat, currentPosition.lng);
            for (int i = 1; i < availableSites.Count; i++)
            {
                secondCordinate = new GeoCoordinate((double)availableSites.ElementAt(i).location_lat, (double)availableSites.ElementAt(i).location_lng);
                distance = firstCordinate.GetDistanceTo(secondCordinate);

                if (distance < minDistance)
                {
                    minIndex = i;
                    minDistance = distance;
                }
            }

            return minIndex;
        }

        // Get time for next site visit.
        private String getTimeForNextSite(String time)
        {
            String pattern = "\\d+";
            Regex rgx = new Regex(pattern);
            MatchCollection mc = Regex.Matches(time, pattern);
            String hour = mc[0].Value;
            String minutes = mc[1].Value;
            String newHour = (int.Parse(hour) + VISIT_LENGTH_IN_HOURS).ToString();
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

        // Get site opening hours from today.
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

        // Add new route to the saved route in the DB.
        private void addToSavedRoute(smart_trip_dbEntities se, List<sites> newRoute, int cityId)
        {
            var route = new routes();
            route.name = "Route number " + (se.routes.Count()+1).ToString();
            route.city_id = cityId;
            route.user_id = ADMIN_ID;
            route.sites = getSitesString(newRoute);
            route.image_url = NEW_YORK_IMAGE_URL;
            route.rate = DEFAULT_RATE;
            route.description = DEFAULT_DESCRIPTION;
            se.routes.Add(route);
            se.SaveChanges();
        }

        // Get sites string id's sepeated by with commas.
        private string getSitesString(List<sites> newRoute)
        {
            String sitesStr = "";
            int counter = 0;

            foreach(sites site in newRoute)
            {
                sitesStr += site.id;
                counter++;
                if(counter < newRoute.Count)
                {
                    sitesStr += ',';
                }
            }

            return sitesStr;
        }
    }
}
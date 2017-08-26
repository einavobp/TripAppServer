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

        // --------------------------------- Requests from client --------------------------------- //

        // Returns all sites in Smart Trip DB by city id.
        [Route("api/sites/getAllSites")]
        [HttpPost]
        public HttpResponseMessage getAllSites(int cityId)
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                var sites = se.sites.Where(s => s.city_id != null && s.city_id == cityId).ToList();
                return rh.HandleResponse(new { city_sites = sites });
            }
        }

        // Returns all sites in Smart Trip DB by city id and and type id.
        [Route("api/sites/getAllCitySitesTypes")]
        [HttpPost]
        public HttpResponseMessage getAllCitySitesTypes(int cityId, int typeId)
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                var sites = se.sites.Where(s => s.city_id != null && s.city_id == cityId && s.types != null && s.types.Contains(typeId.ToString())).ToList();
                return rh.HandleResponse(new { city_sites = sites });
            }
        }

        // Returns site opening hours by the site DB id.
        [Route("api/sites/getSiteOpeningHours")]
        [HttpPost]
        public HttpResponseMessage getSiteOpeningHours(int siteId)
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                var openingHours = se.sites_opening_hours.Where(s => s.id == siteId).ToList();
                return rh.HandleResponse(new { site_opening_hours = openingHours });
            }
        }
    }
}


// --------------------------------- OLD DO NOT DELETE !--------------------------------- //


/*
        // Returns all cities in the DB.
        [Route("api/city/all")]
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
        }/*

        /*
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
        }*/

/*
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
}*/

/*
// Returns full site info by the site google ID.
[Route("api/sites/getSiteFullInfoByGoogleId")]
[HttpPost]
public HttpResponseMessage getSiteFullInfoByGoogleId(String googleId)
{
String url = PLACE_DETAILS_REQUST;
url = url.Replace(PLACE_ID_REPLACEMENT, googleId);
SiteJasonDetails site = getSiteHttpsRequest(url);
return rh.HandleResponse(new { site = site });
}*/



// --------------------------------- Server internal methods --------------------------------- //

/*
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
     */

//private const int NUMBER_OF_SITES_IN_ROUTE = 5;
//private const String PLACE_ID_REPLACEMENT = "<place_id>";
//private const String GOOGLE_PLACES_API_KEY = "AIzaSyApCIPpdSZZHQ6E7EFp9SpnJ1OGF8bLRrQ";
//private const String PLACE_DETAILS_REQUST = "https://maps.googleapis.com/maps/api/place/details/json?placeid=" + PLACE_ID_REPLACEMENT + "&key=" + GOOGLE_PLACES_API_KEY;

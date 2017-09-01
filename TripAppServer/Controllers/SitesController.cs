using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using TripAppServer;
using TripAppServer.Handlers;
using TripAppServer.Models;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Net.Http;

namespace TripAppServer.Controllers
{
    public class SitesController : ApiController
    {
        ResponseHandler rh = new ResponseHandler();

        // --------------------------------- Requests from client --------------------------------- //

        // Returns all sites in Smart Trip DB by city id.
        [HttpPost]
        [Route("api/sites/getAllSites/{city_id}")]
        public HttpResponseMessage getAllSites(int city_id)
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                var sites = se.sites.Where(s => s.city_id != null && s.city_id == city_id).ToList();
                return rh.HandleResponse(new { city_sites = sites });
            }
        }

        // Returns all sites in Smart Trip DB by city id and and type id.
        [Route("api/sites/getAllCitySitesTypes")]
        [HttpPost]
        public HttpResponseMessage getAllCitySitesTypes(CitySiteType siteType)
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                var sites = se.sites.Where(s => s.city_id != null && s.city_id == siteType.cityId && s.types != null && s.types.Contains(siteType.typeId.ToString())).ToList();
                return rh.HandleResponse(new { city_sites = sites });
            }
        }

        // Returns site opening hours by the site DB id.
        [Route("api/sites/getSiteOpeningHours/{siteId}")]
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

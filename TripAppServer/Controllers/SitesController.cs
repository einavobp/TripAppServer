using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TripAppServer.Handlers;
using TripAppServer.Models;

namespace TripAppServer.Controllers
{
    public class SitesController : ApiController
    {
        ResponseHandler rh = new ResponseHandler();
        private const int NUMBER_OF_SITES_IN_ROUTE = 5;

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

        // Returns all sites in Smart Trip DB by type ID.
        [Route("api/sites/getSitesByType")]
        [HttpPost]
        public HttpResponseMessage GetSitesByType(SiteIdModel siteId)
        {
            using (smart_trip_db se = new smart_trip_db()) 
            {
                var sites = se.sites
                   .Where(s => s.types != null && s.types.Contains(siteId.type_id.ToString()))
                   .ToList();

                return rh.HandleResponse(new { sites = sites });
            }
        }

        // Returns random route of 5 sites from the DB.
        [Route("api/sites/getRandomRoute")]
        [HttpGet]
        public HttpResponseMessage GetRandomRoute()
        {
            using (smart_trip_db se = new smart_trip_db()) 
            {
                var sites = se.sites.ToList();
                List<TripAppServer.sites> newRoute = new List<TripAppServer.sites>();
                Random rnd = new Random();

                int i;
                for(i=0;i<NUMBER_OF_SITES_IN_ROUTE;i++)
                {
                    int siteIndex = rnd.Next(0, sites.Count);
                    newRoute.Add(sites.ElementAt(siteIndex));
                    sites.RemoveAt(siteIndex);
                }

                return rh.HandleResponse(new { route = newRoute });
            }
        }
    }
}

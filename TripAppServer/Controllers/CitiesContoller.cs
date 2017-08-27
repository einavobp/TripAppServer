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
    public class CitiesController : ApiController
    {
        ResponseHandler rh = new ResponseHandler();

        // --------------------------------- Requests from client --------------------------------- //      
           
        // Returns all cities in Smart Trip DB.
        [Route("api/cities/all")]
        [HttpGet]
        public HttpResponseMessage getAll()
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                var cities = se.cities.ToList();
                return rh.HandleResponse(new { cities = cities });
            }
        }

        // Returns all routes of the input city id.
        [Route("api/cities/getCityRoutes")]
        [HttpPost]
        public HttpResponseMessage getCityRoutes(int cityId)
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                var routes = se.routes.Where(s => s.city_id !=null && s.city_id == cityId).ToList();
                return rh.HandleResponse(new { city_routes = routes });
            }
        }
    }
}
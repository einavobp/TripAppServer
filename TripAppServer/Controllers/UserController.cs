using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TripAppServer.Handlers;

namespace TripAppServer.Controllers
{
    public class UserController : ApiController
    {
        ResponseHandler rh = new ResponseHandler();

        [HttpGet]
        [Route("api/user/get")]
        public HttpResponseMessage GetUser()
        {
            //have to wrap with using for errors
            using (smart_trip_dbEntities se = new smart_trip_dbEntities()) // this is how you open connection to DB (general)
            {
                var sites = se.sites.ToList();

                return rh.HandleResponse(new { user_name = "orel", password = "123456", sites = sites });
            }
        }

        [HttpGet]
        [Route("api/user/getRestaurant")]
        public HttpResponseMessage GetRestaurants()
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities()) // this is how you open connection to DB (general)
            {
                var sites = se.sites
                    .Where(s=>s.types!=null && s.types.Contains("food"))
                    .ToList();

                return rh.HandleResponse(new { user_name = "orel", password = "123456", sites = sites });
            }
        }


        [HttpPost]
        [Route("api/user/login")]
        // For example - login(username, pass)
        // we shhould create an object and send it like login(user)
        public HttpResponseMessage Login(UserModel userModel ) // controllers must to get an object and not some types
        {
            string name = "orel", pass = "1222";
            if (userModel == null)
                return rh.HandleError(HttpStatusCode.BadRequest, "No creds!!!");
            if (name.Equals(userModel.UserName) && pass.Equals(userModel.Password))
                return rh.HandleResponse("Welcome!!!");
            else
                return rh.HandleError(HttpStatusCode.Unauthorized, "No user found!");
        }
    }
}

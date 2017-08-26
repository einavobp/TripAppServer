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
    public class UserController : ApiController
    {
        ResponseHandler rh = new ResponseHandler();

        // --------------------------------- Requests from client --------------------------------- //

        // Log user with user name and password or device token.
        [Route("api/users/login")]
        [HttpPost]
        public HttpResponseMessage login(UserConnection connectionDetails)
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                bool userExists = isUserExists(se, connectionDetails);
                if (userExists==false) 
                {
                    //To Do: Add new user to DB.
                }

                var cities = se.cities.ToList();
                var categories = se.sites_types.ToList();
                var recommandedRoutes = se.routes.Where(s => s.rate != null && s.rate>=4).ToList();

                return rh.HandleResponse(new { cities = cities, categories = categories, recommanded_routes = recommandedRoutes });
            }
        }

        // --------------------------------- Server internal methods --------------------------------- // 
                  
        // Check if user exits in the DB,
        private bool isUserExists(smart_trip_dbEntities se, UserConnection connectionDetails)
        {
            try
            {
                bool userExists = se.users.Any(o => (o.uname.Equals(connectionDetails.userName) && o.password.Equals(connectionDetails.password)) || (o.device_token.Equals(connectionDetails.deviceToken)));
                if (userExists)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}

// --------------------------------- OLD DO NOT DELETE !--------------------------------- //

/*
        [HttpGet]
        [Route("api/user/get")]
        public HttpResponseMessage GetUser()
        {
            //have to wrap with using for errors
            using (smart_trip_db se = new smart_trip_db()) // this is how you open connection to DB (general)
            {
                var sites = se.sites.ToList();

                return rh.HandleResponse(new { user_name = "orel", password = "123456", sites = sites });
            }
        }

        [HttpGet]
        [Route("api/user/getRestaurant")]
        public HttpResponseMessage GetRestaurants()
        {
            using (smart_trip_db se = new smart_trip_db()) // this is how you open connection to DB (general)
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
        }*/

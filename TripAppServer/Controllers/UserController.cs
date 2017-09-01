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
        private const int RECOMMANDED_ROUTE_RATING = 4;

        // --------------------------------- Requests from client --------------------------------- //

        // Log user with user name and password or device token.
        [Route("api/users/login")]
        [HttpPost]
        public HttpResponseMessage login(UserConnection user)
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                var user_ = getUserIfExists(se, user.userName, user.password, user.deviceToken);
                if (user_ == null)
                {
                    user_ = new users();
                    user_.uname = user.userName;
                    user_.password = user.password;
                    user_.device_token = user.deviceToken;
                    se.users.Add(user_);
                    se.SaveChanges();
                }

                var cities = se.cities.ToList();
                var categories = se.sites_types.ToList();
                var recommandedRoutes = se.routes.Where(s => s.rate != null && s.rate >= RECOMMANDED_ROUTE_RATING).ToList();

                return rh.HandleResponse(new { user = user_, cities = cities, categories = categories, recommanded_routes = recommandedRoutes });
            }
        }

        // Returns all seasons in Smart Trip DB.
        [Route("api/users/getAllSeasons")]
        [HttpGet]
        public HttpResponseMessage getAllSeasons()
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                var seasons = se.seasons.ToList();
                return rh.HandleResponse(new { seasons = seasons });
            }
        }

        // Returns all compositions in Smart Trip DB.
        [Route("api/users/getAllCompositions")]
        [HttpGet]
        public HttpResponseMessage getAllCompositions()
        {
            using (smart_trip_dbEntities se = new smart_trip_dbEntities())
            {
                var compositions = se.composition.ToList();
                return rh.HandleResponse(new { compositions = compositions });
            }
        }

        // --------------------------------- Server internal methods --------------------------------- // 

        // Check if user exits in the DB.
        private bool isUserExists(smart_trip_dbEntities se, String userName, String password, String deviceToken)
        {
            try
            {
                bool userExists = se.users.Any(o => (o.uname.Equals(userName) && o.password.Equals(password)) || (o.device_token.Equals(deviceToken)));
                if (userExists)
                    return true;
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Get user if exist in the DB.
        private users getUserIfExists(smart_trip_dbEntities se, String userName, String password, String deviceToken)
        {
            try
            {
                return se.users.FirstOrDefault(o => (o.uname.Equals(userName) && o.password.Equals(password)) || (o.device_token.Equals(deviceToken)));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
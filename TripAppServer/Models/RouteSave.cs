using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TripAppServer.Models
{
    public class RouteSave
    {
        public String name { get; set; }

        public int cityId { get; set; }

        public int userId { get; set; }

        public String sites { get; set; }

        public String imageUrl { get; set; }

        public double rate { get; set; }

        public String description { get; set; }
    }
}
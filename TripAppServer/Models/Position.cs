using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TripAppServer.Models
{
    public class Position
    {
        public double lat { get; set; }

        public double lng { get; set; }
        public Position(double lat, double lng)
        {
            this.lat = lat;
            this.lng = lng;
        }
    }
}
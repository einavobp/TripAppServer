using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TripAppServer.Models
{
    public class RouteRequest
    {
        public int cityId { get; set; }

        public int seasonId { get; set; }

        public int compositionId { get; set; }

        public String startTime { get; set; }

        public String endTime { get; set; }

    }
}
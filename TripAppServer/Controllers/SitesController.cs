using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TripAppServer.Handlers;

namespace TripAppServer.Controllers
{
  public class SitesController : ApiController
  {
    ResponseHandler rh = new ResponseHandler();

    [Route("api/sites/getAll")]
    [HttpGet]
    public HttpResponseMessage GetSites()
    {
      return rh.HandleResponse(new { name = "einav", version = "1" });
    }
  }
}

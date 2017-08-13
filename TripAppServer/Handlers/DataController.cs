using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace TripAppServer.Handlers
{
  public class DataController : ApiController
  {
    ResponseHandler rh = new ResponseHandler();

    public string FindPlaces(string latlang)
    {
      return string.Format("https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={0}&radius=50000&&key=AIzaSyAgWAvsfKkeRfAIchut1UQrmkHlL_9V8U0", latlang);
    }
    public string FindPlaces(string latlang, string type)
    {
      return string.Format("https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={0}&radius=50000&type={1}&&key=AIzaSyAgWAvsfKkeRfAIchut1UQrmkHlL_9V8U0", latlang, type);
    }
    public string PLACE_DETAILS(string place_id)
    {
      return string.Format("https://maps.googleapis.com/maps/api/place/details/json?placeid={0}&key=AIzaSyAgWAvsfKkeRfAIchut1UQrmkHlL_9V8U0", place_id);
    }

    public List<sites> getSites(string latlan, string type)
    {
      using (var client = new WebClient())
      {
        var values = new NameValueCollection();
        var response = client.DownloadData(FindPlaces(latlan, type));
        var responseString = Encoding.Default.GetString(response);
        JObject responseData = JObject.Parse(responseString);
        List<sites> sites = new List<TripAppServer.sites>();
        foreach (var item in responseData["results"])
        {
          var s = new sites();
          s.location_lat = item["geometry"]["location"]["lat"] == null ? 0 : double.Parse(item["geometry"]["location"]["lat"].ToString());
          s.location_lng = item["geometry"]["location"]["lng"] == null ? 0 : double.Parse(item["geometry"]["location"]["lng"].ToString());
          s.place_id = item["place_id"].ToString();
          s.types = item["types"].ToString();
          var responseD = client.DownloadData(PLACE_DETAILS(s.place_id));
          var responseDString = Encoding.Default.GetString(responseD);
          JObject jData = JObject.Parse(responseDString);
          s.name = jData["result"]["name"].ToString();
          s.formatted_address = jData["result"]["formatted_address"].ToString();
          s.international_phone_number = jData["result"]["formatted_phone_number"] == null ? string.Empty : jData["result"]["formatted_phone_number"].ToString();
          s.rating = jData["result"]["rating"] == null ? 0 : double.Parse(jData["result"]["rating"].ToString());
          //opening_hours
          sites.Add(s);
        }
        return sites;
      }
    }

    [HttpGet]
    public HttpResponseMessage SyncData()
    {
      string[] types2 = new string[] { "aquarium",
        "art_gallery",
        "cafe",
        "casino",
        "park",
        "zoo",
        "travel_agency",
        "transit_station",
        "train_station",
        "museum",
        "restaurant",
        "shopping_mall" };
      string[] types = new string[] { "aquarium",
        "park",
        "zoo",
        "museum",
        "restaurant",
        "shopping_mall" };

      List<sites> sites = new List<TripAppServer.sites>();

      foreach (var item in types)
      {
        sites.AddRange(getSites("40.828042, -73.929971", item));
        sites.AddRange(getSites("40.746257, -73.980128", item));
        JArray j = JArray.FromObject(sites);

        using (System.IO.StreamWriter file =
        new System.IO.StreamWriter(@"C:\Temp\WriteLines2.txt", true))
        {
          file.Write("-------------------------------------");
          file.WriteLine(j.ToString());
          file.Write("-------------------------------------");
        }
      }

      return rh.HandleResponse(new
      {
        size = sites.Count,
        sites = sites
      });

    }
  }
}


using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;

namespace TripAppServer.Handlers
{
    public class ResponseHandler
    {
        public const string ok_response_format = "{{ \"status\": \"ok\", \"body\" : {0}}}";
        public const string err_response_format = "{{ \"status\": \"error\", \"body\" : {0}}}";

   

        public HttpResponseMessage HandleResponse(Object response)
        {
            var res = new HttpResponseMessage(HttpStatusCode.OK);
            res.Content = new StringContent(Serialize(response), System.Text.Encoding.UTF8, "application/json");

            return res;
        }

        public HttpResponseMessage HandleError(string error_description)
        {
            var res = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            res.Content = new StringContent( Serialize(error_description), System.Text.Encoding.UTF8, "application/json");

            return res;
        }

        public HttpResponseMessage HandleError(HttpStatusCode status_code, string error_description)
        {
            var res = new HttpResponseMessage(status_code);
            res.Content = new StringContent(Serialize(error_description), System.Text.Encoding.UTF8, "application/json");

            return res;
        }

        public static string Serialize(Object _o)
        {
            return JsonConvert.SerializeObject(_o, Formatting.None, new JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore, ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
        }


    }
}
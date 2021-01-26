using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace PostmanLib
{
    public class Postman
    {
        private JObject postmanCollection;
        public Postman(string collection)
        {
            postmanCollection = JObject.Parse(collection);
        }

        public string Execute(string function, string variables)
        {
            var item = postmanCollection["item"].FirstOrDefault(
                j => j["name"] + "" == function);
            if (item == null) throw new ArgumentException("function not recognized");
            var request = item["request"];
            var method = request["method"] + "";
            using var web = new WebClient {Encoding = Encoding.UTF8};
            var url = request["url"]["raw"] + "";
            var jVariables = JObject.Parse(variables);
            foreach (var (key, value) in jVariables)
            {
                url = url.Replace("{{" + key + "}}", value + "");
            }

            if (request["auth"] != null && request["auth"]["type"] + "" == "basic")
            {
                var username = request["auth"]["basic"].First(j => j["key"] + "" == "username")["value"] + "";
                var password = request["auth"]["basic"].First(j => j["key"] + "" == "password")["value"] + "";
                var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
                web.Headers.Add(HttpRequestHeader.Authorization,"Basic " + b64);
            }

            if (request["auth"] != null && request["auth"]["type"] + "" == "bearer")
            {
                var token = request["auth"]["bearer"].First(j => j["key"] + "" == "token")["value"] + "";
                foreach (var (key, value) in jVariables)
                {
                    token = token.Replace("{{" + key + "}}", value + "");
                }
                web.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + token);
            }
            
            if (request["header"] is JArray headers)
            {
                foreach (var header in headers)
                {
                    web.Headers.Add(header["key"].ToString(), header["value"].ToString());
                }
            }
            string result;
            switch (method)
            {
                case "GET":
                    result = web.DownloadString(url);
                    break;
                case "POST":
                    var raw = request["body"]["raw"].ToString();
                    foreach (var (key, value) in jVariables)
                    {
                        raw = raw.Replace("{{" + key + "}}", value + "");
                    }
                    result = web.UploadString(url,raw);
                    break;
                default:
                    throw new NotImplementedException("Only HTTP GET and POST are currently supported");
            }
            return result;
        }
    }
}

using System;
using System.Linq;
using System.Net;
using System.Text;
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
            if (method != "GET") 
                throw new NotImplementedException("Only HTTP GET is currently supported");
            if (request["header"] is JArray headers && headers.Count > 0) 
                throw new NotImplementedException("HTTP Headers are not yet supported");
            var url = request["url"]["raw"] + "";
            var jVariables = JObject.Parse(variables);
            foreach (var (key,value) in jVariables)
            {
                url = url.Replace("{{" + key + "}}", value+"");
            }
            using var web = new WebClient {Encoding = Encoding.UTF8};
            var result = web.DownloadString(url);
            return result;
        }
    }
}

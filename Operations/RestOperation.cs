using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using CopyCat.Parser;
using CopyCat.Utils;
using Newtonsoft.Json;

namespace CopyCat.Operations
{
    public class RestOperation : BaseOperation
    {
        public new static bool CommandMatch(string line) => line.Trim().StartsWith("rest", StringComparison.InvariantCultureIgnoreCase);


        public class RestRequest
        {
            [JsonProperty("url")] 
            public string Url { get; set; }
            [JsonProperty("method")]
            public string Method { get; set; }
            [JsonProperty("headers")]
            public Dictionary<string, string> Headers { get; set; }
            [JsonProperty("body")]
            public object Body { get; set; }
        }

        public RestRequest RequestInfo { get; }
        public RestOperation(CopyCatScript script, string line) : base(script, line)
        {
            line = line.TrimStart().Substring("rest".Length);
            RequestInfo = JsonConvert.DeserializeObject<RestRequest>(line);
        }

        public override ResultCode Execute()
        {
            //rest {"url": "https://...", "method":"post", "body" : {...}, "headers": { "a" : "b" } }
            try
            {
                HttpWebRequest request = WebRequest.CreateHttp(RequestInfo.Url);
                request.Method = RequestInfo.Method;
                if (RequestInfo.Headers != null)
                {
                    foreach (string key in RequestInfo.Headers.Keys)
                    {
                        //req.Headers.Set(HttpRequestHeader.)
                        if (Enum.TryParse(key, out HttpRequestHeader specialHeader))
                        {
                            request.Headers.Set(specialHeader, RequestInfo.Headers[key]);
                        }
                        else
                        {
                            request.Headers.Set(key, RequestInfo.Headers[key]);
                        }
                    }
                }
                if (RequestInfo.Body != null)
                {
                    using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                    {
                        writer.WriteLine(JsonConvert.SerializeObject(RequestInfo.Body));
                    }
                }
                RestResponse restResp = RestResponse.GetResponse(request.GetResponse());

                if (restResp.StatusCode.HasValue)
                {
                    Utility.WriteInfo($"Rest request completed with status code [{restResp.StatusCode}]");
                }

            }
            catch (Exception ex)
            {
                return Utility.ExceptionToErrCode(ex);
            }
            return ResultCode.SUCCESS;
        }


        class RestResponse
        {
            public HttpStatusCode? StatusCode { get; private set; }
            public string ResponseText { get; private set; } = string.Empty;
            public static RestResponse GetResponse(WebResponse response)
            {
                RestResponse ret = new RestResponse();
                if (response is HttpWebResponse htResp)
                {
                    ret.StatusCode = htResp.StatusCode;
                    try
                    {
                        if (response.ContentLength > 0)
                        {
                            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                            {
                                ret.ResponseText = reader.ReadToEnd();
                            }
                        }
                    }
                    catch
                    {

                    }
                }
                return ret;
            }
        }

    }
}

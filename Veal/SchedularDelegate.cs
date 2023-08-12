using Kayak.Http;
using Kayak;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Net;
using Jil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection.Metadata.Ecma335.Blobs;
using System.Reflection.PortableExecutable;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System.Web;

namespace Veal
{
    class SchedulerDelegate : ISchedulerDelegate
    {
        public void OnException(IScheduler scheduler, Exception e)
        {
            Debug.WriteLine("Error on scheduler.");
            e.DebugStackTrace();
        }

        public void OnStop(IScheduler scheduler)
        {

        }
    }

    class RequestDelegate : IHttpRequestDelegate
    {
        private string _prefix;
        private HashSet<KeyValuePair<string, MethodInfo>> actionList;
        private HashSet<RouteValueModel> routeValueDictionary;

        public RequestDelegate(string prefix, HashSet<KeyValuePair<string, MethodInfo>> actionList, HashSet<RouteValueModel> routeValueDictionary)
        {
            _prefix = prefix;
            this.actionList = actionList;
            this.routeValueDictionary = routeValueDictionary;
        }
        private bool DoesActualUrlMatchTemplatePattern(string actualRequestUrl, string routeTemplateUrl)
        {
            var result = false;

            var afterTemplate2 = Regex.Replace(routeTemplateUrl, @":(?<type>\w+)\}", "}");
            Console.WriteLine("URL after removal of DataType Names >>> {0}", afterTemplate2);
            //var afterTemplate2 = "vealpostrequesturl/id/{id}/orders?orderId={orderId}&itemId={itemId}"; //remove the types...
            var matches2 = Regex.Matches(afterTemplate2, @"\{(?<variable>\w+)\}");
            Console.WriteLine("After removing the DataTypeNames, we get {0} matches", matches2.Count);


            var cleanUrl = Regex.Replace(afterTemplate2, @"\{([^}]+)\}", "$1");
            Console.WriteLine("Clean URL after all the routeParameter processing is done >>> {0}", cleanUrl);


            //var matches3 = Regex.Matches(afterTemplate3, @"\{(?<variable>\w+)\}");
            var xxx = cleanUrl.Replace(this._prefix, "/");
            //result = Regex.Matches(xxx, @"\{(?<variable>\w+)\}").Count > 0 && Regex.Matches(actualRequestUrl, @"\{(?<variable>\w+)\}").Count > 0;
            //var similarity = findSimilarity(xxx, actualRequestUrl);
            var similarity = similarStringsPercentage(xxx, actualRequestUrl);
            Console.WriteLine("Similarity of strings >>> {0}", similarity);
            result = similarity > 0.70;

            return result;


        }
        public double similarStringsPercentage(string one, string two)
        {
            var commonChars = 0;
            //var totalWordsChars = string.Concat(one, two).Length;
            var (word, other) = one.Length >= two.Length ? (one, two) : (two, one);
            var totalWordChars = word.Length;
            //word is the longest... 
            for (int i = 0; i < word.Length; i++)
            {
                if (i == other.Length - 1) break;
                if (other.Contains(word[i]) && (other[i] == word[i])) commonChars++;
            }

            return (double)commonChars / totalWordChars;

        }
        private bool IsUrlDefined(string routeTemplateUrl, string actualUrl)
        {
            var routeTemplateSegments = new Uri(routeTemplateUrl).Segments;
            var queryParamsRouteTemplate = HttpUtility.ParseQueryString(routeTemplateUrl);
            var qpT = !queryParamsRouteTemplate.HasKeys() ? 0 : queryParamsRouteTemplate.Count;

            var actualUrlSegments = new Uri(string.Concat(this._prefix, actualUrl.Substring(1, actualUrl.Length - 1))).Segments;
            var actualUrlQueryParams = HttpUtility.ParseQueryString(actualUrl);
            var qpA = !actualUrlQueryParams.HasKeys() ? 0 : actualUrlQueryParams.Count;

            return (routeTemplateSegments.Length == actualUrlSegments.Length) && (qpT == qpA);

        }
        public void OnRequest(HttpRequestHead request, IDataProducer requestBody,
                IHttpResponseDelegate response)
        {
            //Console.WriteLine($"Received {request.Method.ToUpperInvariant()} request for {request.Uri} ABSOLUTEURL >>> {_prefix}{request.Path.Replace("/", string.Empty)}");

            var urlExists = this.actionList.FirstOrDefault(x => IsUrlDefined(x.Key, request.Uri));
            if (string.IsNullOrWhiteSpace(urlExists.Key))
            {
                //return 404
                var responseBody = string.Format("The resource you requested '{0}' could not be found.", request.Uri);
                var headers = new HttpResponseHead()
                {
                    Status = "404 Not Found",
                    Headers = new Dictionary<string, string>()
                    {
                        { "Content-Type", "text/plain" },
                        { "Content-Length", responseBody.Length.ToString() }
                    }
                };
                var body = new BufferedProducer(responseBody);

                response.OnResponse(headers, body);
            }
            else
            {
                //capture routeParameter values... if any?
                //
                var totalParamObjects = new List<object>();

                //Console.WriteLine("What we are going to do if it's not 404...Of course we will handle more cases 401, 403, etc");
                if (request.Method.ToUpperInvariant() == "GET")
                {
                    var param = request.QueryString;

                    var type = urlExists.Value.DeclaringType;
                    object classInstance = Activator.CreateInstance(type, new object[] { });
                    try
                    {
                        totalParamObjects = ExtractInvokationParameters(request, urlExists, string.Empty);

                    }
                    catch (Exception ex)
                    {

                        var badRequestBody = string.Format("There is a problem with one or more action parameters {0} Exception {1}", request.Uri, ex.Message);
                        var badRequestHeaders = new HttpResponseHead()
                        {
                            Status = "400 Bad Request",
                            Headers = new Dictionary<string, string>()
                    {
                        { "Content-Type", "text/plain" },
                                {"Connection", "close" },
                        { "Content-Length", badRequestBody.Length.ToString() }
                    }
                        };
                        var badBody = new BufferedProducer(badRequestBody);

                        response.OnResponse(badRequestHeaders, badBody);
                    }

                    var responderResponse = (HttpResponder)urlExists.Value.Invoke(classInstance, totalParamObjects.ToArray());
                    Debug.WriteLine(responderResponse.ToString());
                    Console.WriteLine(responderResponse.ToString());
                    var body = string.Format("{0}", Converter.SerializeObject(responderResponse?.Value));

                    var headers = new HttpResponseHead()
                    {
                        Status = responderResponse.StatusDescription,
                        Headers = new Dictionary<string, string>()
                    {
                        { "Connection", "close" },
                        { "Content-Length", body.Length.ToString() },
                    }
                    };
                    if (request.Headers.ContainsKey("Content-Type"))
                        headers.Headers["Content-Type"] = request.Headers["Content-Type"];
                    else headers.Headers["Content-Type"] = "application/json";

                    response.OnResponse(headers, new BufferedProducer(body));
                }
                else if (request.Method.ToUpperInvariant() == "DELETE")
                {
                    var type = urlExists.Value.DeclaringType;
                    object classInstance = Activator.CreateInstance(type, new object[] { });
                    try
                    {
                        totalParamObjects = ExtractInvokationParameters(request, urlExists, string.Empty);

                    }
                    catch (Exception ex)
                    {

                        var badRequestBody = string.Format("There is a problem with one or more action parameters {0} Exception {1}", request.Uri, ex.Message);
                        var badRequestHeaders = new HttpResponseHead()
                        {
                            Status = "400 Bad Request",
                            Headers = new Dictionary<string, string>()
                    {
                        { "Content-Type", "text/plain" },
                                {"Connection", "close" },
                        { "Content-Length", badRequestBody.Length.ToString() }
                    }
                        };
                        var badBody = new BufferedProducer(badRequestBody);

                        response.OnResponse(badRequestHeaders, badBody);
                    }

                    var responderResponse = (HttpResponder)urlExists.Value.Invoke(classInstance, totalParamObjects.ToArray());
                    Debug.WriteLine(responderResponse.ToString());
                    Console.WriteLine(responderResponse.ToString());
                    var body = string.Format("{0}", Converter.SerializeObject(responderResponse?.Value));

                    var headers = new HttpResponseHead()
                    {
                        Status = responderResponse.StatusDescription,
                        Headers = new Dictionary<string, string>()
                    {
                        { "Connection", "close" },
                        { "Content-Length", body.Length.ToString() },
                    }
                    };
                    if (request.Headers.ContainsKey("Content-Type"))
                        headers.Headers["Content-Type"] = request.Headers["Content-Type"];
                    else headers.Headers["Content-Type"] = "application/json";

                    response.OnResponse(headers, new BufferedProducer(body));
                }
                else if (request.Method.ToUpperInvariant() == "POST")
                {
                    requestBody.Connect(new BufferedConsumer(bufferedBody =>
                    {
                        var type = urlExists.Value.DeclaringType;
                        try
                        {
                            totalParamObjects = ExtractInvokationParameters(request, urlExists, bufferedBody);

                        }
                        catch (Exception ex)
                        {

                            var badRequestBody = string.Format("There is a problem with one or more action parameters {0} Exception {1}", request.Uri, ex.Message);
                            var badRequestHeaders = new HttpResponseHead()
                            {
                                Status = "400 Bad Request",
                                Headers = new Dictionary<string, string>()
                    {
                        { "Content-Type", "text/plain" },
                                {"Connection", "close" },
                        { "Content-Length", badRequestBody.Length.ToString() }
                    }
                            };
                            var badBody = new BufferedProducer(badRequestBody);

                            response.OnResponse(badRequestHeaders, badBody);
                        }

                        //var requestObj = (objectParam.ParameterType) JSON.DeserializeDynamic(bufferedBody);

                        object classInstance = Activator.CreateInstance(type, new object[] { });

                        //System.Reflection.TargetParameterCountException: 'Parameter count mismatch.'//Return 400...
                        //var responderResponse = (HttpResponder)urlExists.Value.Invoke(classInstance, new object[] { actualRouteParams, actualParams, requestObj });
                        var responderResponse = (HttpResponder)urlExists.Value.Invoke(classInstance, totalParamObjects.ToArray());
                        //System.ArgumentException: 'Object of type 'System.Int32' cannot be converted to type 'System.String'.'
                        var body = string.Format("{0}", Converter.SerializeObject(responderResponse?.Value));

                        var headers = new HttpResponseHead()
                        {
                            Status = responderResponse.StatusDescription,
                            Headers = new Dictionary<string, string>()
                        {
                            { "Connection", "close" },
                            { "Content-Length", body.Length.ToString() },
                        }
                        };
                        if (request.Headers.ContainsKey("Content-Type"))
                            headers.Headers["Content-Type"] = request.Headers["Content-Type"];
                        else headers.Headers["Content-Type"] = "application/json";

                        response.OnResponse(headers, new BufferedProducer(body));


                    }, error =>
                    {
                        //    var responseBody = "An errror occurred while processing the request.";
                        //    var headers = new HttpResponseHead()
                        //    {
                        //        Status = "500 Internal Server Error",
                        //        Headers = new Dictionary<string, string>()
                        //{
                        //    { "Content-Type", "text/plain" },
                        //    { "Content-Length", responseBody.Length.ToString() }
                        //}
                        //    };
                        //    var body = new BufferedProducer(responseBody);

                        //    response.OnResponse(headers, new BufferedProducer(body));

                        throw new PipelineException(error);




                    }));

                }
                else
                {
                    Console.WriteLine("This is For HTTP PUT, DELETE, PATCH, OPTIONS etc");
                }

            }



        }

        private new List<object> ExtractInvokationParameters(HttpRequestHead request, KeyValuePair<string, MethodInfo> urlExists, string bufferedBody = null)
        {
            var totalParamObjects = new List<object>();
            ParameterInfo[] parameters = urlExists.Value.GetParameters();
            //actualParams = default;
            //actualRouteParams = default;
            Dictionary<string, string> queryDict = new Dictionary<string, string>();
            object[] actualParams, actualRouteParams;

            string[] anyRouteParams;
            var routeParamDict = new Dictionary<string, string>();

            if (request.QueryString != null)
            {
                var items = request.QueryString.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
.Select(s => s.Split(new[] { '=' }));
                foreach (var item in items)
                {
                    queryDict.Add(item[0], item[1]);
                }
            }
            var routeTemplateSegments = new Uri(string.Concat(this._prefix, urlExists.Key)).Segments;

            Uri uri = new Uri(string.Concat(this._prefix, request.Uri.Substring(1, request.Uri.Length - 1)));
            anyRouteParams = uri.Segments.Where(x => !routeTemplateSegments.Contains(x)).ToArray();
            var pathIndex = 0;
            if (parameters != null && parameters.Any())
            {
                foreach (var item in parameters)
                {
                    if (request.QueryString != null && queryDict.ContainsKey(item.Name) && item.GetCustomAttributes().OfType<QueryParameterAttribute>().Any() && (item.ParameterType.IsPrimitive || item.ParameterType == typeof(Decimal) || item.ParameterType == typeof(String)))
                    {
                        totalParamObjects.Add(Convert.ChangeType(queryDict[item.Name], item.ParameterType));
                    }
                    else if (anyRouteParams.Any() && item.GetCustomAttributes().OfType<PathParameterAttribute>().Any() && (item.ParameterType.IsPrimitive || item.ParameterType == typeof(Decimal) || item.ParameterType == typeof(String)))
                    {

                        totalParamObjects.Add(Convert.ChangeType(anyRouteParams[pathIndex].ToString().Replace("/", string.Empty), item.ParameterType));
                        pathIndex++;

                    }
                    else if (!string.IsNullOrWhiteSpace(bufferedBody) && item.GetCustomAttributes().OfType<JsonBodyAttribute>().Any() && !item.ParameterType.IsPrimitive && item.ParameterType != typeof(Decimal) && item.ParameterType != typeof(String))
                    {
                        var requestObj = JsonConvert.DeserializeObject(bufferedBody, item.ParameterType);
                        totalParamObjects.Add(requestObj);

                    }
                    else
                    {

                    }


                }

            }

            return totalParamObjects;
        }
    }
    //class RequestDelegate : IHttpRequestDelegate
    //{
    //    public void OnRequest(HttpRequestHead request, IDataProducer requestBody,
    //            IHttpResponseDelegate response)
    //    {
    //        if (request.Method.ToUpperInvariant() == "POST" && request.Uri.StartsWith("/bufferedecho"))
    //        {
    //            // when you subecribe to the request body before calling OnResponse,
    //            // the server will automatically send 100-continue if the client is 
    //            // expecting it.
    //            requestBody.Connect(new BufferedConsumer(bufferedBody =>
    //            {
    //                var headers = new HttpResponseHead()
    //                {
    //                    Status = "200 OK",
    //                    Headers = new Dictionary<string, string>()
    //                            {
    //                                //{ "Content-Type", "text/plain" },
    //                                { "Content-Length", request.Headers["Content-Length"] },
    //                                { "Connection", "close" }
    //                            }
    //                };
    //                if (request.Headers.ContainsKey("Content-Type"))
    //                    headers.Headers["Content-Type"] = request.Headers["Content-Type"];
    //                else headers.Headers["Content-Type"] = "text/plain";
    //                response.OnResponse(headers, new BufferedProducer(bufferedBody));
    //            }, error =>
    //            {
    //                // XXX
    //                // uh oh, what happens?
    //            }));
    //        }
    //        else if (request.Method.ToUpperInvariant() == "POST" && request.Uri.StartsWith("/echo"))
    //        {
    //            var headers = new HttpResponseHead()
    //            {
    //                Status = "200 OK",
    //                Headers = new Dictionary<string, string>()
    //                    {
    //                        //{ "Content-Type", "text/plain" },
    //                        { "Connection", "close" }
    //                    }
    //            };
    //            if (request.Headers.ContainsKey("Content-Length"))
    //                headers.Headers["Content-Length"] = request.Headers["Content-Length"];
    //            if (request.Headers.ContainsKey("Content-Type"))
    //                headers.Headers["Content-Type"] = request.Headers["Content-Type"];
    //            else headers.Headers["Content-Type"] = "text/plain";

    //            // if you call OnResponse before subscribing to the request body,
    //            // 100-continue will not be sent before the response is sent.
    //            // per rfc2616 this response must have a 'final' status code,
    //            // but the server does not enforce it.
    //            response.OnResponse(headers, requestBody);
    //        }
    //        else if (request.Uri.StartsWith("/"))
    //        {
    //            var body = string.Format(
    //                "Hello world.\r\nHello.\r\n\r\nUri: {0}\r\nPath: {1}\r\nQuery:{2}\r\nFragment: {3}\r\n",
    //                request.Uri,
    //                request.Path,
    //                request.QueryString,
    //                request.Fragment);

    //            var headers = new HttpResponseHead()
    //            {
    //                Status = "200 OK",
    //                Headers = new Dictionary<string, string>()
    //                {
    //                    //{ "Content-Type", "text/plain" },
    //                    //{ "Content-Type", responseCon },
    //                    { "Content-Length", body.Length.ToString() },
    //                }
    //            };
    //            if (request.Headers.ContainsKey("Content-Type"))
    //                headers.Headers["Content-Type"] = request.Headers["Content-Type"];
    //            else headers.Headers["Content-Type"] = "text/plain";

    //            response.OnResponse(headers, new BufferedProducer(body));
    //        }
    //        else
    //        {
    //            var responseBody = "The resource you requested ('" + request.Uri + "') could not be found.";
    //            var headers = new HttpResponseHead()
    //            {
    //                Status = "404 Not Found",
    //                Headers = new Dictionary<string, string>()
    //                {
    //                    { "Content-Type", "text/plain" },
    //                    { "Content-Length", responseBody.Length.ToString() }
    //                }
    //            };
    //            var body = new BufferedProducer(responseBody);

    //            response.OnResponse(headers, body);
    //        }
    //    }
    //}


    class BufferedProducer : IDataProducer
    {
        ArraySegment<byte> data;

        public BufferedProducer(string data) : this(data, Encoding.UTF8) { }
        public BufferedProducer(string data, Encoding encoding) : this(encoding.GetBytes(data)) { }
        public BufferedProducer(byte[] data) : this(new ArraySegment<byte>(data)) { }
        public BufferedProducer(ArraySegment<byte> data)
        {
            this.data = data;
        }

        public IDisposable Connect(IDataConsumer channel)
        {
            // null continuation, consumer must swallow the data immediately.
            channel.OnData(data, null);
            channel.OnEnd();
            return null;
        }
    }

    class BufferedConsumer : IDataConsumer
    {
        List<ArraySegment<byte>> buffer = new List<ArraySegment<byte>>();
        Action<string> resultCallback;
        Action<Exception> errorCallback;

        public BufferedConsumer(Action<string> resultCallback,
    Action<Exception> errorCallback)
        {
            this.resultCallback = resultCallback;
            this.errorCallback = errorCallback;
        }
        public bool OnData(ArraySegment<byte> data, Action continuation)
        {
            // since we're just buffering, ignore the continuation. 
            // TODO: place an upper limit on the size of the buffer. 
            // don't want a client to take up all the RAM on our server! 
            buffer.Add(data);
            return false;
        }
        public void OnError(Exception error)
        {
            errorCallback(error);
        }

        public void OnEnd()
        {
            // turn the buffer into a string. 
            // 
            // (if this isn't what you want, you could skip 
            // this step and make the result callback accept 
            // List<ArraySegment<byte>> or whatever) 
            // 
            var str = buffer
                .Select(b => Encoding.UTF8.GetString(b.Array, b.Offset, b.Count))
                .Aggregate((result, next) => result + next);

            resultCallback(str);
        }
    }

}

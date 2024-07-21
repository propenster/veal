using Kayak;
using Kayak.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
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
        private HttpAppServer _app;
        public RequestDelegate(HttpAppServer app)
        {
            _app = app;
        }
        private bool DoesActualUrlMatchTemplatePattern(string actualRequestUrl, string routeTemplateUrl)
        {
            var result = false;

            var afterTemplate2 = Regex.Replace(routeTemplateUrl, @":(?<type>\w+)\}", "}");
            //Console.WriteLine("URL after removal of DataType Names >>> {0}", afterTemplate2);
            //var afterTemplate2 = "vealpostrequesturl/id/{id}/orders?orderId={orderId}&itemId={itemId}"; //remove the types...
            var matches2 = Regex.Matches(afterTemplate2, @"\{(?<variable>\w+)\}");
            //Console.WriteLine("After removing the DataTypeNames, we get {0} matches", matches2.Count);

            var cleanUrl = Regex.Replace(afterTemplate2, @"\{([^}]+)\}", "$1");
            //Console.WriteLine("Clean URL after all the routeParameter processing is done >>> {0}", cleanUrl);

            //var matches3 = Regex.Matches(afterTemplate3, @"\{(?<variable>\w+)\}");
            var xxx = cleanUrl.Replace(_app.Prefix, "/");
            //result = Regex.Matches(xxx, @"\{(?<variable>\w+)\}").Count > 0 && Regex.Matches(actualRequestUrl, @"\{(?<variable>\w+)\}").Count > 0;
            //var similarity = findSimilarity(xxx, actualRequestUrl);
            var similarity = similarStringsPercentage(xxx, actualRequestUrl);
            //Console.WriteLine("Similarity of strings >>> {0}", similarity);
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

            var actualUrlSegments = new Uri(string.Concat(_app.Prefix, actualUrl.Substring(1, actualUrl.Length - 1))).Segments;
            var actualUrlQueryParams = HttpUtility.ParseQueryString(actualUrl);
            var qpA = !actualUrlQueryParams.HasKeys() ? 0 : actualUrlQueryParams.Count;

            return (routeTemplateSegments.Length == actualUrlSegments.Length) && (qpT == qpA);

        }
        static string RemoveBaseUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri)) return url;
            return uri.PathAndQuery;
        }
        public void OnRequest(HttpRequestHead request, IDataProducer requestBody,
                IHttpResponseDelegate response)
        {
            //Console.WriteLine($"Received {request.Method.ToUpperInvariant()} request for {request.Uri} ABSOLUTEURL >>> {_prefix}{request.Path.Replace("/", string.Empty)}");
            //var urlExists = _app.ActionList.FirstOrDefault(x => x.Key.Replace(_app.Prefix, "/").Contains(request.Uri));
            var urlExists = _app.ActionList.FirstOrDefault(x => RemoveBaseUrl(x.Key) == request.Uri);
            if (string.IsNullOrWhiteSpace(urlExists.Key))
            {
                urlExists = _app.ActionList.FirstOrDefault(x => IsUrlDefined(x.Key, request.Uri));
            }

            if (string.IsNullOrWhiteSpace(urlExists.Key))
            {
                //or maybe the requestUri is a static file e.g index.html, 


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
                return;
            }
            else
            {
                //capture routeParameter values... if any?
                //

                //check for ActionFilters....
                //check for Middlewares... later feature...

                var actionExecutingContext = new ActionExecutingContext
                {
                    App = _app,
                    Body = string.Empty,
                    Request = request,
                    Result = null,
                };

                var totalParamObjects = new List<object>();

                //Console.WriteLine("What we are going to do if it's not 404...Of course we will handle more cases 401, 403, etc");
                if (request.Method.ToUpperInvariant() == "GET")
                {
                    TryHandleAuthorizationFilters(urlExists, response, request, actionExecutingContext, string.Empty, out bool shouldShortCircuitAuth, out HttpResponseHead authHeaders, out BufferedProducer authResponseBody);
                    if (shouldShortCircuitAuth)
                    {
                        response.OnResponse(authHeaders, authResponseBody);
                        return;
                    }
                    TryHandleActionFilters(urlExists, response, request, actionExecutingContext, string.Empty, out bool shouldShortCircuit, out HttpResponseHead headers, out BufferedProducer responseBody);

                    if (shouldShortCircuit)
                    {
                        response.OnResponse(headers, responseBody);
                        return;
                    }
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
                        return;
                    }

                    var responderResponse = (HttpResponder)urlExists.Value.Invoke(classInstance, totalParamObjects.ToArray());
                    //Debug.WriteLine(responderResponse.ToString());
                    //Console.WriteLine(responderResponse.ToString());
                    var body = responderResponse.ContentType.Contains("json") ? string.Format("{0}", Converter.SerializeObject(responderResponse?.Value)) : responderResponse.Value.ToString();

                    //headers = new HttpResponseHead()
                    //{
                    //    Status = responderResponse.StatusDescription,
                    //    Headers = new Dictionary<string, string>()
                    //{
                    //    { "Connection", "close" },
                    //    { "Content-Length", body.Length.ToString() },
                    //}
                    //};
                    headers = new HttpResponseHead()
                    {
                        Status = responderResponse.StatusDescription,
                        Headers = new Dictionary<string, string>()
                    {
                        { "Connection", "close" },
                        { "Content-Length", responderResponse.ContentLength.ToString() },
                    }
                    };
                    if (request.Headers.ContainsKey("Content-Type"))
                        headers.Headers["Content-Type"] = request.Headers["Content-Type"];
                    //else headers.Headers["Content-Type"] = "application/json";
                    else headers.Headers["Content-Type"] = responderResponse.ContentType;

                    response.OnResponse(headers, new BufferedProducer(body));
                    return;
                }
                else if (request.Method.ToUpperInvariant() == "DELETE")
                {
                    //remember to subscribe to body for delete
                    TryHandleActionFilters(urlExists, response, request, actionExecutingContext, string.Empty, out bool shouldShortCircuit, out HttpResponseHead headers, out BufferedProducer responseBody);

                    if (shouldShortCircuit)
                    {
                        response.OnResponse(headers, responseBody);
                        return;
                    }
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
                        return;
                    }

                    var responderResponse = (HttpResponder)urlExists.Value.Invoke(classInstance, totalParamObjects.ToArray());
                    Debug.WriteLine(responderResponse.ToString());
                    Console.WriteLine(responderResponse.ToString());
                    var body = string.Format("{0}", Converter.SerializeObject(responderResponse?.Value));

                    headers = new HttpResponseHead()
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
                    return;
                }
                else if (request.Method.ToUpperInvariant() == "POST")
                {
                    requestBody.Connect(new BufferedConsumer(bufferedBody =>
                    {

                        //distribute this code of ActionExecutionContext to all aCTIONfILTERS IN THE REQUEST PIPeline

                        //check the current Action has an ActionFilter... grab all action filters that it has...
                        //and chronologically (meaning in order they are added to the action in ActionFilterAttribute) hookup and invoke them

                        //var actionFiltersForThisAction = urlExists.Value.CustomAttributes.Where(x => x.AttributeType == typeof(ActionFilterAttribute)).Select(y => y.ConstructorArguments.OfType<IActionFilter>());

                        TryHandleActionFilters(urlExists, response, request, actionExecutingContext, bufferedBody, out bool shouldShortCircuit, out HttpResponseHead headers, out BufferedProducer responseBody);

                        if (shouldShortCircuit)
                        {
                            response.OnResponse(headers, responseBody);
                            return;
                        }

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
                            return;
                        }
                        object classInstance = Activator.CreateInstance(type, new object[] { });

                        //System.Reflection.TargetParameterCountException: 'Parameter count mismatch.'//Return 400...
                        var responderResponse = (HttpResponder)urlExists.Value.Invoke(classInstance, totalParamObjects.ToArray());
                        //System.ArgumentException: 'Object of type 'System.Int32' cannot be converted to type 'System.String'.'
                        var body = string.Format("{0}", Converter.SerializeObject(responderResponse?.Value));

                        headers = new HttpResponseHead()
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
                        return;


                    }, error =>
                    {

                        throw new PipelineException(error);




                    }));

                }
                else
                {
                    Console.WriteLine("This is For HTTP PUT, DELETE, PATCH, OPTIONS etc");
                }

            }



        }

        private void TryHandleAuthorizationFilters(KeyValuePair<string, MethodInfo> urlExists, IHttpResponseDelegate response, HttpRequestHead request, ActionExecutingContext context, string requestBody, out bool shouldShortCircuit, out HttpResponseHead headers, out BufferedProducer responseBody)
        {
            shouldShortCircuit = false;
            headers = new HttpResponseHead()
            {
                Status = "200 OK",
                Headers = new Dictionary<string, string>()
                        {
                            { "Connection", "close" },
                            { "Content-Length", "500".ToString() },
                        }
            };
            responseBody = new BufferedProducer(string.Empty);

            if (request.Headers.ContainsKey("Content-Type"))
                headers.Headers["Content-Type"] = request.Headers["Content-Type"];
            else headers.Headers["Content-Type"] = "application/json";



            var authorizeAttributesForThisAction = urlExists.Value
                   .GetCustomAttributes<AuthorizeAttribute>();
            if (authorizeAttributesForThisAction != null && authorizeAttributesForThisAction.Any())
            {
                foreach (var item in authorizeAttributesForThisAction)
                {
                    Type authHandlerFilterType = item.AuthHandlerType;
                    Type interfaceType = typeof(IAuthorizationFilter);

                    if (interfaceType.IsAssignableFrom(authHandlerFilterType))
                    {
                        object objInstance = Activator.CreateInstance(authHandlerFilterType, new object[] { });
                        var onAuthenticationMethod = authHandlerFilterType.GetMethod(Defaults.OnAuthenticationMethod);

                        if (request.Method.ToUpperInvariant() == "POST" || request.Method.ToUpperInvariant() == "PUT") context.Body = requestBody;

                        var authFilterResponse = (HttpResponder)onAuthenticationMethod.Invoke(objInstance, new object[] { context, item.Scheme });

                        if (authFilterResponse != null)
                        {
                            var bodyAc = string.Format("{0}", Converter.SerializeObject(authFilterResponse?.Value));
                            headers = new HttpResponseHead()
                            {
                                Status = authFilterResponse.StatusDescription,
                                Headers = new Dictionary<string, string>()
                        {
                            { "Connection", "close" },
                            { "Content-Length", bodyAc.Length.ToString() },
                        }
                            };
                            if (request.Headers.ContainsKey("Content-Type"))
                                headers.Headers["Content-Type"] = request.Headers["Content-Type"];
                            else headers.Headers["Content-Type"] = "application/json";

                            shouldShortCircuit = true;
                            responseBody = new BufferedProducer(bodyAc);

                        }
                    }

                }

            }
        }


        private void TryHandleActionFilters(KeyValuePair<string, MethodInfo> urlExists, IHttpResponseDelegate response, HttpRequestHead request, ActionExecutingContext context, string requestBody, out bool shouldShortCircuit, out HttpResponseHead headers, out BufferedProducer responseBody)
        {
            shouldShortCircuit = false;
            headers = new HttpResponseHead()
            {
                Status = "200 OK",
                Headers = new Dictionary<string, string>()
                        {
                            { "Connection", "close" },
                            { "Content-Length", "500".ToString() },
                        }
            };
            responseBody = new BufferedProducer(string.Empty);
            if (request.Headers.ContainsKey("Content-Type"))
                headers.Headers["Content-Type"] = request.Headers["Content-Type"];
            else headers.Headers["Content-Type"] = "application/json";

            var actionFiltersForThisAction = urlExists.Value
                   .GetCustomAttributes<ActionFilterAttribute>();
            if (actionFiltersForThisAction != null && actionFiltersForThisAction.Any())
            {
                foreach (var item in actionFiltersForThisAction)
                {
                    Type filterType = item.FilterType;
                    Type interfaceType = typeof(IActionFilter);

                    if (interfaceType.IsAssignableFrom(filterType))
                    {
                        //ConstructorInfo constructor = filterType.GetConstructor(new[] { typeof(string) });
                        //if (constructor != null)
                        //{
                        //    IActionFilter filterInstance = (IActionFilter)constructor.Invoke(new object[] { constructorParameterValue });

                        //    // Now you have an instance of the filter with the parameter set
                        //    // You can call the necessary methods on the filterInstance
                        //}
                        object objInstance = Activator.CreateInstance(filterType, new object[] { });
                        var onActionExecutingMethod = filterType.GetMethod(Defaults.OnActionExecutingMethod);

                        if (request.Method.ToUpperInvariant() == "POST" || request.Method.ToUpperInvariant() == "PUT") context.Body = requestBody;


                        var actionFilterResponse = (HttpResponder)onActionExecutingMethod.Invoke(objInstance, new object[] { context });

                        if (actionFilterResponse != null)
                        {
                            //NewMethod(request, response, actionFilterResponse);
                            var bodyAc = string.Format("{0}", Converter.SerializeObject(actionFilterResponse?.Value));

                            headers = new HttpResponseHead()
                            {
                                Status = actionFilterResponse.StatusDescription,
                                Headers = new Dictionary<string, string>()
                        {
                            { "Connection", "close" },
                            { "Content-Length", bodyAc.Length.ToString() },
                        }
                            };
                            if (request.Headers.ContainsKey("Content-Type"))
                                headers.Headers["Content-Type"] = request.Headers["Content-Type"];
                            else headers.Headers["Content-Type"] = "application/json";

                            shouldShortCircuit = true;
                            //headers = headersAc;
                            responseBody = new BufferedProducer(bodyAc);

                            //response.OnResponse(headersAc, new BufferedProducer(bodyAc));


                        }
                    }

                }

            }
        }

        private static void NewMethod(HttpRequestHead request, IHttpResponseDelegate response, HttpResponder responderResponse)
        {
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
            var routeTemplateSegments = new Uri(string.Concat(_app.Prefix, urlExists.Key)).Segments;

            Uri uri = new Uri(string.Concat(_app.Prefix, request.Uri.Substring(1, request.Uri.Length - 1)));
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

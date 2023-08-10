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

        public RequestDelegate(string prefix, HashSet<KeyValuePair<string, MethodInfo>> actionList)
        {
            _prefix = prefix;
            this.actionList = actionList;
        }

        public void OnRequest(HttpRequestHead request, IDataProducer requestBody,
                IHttpResponseDelegate response)
        {
            Console.WriteLine($"Received {request.Method.ToUpperInvariant()} request for {request.Uri} ABSOLUTEURL >>> {_prefix}{request.Path.Replace("/", string.Empty)}");

            var urlExists = this.actionList.FirstOrDefault(x => x.Key.Contains(request.Uri));

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
                Console.WriteLine("What we are going to do if it's not 404...Of course we will handle more cases 401, 403, etc");
                if (request.Method.ToUpperInvariant() == "GET")
                {
                    var param = request.QueryString;

                    var type = urlExists.Value.DeclaringType;
                    ParameterInfo[] parameters = urlExists.Value.GetParameters();
                    object classInstance = Activator.CreateInstance(type, new object[] { });

                    var responderResponse = (HttpResponder)urlExists.Value.Invoke(classInstance, parameters);
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
                    else headers.Headers["Content-Type"] = "text/plain";

                    response.OnResponse(headers, new BufferedProducer(body));
                }
                else if (request.Method.ToUpperInvariant() == "POST")
                {
                    requestBody.Connect(new BufferedConsumer(bufferedBody =>
                    {
                        Console.WriteLine("Intercepted Body in the request {0}", bufferedBody);

                        var param = request.QueryString;
                        //handle query or route parameters here...
                        //reflect them to actual parameter objects that we will pass to the action method

                        var type = urlExists.Value.DeclaringType;

                        ParameterInfo[] parameters = urlExists.Value.GetParameters();
                        var objectParam = parameters.FirstOrDefault(x => !x.ParameterType.IsPrimitive && x.ParameterType != typeof(Decimal) && x.ParameterType != typeof(String));
                        var requestObj = JsonConvert.DeserializeObject(bufferedBody, objectParam.ParameterType);
                        //var requestObj = (objectParam.ParameterType) JSON.DeserializeDynamic(bufferedBody);

                        object classInstance = Activator.CreateInstance(type, new object[] { });
                        //var responderResponse = (HttpResponder)urlExists.Value.Invoke(classInstance, parameters);
                        var responderResponse = (HttpResponder)urlExists.Value.Invoke(classInstance, new object[] { requestObj });

                        //responderResponse.ToListenerResponse(resp);
                        Debug.WriteLine(responderResponse.ToString());
                        Console.WriteLine(responderResponse.ToString());
                        var body = string.Format("{0}", Converter.SerializeObject(responderResponse?.Value));

                        var headers = new HttpResponseHead()
                        {
                            Status = responderResponse.StatusDescription,
                            Headers = new Dictionary<string, string>()
                    {
                        //{ "Content-Type", "text/plain" },
                            { "Connection", "close" },
                        { "Content-Length", body.Length.ToString() },
                    }
                        };
                        if (request.Headers.ContainsKey("Content-Type"))
                            headers.Headers["Content-Type"] = request.Headers["Content-Type"];
                        else headers.Headers["Content-Type"] = "text/plain";

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


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kayak;
using Kayak.Http;
using System.Text.RegularExpressions;
using System.Collections.Immutable;

namespace Veal
{
    internal interface IHttpAppServer
    {
        void Run();
        HttpAppServer Bind(string prefix);
        HttpAppServer Setup();
    }
    public class HttpAppServer : IHttpAppServer
    {
        public HashSet<string> ServiceList { get; set; }
        public string Prefix { get; set; }
        public HashSet<KeyValuePair<string, MethodInfo>> ActionList { get; set; } = new HashSet<KeyValuePair<string, MethodInfo>>();
        public HashSet<RouteValueModel> RouteValueDictionary { get; set; } = new HashSet<RouteValueModel>();

        CancellationTokenSource tokenSource;
        /// <summary>
        /// Bind a baseURL e.g. http://127.0.0.1:8083/
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public HttpAppServer Bind(string prefix)
        {
            this.Prefix = prefix.Trim();
            this.Prefix = this.Prefix[this.Prefix.Length - 1] != '/' ? string.Format("{0}{1}", this.Prefix, "/") : this.Prefix;

            return this;
        }
        /// <summary>
        /// Start listening for requests and treating
        /// </summary>
        public void Run()
        {
            var scheduler = KayakScheduler.Factory.Create(new SchedulerDelegate());
            var server = KayakServer.Factory.CreateHttp(new RequestDelegate(this.Prefix, this.ActionList, this.RouteValueDictionary), scheduler);
            if(this.Prefix.ToLowerInvariant().Contains("localhost"))
            {
                this.Prefix = this.Prefix.ToLowerInvariant().Replace("localhost", "127.0.0.1");
            }
            var ipFromPrefix = (Regex.Match(this.Prefix, @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})"))?.Value;
            IPAddress ipAddress = IPAddress.Parse(ipFromPrefix.Trim());
            var port = int.Parse(this.Prefix.Split(':').LastOrDefault().Replace("/", string.Empty));

            //using (server.Listen(new IPEndPoint(IPAddress.Any, 8080)))
            using (server.Listen(new IPEndPoint(ipAddress, port)))
            {
                // runs scheduler on calling thread. this method will block until
                // someone calls Stop() on the scheduler.
                Console.WriteLine("Listening on Kayak Server v1.0.0 on IP {0} on port {1}...", ipAddress.ToString(), port);

                scheduler.Start();

            }
        }
        /// <summary>
        /// Collates all Action Methods and grabs their info - Route, Name etc...
        /// </summary>
        /// <returns></returns>
        public HttpAppServer Setup()
        {
            //if (services is null || !services.Any()) throw new ArgumentNullException(nameof(services));
            var pattern = @"\{(?<variable>\w+):(?<type>\w+)\}";


            var methods = Assembly.GetEntryAssembly().GetTypes()
                .SelectMany(x => x.GetMethods())
                .Where(y => y.IsPublic && y.GetCustomAttributes().OfType<ActionAttribute>().Any() && y.ReturnType == typeof(HttpResponder) || y.ReturnType == typeof(Task<HttpResponder>))
                    .GroupBy(x => x.Name)
                .ToDictionary(z => z.Key, z => z.FirstOrDefault());

            foreach (var kvp in methods)
            {
                var actionAttr = (ActionAttribute)kvp.Value.GetCustomAttributes(typeof(ActionAttribute), true)[0];
                var _route = actionAttr._Route;
                _route = _route[0] == '/' ? _route.Substring(1, _route.Length - 1) : _route;
                _route = _route.Contains("//") ? _route.Replace("//", "/") : _route;
                var url = new Uri(string.Format("{0}{1}", this.Prefix, _route)).ToString();
                this.ActionList.Add(new KeyValuePair<string, MethodInfo>(url, kvp.Value));

                MatchCollection matches = Regex.Matches(url, pattern);

                Dictionary<string, RouteValue> routeValueDictionary = matches.Cast<Match>().ToDictionary(
                match => match.Groups["variable"].Value,
                //match => match.Groups["type"].Value
                match => new RouteValue { Route = url, DataTypeName = match.Groups["type"].Value }

            );
                var routeValue = new RouteValueModel
                {
                    Route = url,
                    TransformedRouteTemplate = TransformRouteTemplate(url),
                    RouteValues = routeValueDictionary
                };

                this.RouteValueDictionary.Add(routeValue);
            }
            return this;
        }
        //public HttpAppServer Services(IList<string> services)
        //{
        //    if (services is null || !services.Any()) throw new ArgumentNullException(nameof(services));
        //    var pattern = @"\{(?<variable>\w+):(?<type>\w+)\}";


        //    var methods = Assembly.GetEntryAssembly().GetTypes()
        //        .SelectMany(x => x.GetMethods())
        //        .Where(y => y.IsPublic && y.GetCustomAttributes().OfType<ActionAttribute>().Any() && y.ReturnType == typeof(HttpResponder) || y.ReturnType == typeof(Task<HttpResponder>))
        //            .GroupBy(x => x.Name)
        //        .ToDictionary(z => z.Key, z => z.FirstOrDefault());

        //    foreach (var kvp in methods)
        //    {
        //        var actionAttr = (ActionAttribute)kvp.Value.GetCustomAttributes(typeof(ActionAttribute), true)[0];
        //        var _route = actionAttr._Route;
        //        _route = _route[0] == '/' ? _route.Substring(1, _route.Length - 1) : _route;
        //        _route = _route.Contains("//") ? _route.Replace("//", "/") : _route;

        //        //if (!new Uri(string.Format("{0}{1}", this.Prefix, _route)).IsWellFormedOriginalString()) throw new ArgumentException(nameof(_route));

        //        var url = new Uri(string.Format("{0}{1}", this.Prefix, _route)).ToString();
        //        this.ActionList.Add(new KeyValuePair<string, MethodInfo>(url, kvp.Value));

        //        MatchCollection matches = Regex.Matches(url, pattern);

        //        Dictionary<string, RouteValue> routeValueDictionary = matches.Cast<Match>().ToDictionary(
        //        match => match.Groups["variable"].Value,
        //        //match => match.Groups["type"].Value
        //        match => new RouteValue { Route = url, DataTypeName = match.Groups["type"].Value }

        //    );
        //        var routeValue = new RouteValueModel
        //        {
        //            Route = url,
        //            TransformedRouteTemplate = TransformRouteTemplate(url),
        //            RouteValues = routeValueDictionary
        //        };

        //        this.RouteValueDictionary.Add(routeValue);

        //        //matches.Cast<Match>().Select(x => new Dictionary<string, RouteValue>(x.Groups["variable"].Value, new RouteValue { DataTypeName = x.Groups["type"].Value }));

        //        //        .ToDictionary<string, RouteValue>(
        //        //    match => match.Groups["variable"].Value,
        //        //    match => new RouteValue { DataTypeName = match.Groups["type"].Value,  } 
        //        //);


        //    }
        //    return this;
        //}

        private string TransformRouteTemplate(string rawUrlTemplate)
        {
            var afterTemplate2 = Regex.Replace(rawUrlTemplate, @":(?<type>\w+)\}", "}");
            Console.WriteLine("URL after removal of DataType Names >>> {0}", afterTemplate2);

            return afterTemplate2;
        }

    }
}

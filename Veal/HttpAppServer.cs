
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

namespace Veal
{
    internal interface IHttpAppServer
    {
        void Run();
        HttpAppServer Bind(string prefix);
        HttpAppServer Services(IList<string> services);
    }
    public class HttpAppServer : IHttpAppServer
    {
        public HashSet<string> ServiceList { get; set; }
        public string Prefix { get; set; }
        public HashSet<KeyValuePair<string, MethodInfo>> ActionList { get; set; } = new HashSet<KeyValuePair<string, MethodInfo>>();

        CancellationTokenSource tokenSource;
        public HttpAppServer Bind(string prefix)
        {
            this.Prefix = prefix.Trim();
            this.Prefix = this.Prefix[this.Prefix.Length - 1] != '/' ? string.Format("{0}{1}", this.Prefix, "/") : this.Prefix;

            return this;
        }
        public void Run()
        {
            var scheduler = KayakScheduler.Factory.Create(new SchedulerDelegate());
            var server = KayakServer.Factory.CreateHttp(new RequestDelegate(this.Prefix, this.ActionList), scheduler);
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
        public HttpAppServer Services(IList<string> services)
        {
            if (services is null || !services.Any()) throw new ArgumentNullException(nameof(services));

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

                if (!new Uri(string.Format("{0}{1}", this.Prefix, _route)).IsWellFormedOriginalString()) throw new ArgumentException(nameof(_route));

                var url = new Uri(string.Format("{0}{1}", this.Prefix, _route)).ToString();
                this.ActionList.Add(new KeyValuePair<string, MethodInfo>(url, kvp.Value));
            }
            return this;
        }

    }
}

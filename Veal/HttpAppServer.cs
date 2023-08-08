﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

        public HttpAppServer Bind(string prefix)
        {
            this.Prefix = prefix.Trim();
            this.Prefix = this.Prefix[this.Prefix.Length - 1] != '/' ? string.Format("{0}{1}", this.Prefix, "/") : this.Prefix;

            return this;
        }

        public void Run()
        {
            if (string.IsNullOrWhiteSpace(this.Prefix)) throw new ArgumentNullException(nameof(this.Prefix));
            var listener = new HttpListener();
            listener.Prefixes.Add(this.Prefix);
            listener.Start();

            Console.WriteLine("Listening on port {0}...", this.Prefix.Split(':').LastOrDefault().Replace("/", string.Empty));

            while (true)
            {
                HttpListenerContext ctx = listener.GetContext();
                foreach (var item in ActionList)
                {
                    if (ctx.Request.Url.ToString().Contains(item.Key)) continue; //return 404 instead of continue...
                    Console.WriteLine($"Received request for {ctx.Request.Url}");

                    using (var resp = ctx.Response)
                    {
                        var param = ctx.Request.QueryString;
                        var type = item.Value.DeclaringType.Assembly.GetType();

                        ParameterInfo[] parameters = item.Value.GetParameters();
                        object classInstance = Activator.CreateInstance(type);

                        var response = (HttpResponder)item.Value.Invoke(classInstance, parameters);

                        response.ToListenerResponse(resp);

                        byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response.Value)); //of course we want to only do this if requestContentType is application/json or */*
                        resp.ContentLength64 = buffer.Length;

                        using (Stream ros = resp.OutputStream)
                        {
                            ros.Write(buffer, 0, buffer.Length);

                        }

                    }
                }

                //Task.Run(() =>
                //{

                //});


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

            //foreach (var service in services)
            //{
            //    if (!new Uri(string.Format("{0}{1}", this.Prefix, service)).IsWellFormedOriginalString()) throw new ArgumentException(nameof(service));
            //    //{
            //    //    //clean up the Uri here...
            //    //}
            //    this.ServiceList.Add(service);
            //}
            //return this;
        }

    }
    public class Test
    {
        [Get("/hello", "GetHelloEndpoint")]
        public string Hello()
        {
            return "Hello World";
        }
        [Get("/helloasync", "GetHelloAsyncEndpoint")]
        public async Task<HttpResponder> HelloAsync()
        {
            Task t = new Task(() => Console.WriteLine("This is our first Async Action method"));
            await t;
            return HttpResponder.Ok("Hello World Async");
        }
        public void RunTestApp()
        {
            var app = new HttpAppServer().Bind("https://localhost:8001").Services(new List<string> { nameof(Hello), nameof(HelloAsync) });
            app.Run();

        }
    }
}
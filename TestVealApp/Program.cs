﻿using Veal;

internal class Program
{
    [Get("/hello", "GetHelloEndpoint")]
    public HttpResponder Hello()
    {
        return HttpResponder.Ok("Hello World");
    }
    [Get("/helloasync", "GetHelloAsyncEndpoint")]
    public async Task<HttpResponder> HelloAsync()
    {
        Task t = new Task(() => Console.WriteLine("This is our first Async Action method"));
        await t;
        return HttpResponder.Ok("Hello World Async");
    }
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        Console.WriteLine("testing my new Veal App");
        var app = new HttpAppServer().Bind("https://localhost:5001").Services(new List<string> { nameof(Hello), nameof(HelloAsync) });
        app.Run();
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
        //var fieldinfo = typeof(Task).GetField(nameof(HelloAsync), BindingFlags.Instance | BindingFlags.Public);
        //delegate Action = fieldinfo.GetValue(Task) as delegate;

        var app = new HttpAppServer().Bind("https://localhost:8001").Services(new List<string> { nameof(Hello), nameof(HelloAsync) });
        app.Run();
        //Task.Run(() => Hello());
        //Task.Run(() => HelloAsync());

    }
}
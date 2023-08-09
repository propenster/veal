# veal
A lightweight web app development framework in C#

### Let's get on a journey of writing our own lightweight lean web app framework for c#.

* I don't know what features we'll support yet but I'll think of something.

  # To Get Started
   * Create endpoints
     ```csharp
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

     ```

  * Create an HTTPServer in your entry method -> Main
     

  ```csharp
  var app = new HttpAppServer().Bind("http://localhost:4448/").Services(new List<string> { nameof(Hello), nameof(HelloAsync) });

  ```

  * Call app.Run() to start listenting for and treating requests.
    ```csharp
    app.Run();
    ```

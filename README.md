# veal
A lightweight web app development framework in C#

### Let's get on a journey of writing our own lightweight lean web app framework for c#.

 I don't know what features we'll support yet but I'll think of something.

  # To Get Started
  ## Create endpoints
  ```c#
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

    [Post("vealpostrequesturl", "MakeVealPostRequest")]
    public HttpResponder SendVealRequest([JsonBody] VealRequest request)
    {

        var response = new VealResponse { DateOfRequest = DateTime.Now, RequestItem = request };

        return HttpResponder.Ok(response);
    }
     ```
     

  ## Create an HTTPAppServer in your entry method -> Main
  ```c#
  
  var app = new HttpAppServer().Bind("http://localhost:4448/").Services(new List<string> { nameof(Hello), nameof(HelloAsync) });

  ## Call app.Run() to start listenting for and treating requests.

    app.Run();

  ```

# Day 2 - Progress
 * Implement PostAttribute for HTTP POST endpoints
 * Implement ModelBinding
 * We are now able to serve both GET and POST requests via action methods
 * We are also able to handle invalid URIs return 404 response
## See images below for reference.

* Veal Post action using Postman
  
![veal_first_request_v2](https://github.com/propenster/veal/assets/51266654/073321dd-3952-4fbf-8366-40ef6f4a6acf)

* Veal Post action Using Rest Client on VSCode

![veal_first_request_v3](https://github.com/propenster/veal/assets/51266654/fb837a10-8a12-41e5-8b29-1b9ca93735d6)


# Day 3 - Progress
 * Implement More Routing and support for RouteParameters - Path and Query Parameters
 * We need a RouteValueDictionary when OnRequest and we are going to try our possible best to avoid Microsoft.AspNetCore.Http.Routing and in fact any   
   Microsoft.AspNetCore at all. Let's create something simple and less-complicated for the Veal Framework itself. 

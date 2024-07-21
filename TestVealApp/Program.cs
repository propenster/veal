using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using Veal;
using BeetleX;
using TestVealApp;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.IdentityModel.Tokens;

public class Program
{
    [Get("hello", "GetHelloEndpoint")]
    public HttpResponder Hello()
    {
        return HttpResponder.Ok("Hello World This is the Veal Framework... We are trying to make a lightweight, maybe faster web app framework than ASP.NET Core... ");
    }
    [Get("helloasync", "GetHelloAsyncEndpoint")]
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
    [Post("vealpostrequesturl/id/{id:string}", "MakeVealPostRequest")]
    public HttpResponder PostWithQuery([PathParameter]string id, [JsonBody] VealRequest request)
    {
        var response = new VealResponse {id = id, DateOfRequest = DateTime.Now, RequestItem = request };
        return HttpResponder.Ok(response);
    }
    [Get("vealgetwithparameters/id/{id:string}?name={name:string}&age={age:int}", "MakeVealPostRequest")]
    public HttpResponder GetWithParams([PathParameter] string id, [QueryParameter]string name, [QueryParameter]int age)
    {
        var response = new  {  DateOfRequest = DateTime.Now, id = id, name = name, age = age };
        return HttpResponder.Ok(response);
    }
    [Post("vealpostrequesturl/id/{id:string}/orders?orderId={orderId:int}&itemId={itemId:long}", "MakeVealPostWithQueryRequest")]
    public HttpResponder PostWithQueryPlus([PathParameter] string? id, [QueryParameter] int orderId, [QueryParameter] long itemId, [JsonBody] VealRequest request)
    {
        var response = new VealResponse { id = id, orderId = orderId, itemId = itemId, DateOfRequest = DateTime.Now, RequestItem = request };
        return HttpResponder.Ok(response);
    }

    [Post("getwithactionfilter", "GetWithActionFilter")]
    [ActionFilter(typeof(SpecificRequestHeaderActionFilter))]
    public HttpResponder GetWithActionFilter()
    {
        var response = new VealResponse { DateOfRequest = DateTime.Now };
        return HttpResponder.Ok(response);
    }
    [Post("getwithauthorize", "GetWithAuthorize")]
    [Authorize(Defaults.JwtBearerAuthScheme)] //this uses the DefaultAuthHandler, we implemented JwtAuthHandler for you... you can do any other schemes yourself by impelementing the IAuthorizationFilter
    public HttpResponder GetWithAuthorize()
    {
        var response = new VealResponse { DateOfRequest = DateTime.Now };
        return HttpResponder.Ok(response);
    }
    [Post("getwithauthorizedefaultbasicauth", "GetWithAuthorizeCustomAuth")]
    [Authorize(Defaults.BasicAuthScheme)] //we made a default implementation for BasicAuth for you...
    //you can also make your own:
    //[Authorize("Basic", AuthHandlerType = typeof(MyCustomBasicAuthenticationFilter))]
    public HttpResponder GetWithAuthorizeDefaultBasicAuth()
    {
        var response = new VealResponse { DateOfRequest = DateTime.Now };
        return HttpResponder.Ok(response);
    }
    [Post("getwithauthorizecustomauth", "GetWithAuthorizeCustomAuth")]
    //you can also make your own:
    [Authorize("MyCustomAuthScheme", AuthHandlerType = typeof(MyCustomBasicAuthenticationFilter))]
    public HttpResponder GetWithAuthorizeCustomAuth()
    {
        var response = new VealResponse { DateOfRequest = DateTime.Now };
        return HttpResponder.Ok(response);
    }
    [Get("/", "GetHtmlResource")]
    public HttpResponder Index()
    {
        //this takes either the HTML content you want to return or a path to it...
        return HttpResponder.Html("./index.html");
    }
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        Console.WriteLine("testing my new Veal App");

        var jwtSecret = Encoding.ASCII.GetBytes("weyhe12u3ju3u");


        var app = new HttpAppServer().Bind("http://localhost:8449/").Setup().AddOptions(x =>
        {
            x.AuthenticationConfigurations.Add(new JwtConfigurationOption
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateLifetime = false,
                ValidAudience = "http://localhost",
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new SymmetricSecurityKey(jwtSecret),
                ValidIssuer = "http://localhost",
                AuthenticationConfigurations = null,
            });
            x.AuthenticationConfigurations.Add(new BasicAuthenticationOption
            {
                ValidUsername = "MyStrongUsername@12957#",
                ValidPassword = "ThouShallNotPass##@12893"
            });           
            
        });

        try
        {
            app.Run();

        }
        catch (Exception ex)
        {

            Console.WriteLine(ex);
        }
    }


}



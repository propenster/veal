using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using Veal;
using BeetleX;
using TestVealApp;
using System.Text.RegularExpressions;
using System.IO;

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
    private static void TestingRouting()
    {
        var pattern = @"\{(?<variable>\w+):(?<type>\w+)\}";
        //var pathVars = Regex.Matches(routeTemplate, pattern);
        //grab fromPath parameters... use them to replace our pathVars in our ActionUrls...actually action methods...in their order chronologically

        var sampleUrl = "vealpostrequesturl/id/{id:string}/orders?orderId={orderId:int}&itemId={itemId:long}";
        MatchCollection matches = Regex.Matches(sampleUrl, pattern);
        Console.WriteLine("There are {0} route parameter matches ", matches.Count);

        var afterTemplate = "vealpostrequesturl/id/{id:string}/orders?orderId={orderId:int}&itemId={itemId:long}"; //remove the types...
        //let's use Regex to remove the DataTypeNames:
        var afterTemplate2 = Regex.Replace(afterTemplate, @":(?<type>\w+)\}", "}");
        Console.WriteLine("URL after removal of DataType Names >>> {0}", afterTemplate2);
        //var afterTemplate2 = "vealpostrequesturl/id/{id}/orders?orderId={orderId}&itemId={itemId}"; //remove the types...
        var matches2 = Regex.Matches(afterTemplate2, @"\{(?<variable>\w+)\}");
        Console.WriteLine("After removing the DataTypeNames, we get {0} matches", matches2.Count);

        //Now do we still get the same 3 matches when real-world values are in the URL?

        var afterTemplate3 = "vealpostrequesturl/id/{345}/orders?orderId={127839}&itemId={555509}"; //this with real-world scenario values in routeParams
        var matches3 = Regex.Matches(afterTemplate3, @"\{(?<variable>\w+)\}");
        Console.WriteLine("After inserting Real-World Scenario values in RouteParameters, we now get {0} matches", matches3.Count);

        //wow it worked...
        //finally remove curly braces surrounding each of the routeParams

        var cleanUrl = Regex.Replace(afterTemplate3, @"\{([^}]+)\}", "$1");
        Console.WriteLine("Clean URL after all the routeParameter processing is done >>> {0}", cleanUrl);
        //Outpute: vealpostrequesturl/id/345/orders?orderId=127839&itemId=555509
        //You bet it works... 
        //Now we need to return a RouteValueDictionary...
    }
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        Console.WriteLine("testing my new Veal App");
        //var app = new HttpAppServer().Bind("http://localhost:8449/").Services(new List<string> { nameof(Hello), nameof(HelloAsync) });
        var app = new HttpAppServer().Bind("http://localhost:8449/").Setup();

        try
        {
            app.Run();

        }
        catch (Exception ex)
        {

            Console.WriteLine(ex);
        }
    }


    //public static void Main(string[] strings)
    //{
    //    IPAddress localAdd = IPAddress.Parse("127.0.0.1");
    //    TcpListener listener = new TcpListener(localAdd, 3050);
    //    Console.WriteLine("Listening...");
    //    listener.Start();
    //    while (true)
    //    {
    //        //---incoming client connected---
    //        TcpClient client = listener.AcceptTcpClient();
    //        Console.WriteLine("Accepted connect request from client {0} to Server Endpoint {1}", client.Client.RemoteEndPoint, client.Client.LocalEndPoint);

    //        //---get the incoming data through a network stream---
    //        NetworkStream nwStream = client.GetStream();
    //        byte[] buffer = new byte[client.ReceiveBufferSize];

    //        //---read incoming stream---
    //        int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);

    //        //---convert the data received into a string---
    //        string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);
    //        Console.WriteLine("Received Request Data : {0}", dataReceived);

    //        //IF YOU WANT TO WRITE BACK TO CLIENT USE
    //        //string yourmessage = console.ReadLine();
    //        string yourMessage = JsonConvert.SerializeObject(new { response = "Echo OK", time = DateTime.Now });
    //        //byte[] sendBytes = Encoding.UTF8.GetBytes(yourMessage);
    //        byte[] sendBytes = new UTF8Encoding().GetBytes(yourMessage);
    //        //---write back the text to the client---
    //        Console.WriteLine("Sending back : " + yourMessage);

    //        StreamWriter sw = new StreamWriter(client.GetStream());
    //        sw.Write("HTTP/1.0 200 OK\r\n\r\n");
    //        sw.Flush();
    //        nwStream.Write(sendBytes, 0, sendBytes.Length);
    //        //sw.BaseStream.Write(sendBytes, 0, sendBytes.Length);
    //        client.Close();
    //    }
    //    listener.Stop();
    //}




}



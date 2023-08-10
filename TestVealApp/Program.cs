using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using Veal;
using BeetleX;
using TestVealApp;

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
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        Console.WriteLine("testing my new Veal App");
        var app = new HttpAppServer().Bind("http://localhost:8449/").Services(new List<string> { nameof(Hello), nameof(HelloAsync) });
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
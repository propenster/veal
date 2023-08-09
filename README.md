# veal
A lightweight web app development framework in C#

### Let's get on a journey of writing our own lightweight lean web app framework for c#.

* I don't know what features we'll support yet but I'll think of something.

  # To Get Started

  ```csharp
  Create an HttpAppServer.
  var app = new HttpAppServer().Bind("http://localhost:4448/").Services(new List<string> { nameof(Hello), nameof(HelloAsync) });
  Call app.Run() to start listening for and treating requests.
  app.Run()


  ```

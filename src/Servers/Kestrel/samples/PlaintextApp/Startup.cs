// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

var host = new HostBuilder()
     .ConfigureWebHost(webHost =>
     {
         webHost.UseKestrel(o => o.ListenLocalhost(5000));

         webHost.ConfigureServices(services => services.AddRouting());
         webHost.Configure(app =>
         {
             // Synchronous middleware
             app.Use(next =>
             {
                 return context =>
                 {
                     context.Response.Headers["X-Random"] = Random.Shared.Next().ToString(CultureInfo.InvariantCulture);
                     next(context);
                 };
             });

             app.UseRouting();
             app.UseEndpoints(endpoints =>
             {
                 // Synchronous route match
                 endpoints.MapGet("/", context =>
                 {
                     // Synchronous JSON
                     context.Response.WriteAsJson("Hello World");
                 });
             });
         });
     })
     .Build();

host.Run();

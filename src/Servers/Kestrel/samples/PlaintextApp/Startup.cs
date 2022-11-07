// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var payload = "Hello, World!"u8.ToArray();

var host = new HostBuilder()
     .ConfigureWebHost(webHost =>
     {
         webHost.UseKestrel(o => o.ListenLocalhost(5000));

         webHost.ConfigureServices(services => services.AddRouting());
         webHost.Configure(app =>
         {
             app.UseRouting();
             app.UseEndpoints(endpoints =>
             {
                 endpoints.MapGet("/", context =>
                 {
                     var response = context.Response;

                     response.StatusCode = 200;
                     response.ContentType = "text/plain";
                     response.ContentLength = payload.Length;

                     return response.Body.WriteAsync(payload).AsTask();
                 });

                 endpoints.MapGet("/green", context =>
                 {
                     return Task.RunAsGreenThread(() =>
                     {
                         var response = context.Response;

                         response.StatusCode = 200;
                         response.ContentType = "text/plain";
                         response.ContentLength = payload.Length;

                         // This is async IO under the covers!
                         response.Body.Write(payload);
                     });
                 });
             });
         });
     })
     .Build();

host.Run();

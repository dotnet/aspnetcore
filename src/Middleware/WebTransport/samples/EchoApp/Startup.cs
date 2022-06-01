//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebTransport;
using System.Text;

namespace EchoApp;

public class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddConsole());
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseWebTransport();

        app.Use(async (context, next) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webTransportSession = await context.WebTransport.AcceptWebTransportSessionAsync();
                await Echo(context, webTransportSession, loggerFactory.CreateLogger("Echo"));
            }
            else
            {
                await next(context);
            }
        });

        app.UseFileServer();
    }

    private async Task Echo(HttpContext context, WebTransportSession session, ILogger logger)
    {
        // loop infinitely reading and then writing to the session's stream
        // print to log via: logger.LogDebug(stringMessage);
    }
}
/* TODO
 * Add sample for bi and uni directional streams
 * Add sample for datagrams (?)
 * Actually implement Echo
 */

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.WebTransport;
using Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;

namespace Http3SampleApp;

public class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var feature = context.Features.Get<IHttpWebTransportSessionFeature>();
            if (feature is not null)
            {
                var session = await feature.AcceptAsync(CancellationToken.None);

                // opens either a bidirectional or unidirectional stream
                test(await session.AcceptStreamAsync<WebTransportBaseStream>(CancellationToken.None));
                // waits specifically for a bidirectional stream. Discarding all undirectional ones until it gets a bidirectional one
                test(await session.AcceptStreamAsync<WebTransportBidirectionalStream>(CancellationToken.None));
                // waits specifically for a undirectional input stream
                test(await session.AcceptStreamAsync<WebTransportInputStream>(CancellationToken.None));
            }
            else
            {
                await next(context);
            }

            await Task.Delay(TimeSpan.FromMinutes(150));
        });
    }

    private void test(WebTransportBaseStream stream)
    {
        if (stream != null)
        {
            if (stream.GetType() == typeof(WebTransportBidirectionalStream))
            {
                Console.WriteLine("bidirectional"); 
            }
            else if (stream.GetType() == typeof(WebTransportInputStream))
            {
                Console.WriteLine("unidirectional");
            }
        }
    }
}

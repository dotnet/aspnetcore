// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Benchmarks;

public class StartupUsingRouter
{
    private static readonly byte[] _helloWorldPayload = Encoding.UTF8.GetBytes("Hello, World!");

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouter(routes =>
        {
            routes.MapRoute("/plaintext", (httpContext) =>
            {
                var response = httpContext.Response;
                var payloadLength = _helloWorldPayload.Length;
                response.StatusCode = 200;
                response.ContentType = "text/plain";
                response.ContentLength = payloadLength;
                return response.Body.WriteAsync(_helloWorldPayload, 0, payloadLength);
            });
        });
    }
}

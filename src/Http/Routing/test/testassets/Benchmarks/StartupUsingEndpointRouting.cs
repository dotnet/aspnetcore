// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Benchmarks;

public class StartupUsingEndpointRouting
{
    private static readonly byte[] _helloWorldPayload = Encoding.UTF8.GetBytes("Hello, World!");

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            var endpointDataSource = new DefaultEndpointDataSource(new[]
            {
                    new RouteEndpoint(
                        requestDelegate: (httpContext) =>
                        {
                            var response = httpContext.Response;
                            var payloadLength = _helloWorldPayload.Length;
                            response.StatusCode = 200;
                            response.ContentType = "text/plain";
                            response.ContentLength = payloadLength;
                            return response.Body.WriteAsync(_helloWorldPayload, 0, payloadLength);
                        },
                        routePattern: RoutePatternFactory.Parse("/plaintext"),
                        order: 0,
                        metadata: EndpointMetadataCollection.Empty,
                        displayName: "Plaintext"),
            });

            endpoints.DataSources.Add(endpointDataSource);
        });
    }
}

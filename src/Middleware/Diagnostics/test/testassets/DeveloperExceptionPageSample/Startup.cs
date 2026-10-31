// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Patterns;

namespace DeveloperExceptionPageSample;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddProblemDetails();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.Use((context, next) =>
        {
            context.Request.RouteValues = new RouteValueDictionary(new
            {
                routeValue1 = "Value1",
                routeValue2 = "Value2",
            });

            var endpoint = new RouteEndpoint(
                c => null,
                RoutePatternFactory.Parse("/"),
                0,
                new EndpointMetadataCollection(
                    new HttpMethodMetadata(new[] { "GET", "POST" }),
                    "this is a metadata \r\n with multuple line\r\n and <p>Html tags</p>"),
                "Endpoint display name");

            context.SetEndpoint(endpoint);
            return next(context);
        });
        app.UseDeveloperExceptionPage();
        app.Run(context =>
        {
            throw new Exception(string.Concat(
                "Demonstration exception. The list:", "\r\n",
                "New Line 1", "\n",
                "New Line 2", Environment.NewLine,
                "New Line 3"));
        });
    }

    public static Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>();
            })
            .Build();

        return host.RunAsync();
    }
}

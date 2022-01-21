// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HealthChecksSample;

// Pass in `--scenario writer` at the command line to run this sample.
public class CustomWriterStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Registers required services for health checks
        services.AddHealthChecks()

            // Registers a custom health check implementation
            .AddGCInfoCheck("GCInfo");
    }

    public void Configure(IApplicationBuilder app)
    {
        // This will register the health checks middleware at the URL /health
        // 
        // This example overrides the HealthCheckResponseWriter to write the health
        // check result in a totally custom way.
        app.UseHealthChecks("/health", new HealthCheckOptions()
        {
            // This custom writer formats the detailed status as JSON.
            ResponseWriter = WriteResponse,
        });

        app.Run(async (context) =>
        {
            await context.Response.WriteAsync("Go to /health to see the health status");
        });
    }

    private static Task WriteResponse(HttpContext httpContext, HealthReport result)
    {
        httpContext.Response.ContentType = "application/json";

        var json = new JObject(
            new JProperty("status", result.Status.ToString()),
            new JProperty("results", new JObject(result.Entries.Select(pair =>
                new JProperty(pair.Key, new JObject(
                    new JProperty("status", pair.Value.Status.ToString()),
                    new JProperty("description", pair.Value.Description),
                    new JProperty("data", new JObject(pair.Value.Data.Select(p => new JProperty(p.Key, p.Value))))))))));
        return httpContext.Response.WriteAsync(json.ToString(Formatting.Indented));
    }
}

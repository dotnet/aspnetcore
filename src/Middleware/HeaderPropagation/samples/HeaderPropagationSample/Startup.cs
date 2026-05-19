// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;

namespace HeaderPropagationSample;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();

        // A sample for configuration for propagating an A/B testing header. If we see
        // the X-BetaFeatures header then we forward it to outgoing calls. If we don't
        // see it, then we generate a new value randomly.
        //
        // To see this in action, send a request to /
        // - If you do not specify X-BetaFeatures the server will generate a new value
        // - If you specify X-BetaFeatures then you will see the value propagated
        //
        // This demonstrates two common uses of header propagation:
        // 1. Forward a header as-is
        // 2. Generate a new header value, or conditionally generate a header value
        //
        // It's also easy to forward a header with a different name, using Add(string, string)
        services.AddHeaderPropagation(options =>
        {
            // Propagate the X-BetaFeatures if present.
            options.Headers.Add("X-BetaFeatures");

            // Generate a new X-BetaFeatures if not present.
            options.Headers.Add("X-BetaFeatures", context =>
            {
                return GenerateBetaFeatureOptions();
            });
        });

        services
            .AddHttpClient("test")
            .AddHeaderPropagation();

        services
            .AddHttpClient("another")
            .AddHeaderPropagation(options => options.Headers.Add("X-BetaFeatures", "X-Experiments"));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHttpClientFactory clientFactory)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHeaderPropagation();

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", async context =>
            {
                foreach (var header in context.Request.Headers)
                {
                    await context.Response.WriteAsync($"'/' Got Header '{header.Key}': {string.Join(", ", (string[])header.Value)}\r\n");
                }

                var clientNames = new[] { "test", "another" };
                foreach (var clientName in clientNames)
                {
                    await context.Response.WriteAsync("Sending request to /forwarded\r\n");

                    var uri = UriHelper.BuildAbsolute(context.Request.Scheme, context.Request.Host, context.Request.PathBase, "/forwarded");
                    var client = clientFactory.CreateClient(clientName);
                    var response = await client.GetAsync(uri);

                    foreach (var header in response.RequestMessage.Headers)
                    {
                        await context.Response.WriteAsync($"Sent Header '{header.Key}': {string.Join(", ", header.Value)}\r\n");
                    }

                    await context.Response.WriteAsync("Got response\r\n");
                    await context.Response.WriteAsync(await response.Content.ReadAsStringAsync());
                }
            });

            endpoints.MapGet("/forwarded", async context =>
            {
                foreach (var header in context.Request.Headers)
                {
                    await context.Response.WriteAsync($"'/forwarded' Got Header '{header.Key}': {string.Join(", ", (string[])header.Value)}\r\n");
                }
            });
        });
    }

    private static StringValues GenerateBetaFeatureOptions()
    {
        var features = new string[]
        {
                "Widgets",
                "Social",
                "Speedy-Checkout",
        };

        var threshold = 0.80; // 20% chance for each feature in beta.

        var values = new List<string>();
        for (var i = 0; i < features.Length; i++)
        {
            if (Random.Shared.NextDouble() > threshold)
            {
                values.Add(features[i]);
            }
        }

        if (values.Count == 0)
        {
            return new StringValues("none");
        }

        return new StringValues(values.ToArray());
    }
}

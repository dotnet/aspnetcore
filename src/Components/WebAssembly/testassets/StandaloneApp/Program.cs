// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Hosting;

namespace StandaloneApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("app");

        // Conditionally enable OTel + service discovery (when OTEL_EXPORTER_OTLP_ENDPOINT is set by the gateway)
        if (!string.IsNullOrEmpty(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.AddBlazorClientServiceDefaults();
        }

        // Named HttpClient for service discovery (resolves via gateway when service defaults are active).
        // Note: AddEnvironmentVariables() normalizes __ to : in config keys, so we use : separator here.
        var weatherApiUrl = builder.Configuration["services:weatherapi:https:0"]
            ?? builder.Configuration["services:weatherapi:http:0"];
        if (weatherApiUrl is not null)
        {
            builder.Services.AddHttpClient("weatherapi", client =>
                client.BaseAddress = new Uri(weatherApiUrl));
        }

        builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        await builder.Build().RunAsync();
    }
}

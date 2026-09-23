// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class BlazorClientExtensions
{
    public static WebAssemblyHostBuilder AddBlazorClientServiceDefaults(this WebAssemblyHostBuilder builder)
    {
        builder.ConfigureBlazorClientOpenTelemetry();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddServiceDiscovery();
        });

        return builder;
    }

    private static WebAssemblyHostBuilder ConfigureBlazorClientOpenTelemetry(this WebAssemblyHostBuilder builder)
    {
        var otlpPathBase = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (string.IsNullOrEmpty(otlpPathBase))
        {
            return builder;
        }

        // Read the service name from configuration (set by Aspire hosting via the gateway).
        var serviceName = builder.Configuration["OTEL_SERVICE_NAME"]!;

        var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);
        var otlpEndpoint = new Uri(baseAddress, $"{otlpPathBase}/");

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName, serviceInstanceId: serviceName))
            .WithLogging(logging =>
            {
                logging.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint, "v1/logs"));
            }, options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter("Microsoft.AspNetCore.Components");
                metrics.AddMeter("Microsoft.AspNetCore.Components.Lifecycle");
                metrics.AddHttpClientInstrumentation();
                metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint, "v1/metrics"));
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource("Microsoft.AspNetCore.Components")
                    .AddHttpClientInstrumentation();
                tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint, "v1/traces"));
            });

        return builder;
    }
}

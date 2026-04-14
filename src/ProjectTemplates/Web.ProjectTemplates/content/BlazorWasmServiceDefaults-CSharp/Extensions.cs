// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorWasm.ServiceDefaults1;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Retry;

namespace Microsoft.Extensions.Hosting;

public static class BlazorClientExtensions
{
    public static WebAssemblyHostBuilder AddBlazorClientServiceDefaults(this WebAssemblyHostBuilder builder)
    {
        ComponentsMetricsServiceCollectionExtensions.AddComponentsMetrics(builder.Services);
        ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing(builder.Services);

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
        // Read the service name from configuration (set by Aspire hosting via the gateway).
        var serviceName = builder.Configuration["OTEL_SERVICE_NAME"] ?? "BlazorWasm.ServiceDefaults1";

        // Build a resilience pipeline for OTLP export retries.
        // The OTel SDK's built-in retry uses Thread.Sleep which would deadlock on WASM,
        // so we handle retries ourselves with async-safe exponential backoff.
        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new HttpRetryStrategyOptions
            {
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(5),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldRetryAfterHeader = true,
            })
            .Build();

        // Wire HttpClientFactory for all OTLP exporter instances via IPostConfigureOptions.
        // The fire-and-forget handler works around the OTel SDK's sync-over-async deadlock
        // on WASM: OtlpExportClient.SendHttpRequest() calls SendAsync().GetAwaiter().GetResult()
        // which blocks the single WASM thread. Our handler returns 200 immediately to unblock
        // the SDK, then fires the real request with retries in the background.
        builder.Services.AddSingleton<IPostConfigureOptions<OtlpExporterOptions>>(sp =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Aspire.OtlpExport");
            return new PostConfigureOptions<OtlpExporterOptions>(null, o =>
            {
                o.HttpClientFactory = () => new HttpClient(new BackgroundExportHandler(pipeline, logger));
            });
        });

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
            logging.AddOtlpExporter();
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithMetrics(metrics =>
            {
                metrics.AddMeter("Microsoft.AspNetCore.Components");
                metrics.AddMeter("Microsoft.AspNetCore.Components.Lifecycle");
                metrics.AddHttpClientInstrumentation();
                metrics.AddOtlpExporter();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource("Microsoft.AspNetCore.Components")
                    .AddHttpClientInstrumentation();
                tracing.AddOtlpExporter();
            });

        return builder;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorWasm.ServiceDefaults1;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;

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

        var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);
        var otlpEndpoint = new Uri(baseAddress, $"{otlpPathBase}/");

        // Wire HttpClientFactory for all OTLP exporter instances via IPostConfigureOptions.
        // The fire-and-forget handler works around the OTel SDK's sync-over-async deadlock
        // on WASM: OtlpExportClient.SendHttpRequest() calls SendAsync().GetAwaiter().GetResult()
        // which blocks the single WASM thread. Our handler returns 200 immediately to unblock
        // the SDK, then fires the real request with retries in the background.
        builder.Services.AddSingleton<IPostConfigureOptions<OtlpExporterOptions>>(sp =>
        {
            return new PostConfigureOptions<OtlpExporterOptions>(null, o =>
            {
                o.HttpClientFactory = () => new HttpClient(new BackgroundExportHandler(pipeline, sp));
            });
        });

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

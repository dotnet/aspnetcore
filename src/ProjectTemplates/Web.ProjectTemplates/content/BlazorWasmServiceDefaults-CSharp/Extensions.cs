using System.Diagnostics;
using BlazorWasm.ServiceDefaults1.Telemetry;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class BlazorClientExtensions
{

    public static WebAssemblyHostBuilder AddBlazorClientServiceDefaults(this WebAssemblyHostBuilder builder)
    {
        // Use OTEL_SERVICE_NAME from configuration (set by Aspire hosting via the gateway)
        // to identify this app in the dashboard. Falls back to a default if not set.
        var serviceName = builder.Configuration["OTEL_SERVICE_NAME"] ?? "BlazorWasm.ServiceDefaults1";

        builder.ConfigureBlazorClientOpenTelemetry(serviceName);

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        return builder;
    }

    private static WebAssemblyHostBuilder ConfigureBlazorClientOpenTelemetry(this WebAssemblyHostBuilder builder, string serviceName)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithMetrics(metrics =>
            {
                metrics.AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    // Add Blazor component metrics (this only works on 10.0 and onwards)
                    // See: https://learn.microsoft.com/aspnet/core/blazor/performance#metrics-and-tracing
                    .AddMeter("Microsoft.AspNetCore.Components")
                    .AddMeter("Microsoft.AspNetCore.Components.Lifecycle");
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(serviceName)
                    .AddHttpClientInstrumentation(options =>
                    {
                        // Filter out OTLP export requests to avoid a feedback loop.
                        // Without this, every telemetry export POST (v1/traces, v1/metrics,
                        // v1/logs) generates a new trace, which then gets exported, creating
                        // an ever-growing cycle that floods the dashboard.
                        options.FilterHttpRequestMessage = request =>
                            request.RequestUri is null
                            || (!request.RequestUri.AbsolutePath.Contains("/v1/traces")
                                && !request.RequestUri.AbsolutePath.Contains("/v1/metrics")
                                && !request.RequestUri.AbsolutePath.Contains("/v1/logs"));
                    })
                    // Add Blazor component tracing
                    .AddSource("Microsoft.AspNetCore.Components");
            });

        builder.AddBlazorClientOpenTelemetryExporters(serviceName);

        return builder;
    }

    private static WebAssemblyHostBuilder AddBlazorClientOpenTelemetryExporters(this WebAssemblyHostBuilder builder, string serviceName)
    {
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var otlpHeaders = builder.Configuration["OTEL_EXPORTER_OTLP_HEADERS"];

        // Parse OTLP headers (format: "key1=value1,key2=value2" or "key1=value1")
        var headers = ParseOtlpHeaders(otlpHeaders);

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            // The endpoint may be relative (/_otlp) or absolute (https://localhost:21187)
            // For relative URLs, construct the full URL using the app's base address
            Uri endpoint;
            if (otlpEndpoint.StartsWith("/"))
            {
                // Relative URL - combine with app's base address
                var baseUri = new Uri(builder.HostEnvironment.BaseAddress);
                endpoint = new Uri(baseUri, otlpEndpoint);
            }
            else
            {
                endpoint = new Uri(otlpEndpoint);
            }

            // Configure tracing with WebAssembly-compatible exporter.
            // We create HttpClient directly instead of using IHttpClientFactory to avoid
            // Lazy<T> reentrancy: TracerProvider (Lazy) → AddProcessor factory → IHttpClientFactory
            // → CreateClient → HTTP instrumentation handler → resolves TracerProvider → crash.
            var traceHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing.AddProcessor(sp =>
                    {
                        var exporter = new WebAssemblyOtlpTraceExporter(
                            new Uri(endpoint, "v1/traces"),
                            serviceName,
                            headers,
                            traceHttpClient);

                        return new TaskBasedBatchExportProcessor<Activity>(
                            exporter,
                            maxQueueSize: 2048,
                            scheduledDelayMilliseconds: 5000,
                            exporterTimeoutMilliseconds: 30000,
                            maxExportBatchSize: 512);
                    });
                });

            // Configure metrics with WebAssembly-compatible exporter.
            // Uses MeterListener (a .NET runtime feature) instead of MeterProvider,
            // which works in the single-threaded WebAssembly environment.
            var metricsHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            builder.Services.AddSingleton(sp =>
            {
                return new WebAssemblyOtlpMetricExporter(
                    new Uri(endpoint, "v1/metrics"),
                    serviceName,
                    headers,
                    httpClient: metricsHttpClient);
            });

            // Configure logging with WebAssembly-compatible exporter.
            var logsHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            builder.Services.AddOpenTelemetry()
                .WithLogging(logging =>
                {
                    logging.AddProcessor(sp =>
                    {
                        var exporter = new WebAssemblyOtlpLogExporter(
                            new Uri(endpoint, "v1/logs"),
                            serviceName,
                            headers,
                            logsHttpClient);

                        return new TaskBasedBatchExportProcessor<LogRecord>(
                            exporter,
                            maxQueueSize: 2048,
                            scheduledDelayMilliseconds: 5000,
                            exporterTimeoutMilliseconds: 30000,
                            maxExportBatchSize: 512);
                    });
                });
        }

        return builder;
    }

    private static Dictionary<string, string>? ParseOtlpHeaders(string? headersString)
    {
        if (string.IsNullOrWhiteSpace(headersString))
        {
            return null;
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // OTLP headers format: "key1=value1,key2=value2" or just "key1=value1"
        var pairs = headersString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var pair in pairs)
        {
            var separatorIndex = pair.IndexOf('=');
            if (separatorIndex > 0)
            {
                var key = pair.Substring(0, separatorIndex).Trim();
                var value = pair.Substring(separatorIndex + 1).Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    headers[key] = value;
                }
            }
        }

        return headers.Count > 0 ? headers : null;
    }
}

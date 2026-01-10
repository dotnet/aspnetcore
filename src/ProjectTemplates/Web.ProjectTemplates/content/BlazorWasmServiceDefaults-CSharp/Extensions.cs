using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for configuring service defaults for Blazor WebAssembly clients.
/// Adds OpenTelemetry, service discovery, and resilience support.
/// </summary>
public static class BlazorClientExtensions
{
    private const string DefaultServiceName = "blazor-webassembly-client";

    /// <summary>
    /// Adds common service defaults for Blazor WebAssembly clients including
    /// OpenTelemetry, service discovery, and resilience.
    /// </summary>
    /// <param name="builder">The <see cref="WebAssemblyHostBuilder"/> to configure.</param>
    /// <returns>The configured <see cref="WebAssemblyHostBuilder"/>.</returns>
    public static WebAssemblyHostBuilder AddBlazorClientServiceDefaults(this WebAssemblyHostBuilder builder)
    {
        builder.ConfigureBlazorClientOpenTelemetry();

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

    /// <summary>
    /// Configures OpenTelemetry for Blazor WebAssembly clients with logging, metrics, and tracing.
    /// </summary>
    /// <param name="builder">The <see cref="WebAssemblyHostBuilder"/> to configure.</param>
    /// <returns>The configured <see cref="WebAssemblyHostBuilder"/>.</returns>
    public static WebAssemblyHostBuilder ConfigureBlazorClientOpenTelemetry(this WebAssemblyHostBuilder builder)
    {
        var serviceName = builder.Configuration["OTEL_SERVICE_NAME"] ?? DefaultServiceName;

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithMetrics(metrics =>
            {
                // Uncomment the following lines to enable Blazor component metrics
                // See: https://learn.microsoft.com/aspnet/core/blazor/performance#metrics-and-tracing
                //metrics.AddMeter("Microsoft.AspNetCore.Components");
                //metrics.AddMeter("Microsoft.AspNetCore.Components.Lifecycle");

                metrics.AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                // Uncomment the following line to enable Blazor component tracing
                //tracing.AddSource("Microsoft.AspNetCore.Components");

                tracing.AddSource(serviceName)
                    .AddHttpClientInstrumentation();
            });

        builder.AddBlazorClientOpenTelemetryExporters();

        return builder;
    }

    private static WebAssemblyHostBuilder AddBlazorClientOpenTelemetryExporters(this WebAssemblyHostBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }
}

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
                metrics.AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
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
#if (!hosted)

/// <summary>
/// Extension methods for configuring the server-side host to pass configuration
/// to a standalone Blazor WebAssembly client via the configuration endpoint.
/// </summary>
public static class BlazorClientServerExtensions
{
    /// <summary>
    /// Adds the Blazor WebAssembly client configuration endpoint that serves
    /// environment variables and service URLs to the standalone client.
    /// The client's JS initializer fetches this endpoint during boot.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to configure.</param>
    /// <returns>The configured <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseBlazorClientConfiguration(this IApplicationBuilder app)
    {
        // TODO: Implement configuration endpoint for standalone scenario
        // This endpoint serves configuration (OTEL endpoints, service URLs, etc.)
        // to the standalone Blazor WebAssembly client via /_blazor/_configuration.
        // The client's JS initializer (onRuntimeConfigLoaded) fetches this endpoint.
        return app;
    }
}
#endif

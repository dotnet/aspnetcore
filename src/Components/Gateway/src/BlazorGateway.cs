// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.AspNetCore.Components.Gateway;

/// <summary>
/// Intended for framework test use only.
/// </summary>
public static class BlazorGateway
{
    /// <summary>
    /// Builds a <see cref="WebApplication"/> configured as a Blazor Gateway.
    /// Reads ClientApps config section for endpoint manifests and YARP reverse proxy configuration.
    /// </summary>
    public static WebApplication BuildWebHost(string[] args) =>
        BuildWebHost(WebApplication.CreateSlimBuilder(args));

    internal static WebApplication BuildWebHost(WebApplicationBuilder builder)
    {
        var options = new BlazorGatewayOptions();
        builder.Configuration.GetSection(BlazorGatewayOptions.SectionName).Bind(options);

        if (options.Telemetry.Enabled)
        {
            builder.ConfigureOpenTelemetry(options.Telemetry);
        }

        if (options.HealthChecks.Enabled)
        {
            builder.Services.AddHealthChecks()
                .AddCheck<LivenessHealthCheck>("self", tags: [options.HealthChecks.LivenessTag]);
        }

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            if (options.HttpClient.StandardResilience)
            {
                http.AddStandardResilienceHandler();
            }
            if (options.HttpClient.ServiceDiscovery)
            {
                http.AddServiceDiscovery();
            }
        });

        if (options.HttpClient.ServiceDiscovery)
        {
            builder.Services.AddServiceDiscovery();
        }

        builder.WebHost.UseStaticWebAssets();

        var appConfigs = builder.Configuration.GetSection("ClientApps")
            .Get<Dictionary<string, ClientAppConfiguration>>() ?? [];

        var proxySection = builder.Configuration.GetSection("ReverseProxy");
        var hasProxy = proxySection.Exists();

        if (hasProxy)
        {
            builder.Services.AddReverseProxy()
                .LoadFromConfig(proxySection)
                .AddServiceDiscoveryDestinationResolver();
        }

        var app = builder.Build();

        // HSTS tells browsers to always use HTTPS for this host, preventing future HTTP requests.
        // Only enable in non-development to avoid interfering with dev certificates and localhost.
        // See https://aka.ms/aspnetcore-hsts
        if (!app.Environment.IsDevelopment() && options.Hsts.Enabled)
        {
            app.UseHsts();
        }

        if (options.HttpsRedirection.Enabled)
        {
            // Only redirect top-level navigations (browser URL bar) from HTTP to HTTPS.
            // The Sec-Fetch-Dest header distinguishes navigations from subresource loads
            // and API fetches. This ensures the served document loads on HTTPS when
            // available, making subsequent fetch/XHR requests same-origin.
            // See https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Sec-Fetch-Dest
            app.UseWhen(
                context => string.Equals(
                    context.Request.Headers["Sec-Fetch-Dest"].ToString(),
                    "document",
                    StringComparison.OrdinalIgnoreCase),
                branch => branch.UseHttpsRedirection());
        }

        if (!string.IsNullOrEmpty(options.PathBase))
        {
            app.UsePathBase(options.PathBase);
        }

        if (app.Environment.IsDevelopment() && options.HealthChecks.Enabled)
        {
            app.MapHealthChecks(options.HealthChecks.Path);
            app.MapHealthChecks(options.HealthChecks.LivenessPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains(options.HealthChecks.LivenessTag)
            });
        }

        if (hasProxy)
        {
            app.MapReverseProxy();
        }

        foreach (var appConfig in appConfigs.Values)
        {
            if (!string.IsNullOrEmpty(appConfig.ConfigEndpointPath) && !string.IsNullOrEmpty(appConfig.ConfigResponse))
            {
                app.MapGet(appConfig.ConfigEndpointPath, () => Results.Content(appConfig.ConfigResponse, "application/json"))
                    .WithMetadata(new ContentEncodingMetadata("identity", 1.0));
            }

            if (!string.IsNullOrEmpty(appConfig.EndpointsManifest))
            {
                app.MapGroup(appConfig.PathPrefix ?? "").MapStaticAssets(appConfig.EndpointsManifest);
            }
        }

        return app;
    }

    private static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder, BlazorGatewayOptions.TelemetryOptions telemetry)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = context => TelemetryFilters.ShouldTraceInboundRequest(context.Request.Path, telemetry.ExcludePaths))
                    .AddHttpClientInstrumentation(options =>
                        // Filter out the gateway's own OTLP export calls to the dashboard
                        // to prevent a feedback loop (exporting traces creates new traces).
                        options.FilterHttpRequestMessage = request => TelemetryFilters.ShouldTraceOutboundRequest(request.RequestUri, telemetry.ExcludeOutboundPaths));
            });

        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }
}

sealed class ClientAppConfiguration
{
    public string? PathPrefix { get; set; }
    public string? EndpointsManifest { get; set; }
    public string? ConfigEndpointPath { get; set; }
    public string? ConfigResponse { get; set; }
}

// Liveness check that flips to Unhealthy as soon as the host begins shutting down,
// so orchestrators (Kubernetes, ACA) stop routing new requests during the
// terminationGracePeriodSeconds drain window while in-flight requests complete.
internal sealed class LivenessHealthCheck(IHostApplicationLifetime lifetime) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken) =>
        Task.FromResult(lifetime.ApplicationStopping.IsCancellationRequested
            ? HealthCheckResult.Unhealthy("Application is shutting down.")
            : HealthCheckResult.Healthy());
}

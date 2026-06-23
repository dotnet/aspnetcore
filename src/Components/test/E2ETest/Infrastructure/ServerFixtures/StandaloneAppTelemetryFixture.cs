// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Gateway;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;

/// <summary>
/// Fixture that hosts the Blazor gateway in-process with:
/// - A fake OTLP collector (records trace/log exports from the WASM app)
/// - A fake weather API (validates service discovery works)
/// - A custom OTLP proxy endpoint (/_otlp) that forwards telemetry to the collector
/// - ConfigEndpoint that injects OTEL env vars into the WASM runtime
/// </summary>
public class StandaloneAppTelemetryFixture : WebHostServerFixture
{
    private WebApplication _collectorApp;
    private WebApplication _weatherApp;

    public string CollectorUrl { get; private set; }
    public string WeatherApiUrl { get; private set; }

    /// <summary>Trace export request bodies received by the fake collector.</summary>
    public ConcurrentBag<byte[]> Traces { get; } = new();

    /// <summary>Log export request bodies received by the fake collector.</summary>
    public ConcurrentBag<byte[]> Logs { get; } = new();

    /// <summary>Metric export request bodies received by the fake collector.</summary>
    public ConcurrentBag<byte[]> Metrics { get; } = new();

    /// <summary>Number of requests the fake weather API received.</summary>
    public int WeatherApiRequestCount => _weatherApiRequestCount;
    private int _weatherApiRequestCount;

    protected override IHost CreateWebHost()
    {
        // Start the fake OTLP collector
        StartCollector();

        // Start the fake weather API
        StartWeatherApi();

        var contentRoot = FindSampleOrTestSitePath(
            typeof(StandaloneApp.Program).Assembly.FullName);

        var host = "127.0.0.1";
        if (E2ETestOptions.Instance.SauceTest)
        {
            host = E2ETestOptions.Instance.Sauce.HostName;
        }

        var assemblyLocation = typeof(StandaloneApp.Program).Assembly.Location;

        // WASM config response: environment variables injected into the browser.
        // OTEL_EXPORTER_OTLP_ENDPOINT is a relative path (_otlp) so the WASM app
        // sends telemetry to the gateway (same origin), which proxies it to the collector.
        var configResponse = JsonSerializer.Serialize(new
        {
            webAssembly = new
            {
                environment = new Dictionary<string, string>
                {
                    ["OTEL_SERVICE_NAME"] = "standalone-telemetry-test",
                    ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "_otlp",
                    // Service discovery: resolve "weatherapi" to the fake API
                    ["services__weatherapi__http__0"] = WeatherApiUrl,
                }
            }
        });

        var args = new List<string>
        {
            "--urls", $"http://{host}:0",
            "--contentroot", contentRoot,
            "--pathbase", "",
            "--staticWebAssets", Path.ChangeExtension(assemblyLocation, ".staticwebassets.runtime.json"),
            "--ClientApps:app:EndpointsManifest", Path.ChangeExtension(assemblyLocation, ".staticwebassets.endpoints.json"),
            "--ClientApps:app:PathPrefix", "",
            // Config endpoint for WASM env vars
            "--ClientApps:app:ConfigEndpointPath", "/_blazor/_configuration",
            "--ClientApps:app:ConfigResponse", configResponse,
            // Gateway's own telemetry export to our fake collector
            "--OTEL_EXPORTER_OTLP_ENDPOINT", CollectorUrl,
            "--OTEL_SERVICE_NAME", "standalone-gateway",
        };

        var app = BlazorGateway.BuildWebHost(args.ToArray());

        // OTLP proxy: the WASM app posts telemetry to /_otlp/... on the gateway (same origin).
        // We forward those requests to the fake collector. This avoids needing YARP's
        // RateLimiting dependency in the test host.
        var collectorBaseUrl = CollectorUrl;
        app.Map("/_otlp/{**path}", async (HttpContext ctx, string path) =>
        {
            using var client = new HttpClient();
            using var forwardRequest = new HttpRequestMessage(
                new HttpMethod(ctx.Request.Method),
                $"{collectorBaseUrl}/{path}");

            if (ctx.Request.ContentLength > 0 || ctx.Request.Headers.ContainsKey("Transfer-Encoding"))
            {
                using var ms = new MemoryStream();
                await ctx.Request.Body.CopyToAsync(ms);
                forwardRequest.Content = new ByteArrayContent(ms.ToArray());
                if (ctx.Request.ContentType is not null)
                {
                    forwardRequest.Content.Headers.ContentType =
                        System.Net.Http.Headers.MediaTypeHeaderValue.Parse(ctx.Request.ContentType);
                }
            }

            var response = await client.SendAsync(forwardRequest);
            ctx.Response.StatusCode = (int)response.StatusCode;
        });

        return app;
    }

    private void StartCollector()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:0");

        _collectorApp = builder.Build();

        _collectorApp.MapPost("/v1/traces/{**rest}", async (HttpContext ctx) =>
        {
            using var ms = new MemoryStream();
            await ctx.Request.Body.CopyToAsync(ms);
            Traces.Add(ms.ToArray());
            ctx.Response.StatusCode = 200;
        });

        _collectorApp.MapPost("/v1/logs/{**rest}", async (HttpContext ctx) =>
        {
            using var ms = new MemoryStream();
            await ctx.Request.Body.CopyToAsync(ms);
            Logs.Add(ms.ToArray());
            ctx.Response.StatusCode = 200;
        });

        _collectorApp.MapPost("/v1/metrics/{**rest}", async (HttpContext ctx) =>
        {
            using var ms = new MemoryStream();
            await ctx.Request.Body.CopyToAsync(ms);
            Metrics.Add(ms.ToArray());
            ctx.Response.StatusCode = 200;
        });

        RunInBackgroundThread(_collectorApp.Start);

        CollectorUrl = _collectorApp.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>().Addresses.First();
    }

    private void StartWeatherApi()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:0");
        builder.Services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

        _weatherApp = builder.Build();
        _weatherApp.UseCors();

        _weatherApp.MapGet("/weatherforecast", (HttpContext ctx) =>
        {
            Interlocked.Increment(ref _weatherApiRequestCount);
            var forecasts = new[]
            {
                new { Date = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), TemperatureC = 25, Summary = "Warm" },
                new { Date = DateTime.Now.AddDays(2).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), TemperatureC = 30, Summary = "Hot" },
                new { Date = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), TemperatureC = 15, Summary = "Cool" },
            };
            return Results.Json(forecasts);
        });

        RunInBackgroundThread(_weatherApp.Start);

        WeatherApiUrl = _weatherApp.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>().Addresses.First();
    }

    public override void Dispose()
    {
        base.Dispose();

        try { _collectorApp?.StopAsync().Wait(TimeSpan.FromSeconds(5)); } catch { }
        try { _weatherApp?.StopAsync().Wait(TimeSpan.FromSeconds(5)); } catch { }
        (_collectorApp as IDisposable)?.Dispose();
        (_weatherApp as IDisposable)?.Dispose();
    }

    /// <summary>
    /// Waits up to the specified duration for at least <paramref name="count"/> trace exports.
    /// </summary>
    public async Task<bool> WaitForTracesAsync(int count, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (Traces.Count >= count)
            {
                return true;
            }
            await Task.Delay(250);
        }
        return Traces.Count >= count;
    }

    /// <summary>
    /// Waits up to the specified duration for at least <paramref name="count"/> log exports.
    /// </summary>
    public async Task<bool> WaitForLogsAsync(int count, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (Logs.Count >= count)
            {
                return true;
            }
            await Task.Delay(250);
        }
        return Logs.Count >= count;
    }
}

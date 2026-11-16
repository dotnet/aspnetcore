// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.StaticAssets;

// Invokes the real endpoint produced by MapStaticAssets for a publish (non-build) manifest,
// so no development-time runtime handler is involved.
public class StaticAssetsInvokerBenchmark
{
    private const string ETag = "\"Fa0yOlTdLpA5u5+vzI2jL3sZcGbSqTqxWDWdRxRxNTI=\"";

    private string _root;
    private ServiceProvider _services;
    private RequestDelegate _requestDelegate;

    [GlobalSetup]
    public void Setup()
    {
        _root = Path.Combine(Path.GetTempPath(), "StaticAssetsInvokerBenchmark", Guid.NewGuid().ToString("N"));
        var webRoot = Path.Combine(_root, "wwwroot");
        Directory.CreateDirectory(webRoot);
        File.WriteAllText(Path.Combine(webRoot, "site.css"), new string('a', 1024));

        var manifestPath = Path.Combine(_root, "app.staticwebassets.endpoints.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(new
        {
            Version = 1,
            ManifestType = "Publish",
            Endpoints = new[]
            {
                new StaticAssetDescriptor
                {
                    Route = "site.css",
                    AssetPath = "site.css",
                    Selectors = [],
                    Properties = [],
                    ResponseHeaders =
                    [
                        new("Accept-Ranges", "bytes"),
                        new("Cache-Control", "max-age=31536000, immutable"),
                        new("Content-Length", "1024"),
                        new("Content-Type", "text/css"),
                        new("ETag", ETag),
                        new("Last-Modified", new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture)),
                    ],
                },
            },
        }));

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IWebHostEnvironment>(new BenchmarkWebHostEnvironment(_root, webRoot));
        services.AddRouting();
        _services = services.BuildServiceProvider();

        var endpoints = new BenchmarkEndpointRouteBuilder(_services);
        endpoints.MapStaticAssets(manifestPath);

        var endpoint = endpoints.DataSources.SelectMany(d => d.Endpoints).OfType<RouteEndpoint>().Single();
        _requestDelegate = endpoint.RequestDelegate;

        if (Head200().GetAwaiter().GetResult() != StatusCodes.Status200OK ||
            GetIfNoneMatch304().GetAwaiter().GetResult() != StatusCodes.Status304NotModified)
        {
            throw new InvalidOperationException("The benchmark requests did not produce the expected status codes.");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _services.Dispose();
        Directory.Delete(_root, recursive: true);
    }

    [Benchmark]
    public async Task<int> Head200()
    {
        var context = CreateContext(HttpMethods.Head);
        await _requestDelegate(context);
        return context.Response.StatusCode;
    }

    [Benchmark]
    public async Task<int> GetIfNoneMatch304()
    {
        var context = CreateContext(HttpMethods.Get);
        context.Request.Headers.IfNoneMatch = ETag;
        await _requestDelegate(context);
        return context.Response.StatusCode;
    }

    private DefaultHttpContext CreateContext(string method)
    {
        var context = new DefaultHttpContext { RequestServices = _services };
        context.Request.Method = method;
        context.Request.Path = "/site.css";
        context.Response.Body = Stream.Null;
        return context;
    }

    private sealed class BenchmarkEndpointRouteBuilder(IServiceProvider serviceProvider) : IEndpointRouteBuilder
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;

        public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();

        public IApplicationBuilder CreateApplicationBuilder() => new ApplicationBuilder(ServiceProvider);
    }

    private sealed class BenchmarkWebHostEnvironment(string contentRoot, string webRoot) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "app";
        public string EnvironmentName { get; set; } = "Production";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(contentRoot);
        public string WebRootPath { get; set; } = webRoot;
        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider(webRoot);
    }
}

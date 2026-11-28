// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.HttpLogging.Microbenchmarks;

[MemoryDiagnoser]
public class W3CLoggingMiddlewareBenchmark
{
    private string _logDirectory = null!;
    private ServiceProvider _defaultServices = null!;
    private ServiceProvider _additionalHeadersServices = null!;
    private RequestDelegate _defaultFields = null!;
    private RequestDelegate _additionalHeaders = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _logDirectory = Path.Combine(Path.GetTempPath(), "W3CLoggingMiddlewareBenchmark", Guid.NewGuid().ToString("N"));
        (_defaultServices, _defaultFields) = CreatePipeline(_ => { });
        (_additionalHeadersServices, _additionalHeaders) = CreatePipeline(options =>
        {
            options.AdditionalRequestHeaders.Add("x-forwarded-for");
            options.AdditionalRequestHeaders.Add("x-request-id");
            options.AdditionalRequestHeaders.Add("accept-language");
        });
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        // W3CLogger is only IAsyncDisposable, so the providers must be disposed asynchronously.
        _defaultServices.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _additionalHeadersServices.DisposeAsync().AsTask().GetAwaiter().GetResult();
        if (Directory.Exists(_logDirectory))
        {
            Directory.Delete(_logDirectory, recursive: true);
        }
    }

    [Benchmark(Baseline = true)]
    public HttpContext ContextOnly() => CreateHttpContext();

    [Benchmark]
    public Task DefaultFields() => _defaultFields(CreateHttpContext());

    [Benchmark]
    public Task DefaultFieldsWithAdditionalRequestHeaders() => _additionalHeaders(CreateHttpContext());

    private (ServiceProvider, RequestDelegate) CreatePipeline(Action<W3CLoggerOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new BenchmarkHostEnvironment(_logDirectory));
        services.AddW3CLogging(options =>
        {
            options.LogDirectory = _logDirectory;
            configure(options);
        });

        var serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);
        app.UseW3CLogging();
        app.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });
        var pipeline = app.Build();

        // Complete the logger's background queue so every request still formats the full log line
        // in W3CLogger.Log but nothing is written to disk.
        var w3cLoggerType = services.Single(d => d.ServiceType.Name == "W3CLogger").ServiceType;
        ((IAsyncDisposable)serviceProvider.GetRequiredService(w3cLoggerType)).DisposeAsync().AsTask().GetAwaiter().GetResult();

        return (serviceProvider, pipeline);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.12");
        context.Connection.LocalIpAddress = IPAddress.Parse("10.0.0.1");
        context.Connection.LocalPort = 5000;

        var request = context.Request;
        request.Protocol = "HTTP/1.1";
        request.Method = HttpMethods.Get;
        request.Scheme = "https";
        request.Path = "/api/products/12345";
        request.QueryString = new QueryString("?page=2&sort=price");
        request.Headers.Host = "www.example.com";
        request.Headers.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        request.Headers.Referer = "https://www.example.com/products?category=books";
        request.Headers["x-forwarded-for"] = "203.0.113.7";
        request.Headers["x-request-id"] = "0HN1QJ5K8V2A3:00000001";
        request.Headers.AcceptLanguage = "en-US,en;q=0.9";
        return context;
    }

    private sealed class BenchmarkHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "W3CLoggingMiddlewareBenchmark";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

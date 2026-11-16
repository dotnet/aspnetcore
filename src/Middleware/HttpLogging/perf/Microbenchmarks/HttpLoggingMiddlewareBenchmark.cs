// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpLogging.Microbenchmarks;

[MemoryDiagnoser]
public class HttpLoggingMiddlewareBenchmark
{
    private ServiceProvider _defaultFieldsServices = null!;
    private ServiceProvider _combineLogsServices = null!;
    private RequestDelegate _defaultFields = null!;
    private RequestDelegate _combineLogs = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        (_defaultFieldsServices, _defaultFields) = CreatePipeline(_ => { });
        (_combineLogsServices, _combineLogs) = CreatePipeline(options => options.CombineLogs = true);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _defaultFieldsServices.Dispose();
        _combineLogsServices.Dispose();
    }

    [Benchmark(Baseline = true)]
    public HttpContext ContextOnly() => CreateHttpContext();

    [Benchmark]
    public Task DefaultFields() => _defaultFields(CreateHttpContext());

    [Benchmark]
    public Task CombineLogs() => _combineLogs(CreateHttpContext());

    private static (ServiceProvider, RequestDelegate) CreatePipeline(Action<HttpLoggingOptions> configure)
    {
        var services = new ServiceCollection();
        // Formats every log message like a console sink would, without doing any I/O.
        services.AddLogging(builder => builder.ClearProviders().AddProvider(new FormattingLoggerProvider()));
        services.AddHttpLogging(configure);

        var serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);
        app.UseHttpLogging();
        app.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers["x-request-id"] = "0HN1QJ5K8V2A3:00000001";
            return Task.CompletedTask;
        });
        return (serviceProvider, app.Build());
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Protocol = "HTTP/1.1";
        request.Method = HttpMethods.Get;
        request.Scheme = "https";
        request.Path = "/api/products/12345";
        request.QueryString = new QueryString("?page=2&sort=price");
        request.Headers.Host = "www.example.com";
        request.Headers.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        request.Headers.Accept = "application/json";
        request.Headers.AcceptEncoding = "gzip, deflate, br";
        request.Headers.AcceptLanguage = "en-US,en;q=0.9";
        request.Headers.Connection = "keep-alive";
        request.Headers.Referer = "https://www.example.com/products";
        request.Headers.CacheControl = "no-cache";
        request.Headers["x-request-id"] = "0HN1QJ5K8V2A3:00000001";
        return context;
    }

    private sealed class FormattingLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new FormattingLogger();

        public void Dispose()
        {
        }
    }

    private sealed class FormattingLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => GC.KeepAlive(formatter(state, exception));
    }
}

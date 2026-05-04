// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class CacheBoundaryRenderTest
{
    [Fact]
    public async Task MissingDependencies_FallsBackToChildContent()
    {
        var component = new CacheBoundary
        {
            ChildContent = builder => builder.AddContent(0, "hello"),
        };

        var frames = await RenderComponent(component);

        AssertContainsText(frames, "hello");
    }

    [Fact]
    public async Task DeserializationFailure_FallsBackToChildContent_AndLogsWarning()
    {
        var testLogger = new TestLogger();
        var httpContext = CreateHttpContext();
        httpContext.RequestServices = new TestServiceProviderWithLogger(testLogger);

        var store = new TestCacheStore { ReturnForAnyKey = "NOT VALID JSON {{{" };

        var component = new CacheBoundary
        {
            ChildContent = builder => builder.AddContent(0, "fallback"),
            CacheStore = store,
            HttpContext = httpContext,
        };

        var frames = await RenderComponent(component);

        AssertContainsText(frames, "fallback");
        var entry = Assert.Single(testLogger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("Failed to restore CacheBoundary", entry.Message);
        Assert.NotNull(entry.Exception);
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        context.Request.Path = "/test";
        context.RequestServices = new TestServiceProviderWithLogger(new TestLogger());

        return context;
    }

    private static async Task<ArrayRange<RenderTreeFrame>> RenderComponent(CacheBoundary component)
    {
        var renderer = new TestRenderer();
        var id = renderer.AssignRootComponentId(component);
        await renderer.RenderRootComponentAsync(id);

        return renderer.GetCurrentRenderTreeFrames(id);
    }

    private static void AssertContainsText(ArrayRange<RenderTreeFrame> frames, string expectedText)
    {
        for (var i = 0; i < frames.Count; i++)
        {
            ref var frame = ref frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Text && frame.TextContent == expectedText)
            {
                return;
            }
        }

        Assert.Fail($"Expected to find text frame '{expectedText}' but it was not present.");
    }

    private sealed class TestCacheStore : ICacheBoundaryStore
    {
        public Dictionary<string, string> Data { get; } = new();
        public string? ReturnForAnyKey { get; set; }

        public string? Get(string key)
            => ReturnForAnyKey ?? (Data.TryGetValue(key, out var value) ? value : null);

        public void Set(string key, string json, CacheStoreOptions options = default)
            => Data[key] = json;

        public void Dispose() { }
    }

    private sealed class TestLogger : ILogger
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }

        public record LogEntry(LogLevel Level, string Message, Exception? Exception);
    }

    private sealed class TestServiceProviderWithLogger : IServiceProvider
    {
        private readonly TestLogger _logger;

        public TestServiceProviderWithLogger(TestLogger logger)
        {
            _logger = logger;
        }

        public object? GetService(Type serviceType)
            => serviceType == typeof(ILoggerFactory) ? new TestLoggerFactory(_logger) : null;
    }

    private sealed class TestLoggerFactory : ILoggerFactory
    {
        private readonly TestLogger _logger;

        public TestLoggerFactory(TestLogger logger) => _logger = logger;

        public ILogger CreateLogger(string categoryName) => _logger;

        public void AddProvider(ILoggerProvider provider) { }

        public void Dispose() { }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class CacheBoundaryRenderTest
{
    [Fact]
    public async Task DeserializationFailure_FallsBackToChildContent_AndLogsWarning()
    {
        var testLogger = new TestLogger();
        var httpContext = CreateHttpContext();

        var store = new TestCacheStore { ReturnForAnyKey = "NOT VALID JSON {{{" };
        var service = new CacheBoundaryService(store, new TestLoggerFactory(testLogger));

        var component = new CacheBoundary
        {
            ChildContent = builder => builder.AddContent(0, "fallback"),
            CacheService = service,
            HttpContext = httpContext,
        };

        var frames = await RenderComponent(component);

        AssertContainsText(frames, "fallback");
        var entry = Assert.Single(testLogger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("Failed to restore CacheBoundary", entry.Message);
        Assert.NotNull(entry.Exception);
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

    [Fact]
    public async Task CacheHit_DoesNotInvokeChildContent()
    {
        var httpContext = CreateHttpContext();

        var precomputed = new SerializedRenderFragment
        {
            Nodes = [new RenderTreeNode { Type = "markup", Content = "<p>from-cache</p>" }],
        };
        var store = new TestCacheStore { ReturnForAnyKey = JsonSerializer.Serialize(precomputed, ServerComponentSerializationSettings.JsonSerializationOptions) };
        var service = new CacheBoundaryService(store, new TestLoggerFactory(new TestLogger()));

        var childContentInvocations = 0;
        var component = new CacheBoundary
        {
            ChildContent = builder =>
            {
                childContentInvocations++;
                builder.AddMarkupContent(0, "<p>from-fresh</p>");
            },
            CacheService = service,
            HttpContext = httpContext,
        };

        var frames = await RenderComponent(component);

        Assert.Equal(0, childContentInvocations);
        AssertContainsMarkup(frames, "<p>from-cache</p>");
    }

    private static void AssertContainsMarkup(ArrayRange<RenderTreeFrame> frames, string expectedMarkup)
    {
        for (var i = 0; i < frames.Count; i++)
        {
            ref var frame = ref frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Markup && frame.MarkupContent == expectedMarkup)
            {
                return;
            }
        }

        Assert.Fail($"Expected to find markup frame '{expectedMarkup}' but it was not present.");
    }

    private sealed class TestCacheStore : ICacheBoundaryStore
    {
        public Dictionary<string, string> Data { get; } = new();
        public string ReturnForAnyKey { get; set; }

        public async ValueTask<string> GetOrCreateAsync(
            string key,
            Func<CancellationToken, ValueTask<string>> factory,
            CacheStoreOptions options,
            CancellationToken cancellationToken)
        {
            if (ReturnForAnyKey is not null)
            {
                return ReturnForAnyKey;
            }

            if (Data.TryGetValue(key, out var value))
            {
                return value;
            }

            var created = await factory(cancellationToken).ConfigureAwait(false);
            Data[key] = created;
            return created;
        }

        public void Dispose() { }
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
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

    private sealed class TestLogger : ILogger
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }

        public record LogEntry(LogLevel Level, string Message, Exception Exception);
    }

    private sealed class TestServiceProviderWithLogger : IServiceProvider
    {
        private readonly TestLogger _logger;

        public TestServiceProviderWithLogger(TestLogger logger)
        {
            _logger = logger;
        }

        public object GetService(Type serviceType)
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

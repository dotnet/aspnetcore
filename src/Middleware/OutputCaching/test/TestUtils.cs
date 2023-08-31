// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

internal class TestUtils
{
    static TestUtils()
    {
        // Force sharding in tests
        StreamUtilities.BodySegmentSize = 10;
    }

    private static bool TestRequestDelegate(HttpContext context, string guid)
    {
        var headers = context.Response.GetTypedHeaders();
        headers.Date = DateTimeOffset.UtcNow;
        headers.Headers["X-Value"] = guid;

        if (context.Request.Method != "HEAD")
        {
            return true;
        }
        return false;
    }

    internal static async Task TestRequestDelegateWriteAsync(HttpContext context)
    {
        var uniqueId = Guid.NewGuid().ToString();
        if (TestRequestDelegate(context, uniqueId))
        {
            await context.Response.WriteAsync(uniqueId);
        }
    }

    internal static async Task TestRequestDelegateSendFileAsync(HttpContext context)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestDocument.txt");
        var uniqueId = Guid.NewGuid().ToString();
        if (TestRequestDelegate(context, uniqueId))
        {
            await context.Response.SendFileAsync(path, 0, null);
            await context.Response.WriteAsync(uniqueId);
        }
    }

    internal static Task TestRequestDelegateWrite(HttpContext context)
    {
        var uniqueId = Guid.NewGuid().ToString();
        if (TestRequestDelegate(context, uniqueId))
        {
            var feature = context.Features.Get<IHttpBodyControlFeature>();
            if (feature != null)
            {
                feature.AllowSynchronousIO = true;
            }
            context.Response.Write(uniqueId);
        }
        return Task.CompletedTask;
    }

    internal static async Task TestRequestDelegatePipeWriteAsync(HttpContext context)
    {
        var uniqueId = Guid.NewGuid().ToString();
        if (TestRequestDelegate(context, uniqueId))
        {
            Encoding.UTF8.GetBytes(uniqueId, context.Response.BodyWriter);
            await context.Response.BodyWriter.FlushAsync();
        }
    }

    internal static IOutputCacheKeyProvider CreateTestKeyProvider()
    {
        return CreateTestKeyProvider(new OutputCacheOptions());
    }

    internal static IOutputCacheKeyProvider CreateTestKeyProvider(OutputCacheOptions options)
    {
        return new OutputCacheKeyProvider(new DefaultObjectPoolProvider(), Options.Create(options));
    }

    internal static IEnumerable<IHostBuilder> CreateBuildersWithOutputCaching(
        Action<IApplicationBuilder>? configureDelegate = null,
        OutputCacheOptions? options = null,
        Action<HttpContext>? contextAction = null)
    {
        return CreateBuildersWithOutputCaching(configureDelegate, options, new RequestDelegate[]
        {
            context =>
            {
                contextAction?.Invoke(context);
                return TestRequestDelegateWrite(context);
            },
            context =>
            {
                contextAction?.Invoke(context);
                return TestRequestDelegateWriteAsync(context);
            },
            context =>
            {
                contextAction?.Invoke(context);
                return TestRequestDelegateSendFileAsync(context);
            },
            context =>
            {
                contextAction?.Invoke(context);
                return TestRequestDelegatePipeWriteAsync(context);
            },
        });
    }

    private static IEnumerable<IHostBuilder> CreateBuildersWithOutputCaching(
        Action<IApplicationBuilder>? configureDelegate = null,
        OutputCacheOptions? options = null,
        IEnumerable<RequestDelegate>? requestDelegates = null)
    {
        if (configureDelegate == null)
        {
            configureDelegate = app => { };
        }
        if (requestDelegates == null)
        {
            requestDelegates = new RequestDelegate[]
            {
                TestRequestDelegateWriteAsync,
                TestRequestDelegateWrite,
                TestRequestDelegatePipeWriteAsync,
            };
        }

        foreach (var requestDelegate in requestDelegates)
        {
            // Test with in memory OutputCache
            yield return new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddOutputCache(outputCachingOptions =>
                        {
                            if (options != null)
                            {
                                outputCachingOptions.MaximumBodySize = options.MaximumBodySize;
                                outputCachingOptions.UseCaseSensitivePaths = options.UseCaseSensitivePaths;
                                outputCachingOptions.TimeProvider = options.TimeProvider;
                                outputCachingOptions.BasePolicies = options.BasePolicies;
                                outputCachingOptions.DefaultExpirationTimeSpan = options.DefaultExpirationTimeSpan;
                                outputCachingOptions.SizeLimit = options.SizeLimit;
                            }
                            else
                            {
                                outputCachingOptions.BasePolicies = new();
                                outputCachingOptions.BasePolicies.Add(new OutputCachePolicyBuilder().Build());
                            }
                        });
                    })
                    .Configure(app =>
                    {
                        configureDelegate(app);
                        app.UseOutputCache();
                        app.Run(requestDelegate);
                    });
                });
        }
    }

    internal static OutputCacheMiddleware CreateTestMiddleware(
        RequestDelegate? next = null,
        IOutputCacheStore? cache = null,
        OutputCacheOptions? options = null,
        TestSink? testSink = null,
        IOutputCacheKeyProvider? keyProvider = null
        )
    {
        if (next == null)
        {
            next = httpContext => Task.CompletedTask;
        }
        if (cache == null)
        {
            cache = new SimpleTestOutputCache();
        }
        if (options == null)
        {
            options = new OutputCacheOptions();
        }
        if (keyProvider == null)
        {
            keyProvider = new OutputCacheKeyProvider(new DefaultObjectPoolProvider(), Options.Create(options));
        }

        return new OutputCacheMiddleware(
            next,
            Options.Create(options),
            testSink == null ? NullLoggerFactory.Instance : new TestLoggerFactory(testSink, true),
            cache,
            keyProvider);
    }

    internal static OutputCacheContext CreateTestContext(HttpContext? httpContext = null, IOutputCacheStore? cache = null, OutputCacheOptions? options = null, ITestSink? testSink = null)
    {
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IOutputCacheStore))).Returns(cache ?? new SimpleTestOutputCache());
        serviceProvider.Setup(x => x.GetService(typeof(IOptions<OutputCacheOptions>))).Returns(Options.Create(options ?? new OutputCacheOptions()));
        serviceProvider.Setup(x => x.GetService(typeof(ILogger<OutputCacheMiddleware>))).Returns(testSink == null ? NullLogger.Instance : new TestLogger("OutputCachingTests", testSink, true));

        httpContext ??= new DefaultHttpContext();
        httpContext.RequestServices = serviceProvider.Object;

        return new OutputCacheContext()
        {
            HttpContext = httpContext,
            EnableOutputCaching = true,
            AllowCacheStorage = true,
            AllowCacheLookup = true,
            ResponseTime = DateTimeOffset.UtcNow
        };
    }

    internal static OutputCacheContext CreateUninitializedContext(HttpContext? httpContext = null, IOutputCacheStore? cache = null, OutputCacheOptions? options = null, ITestSink? testSink = null)
    {
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IOutputCacheStore))).Returns(cache ?? new SimpleTestOutputCache());
        serviceProvider.Setup(x => x.GetService(typeof(IOptions<OutputCacheOptions>))).Returns(Options.Create(options ?? new OutputCacheOptions()));
        serviceProvider.Setup(x => x.GetService(typeof(ILogger<OutputCacheMiddleware>))).Returns(testSink == null ? NullLogger.Instance : new TestLogger("OutputCachingTests", testSink, true));

        httpContext ??= new DefaultHttpContext();
        httpContext.RequestServices = serviceProvider.Object;

        return new OutputCacheContext()
        {
            HttpContext = httpContext,
        };
    }
    internal static void AssertLoggedMessages(IEnumerable<WriteContext> messages, params LoggedMessage[] expectedMessages)
    {
        var messageList = messages.ToList();
        Assert.Equal(expectedMessages.Length, messageList.Count);

        for (var i = 0; i < messageList.Count; i++)
        {
            Assert.Equal(expectedMessages[i].EventId, messageList[i].EventId);
            Assert.Equal(expectedMessages[i].LogLevel, messageList[i].LogLevel);
        }
    }

    public static HttpRequestMessage CreateRequest(string method, string requestUri)
    {
        return new HttpRequestMessage(new HttpMethod(method), requestUri);
    }
}

internal static class HttpResponseWritingExtensions
{
    internal static void Write(this HttpResponse response, string text)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(text);

        var data = Encoding.UTF8.GetBytes(text);
        response.Body.Write(data, 0, data.Length);
    }
}

internal class LoggedMessage
{
    internal static LoggedMessage NotModifiedIfNoneMatchStar => new LoggedMessage(1, LogLevel.Debug);
    internal static LoggedMessage NotModifiedIfNoneMatchMatched => new LoggedMessage(2, LogLevel.Debug);
    internal static LoggedMessage NotModifiedIfModifiedSinceSatisfied => new LoggedMessage(3, LogLevel.Debug);
    internal static LoggedMessage NotModifiedServed => new LoggedMessage(4, LogLevel.Information);
    internal static LoggedMessage CachedResponseServed => new LoggedMessage(5, LogLevel.Information);
    internal static LoggedMessage GatewayTimeoutServed => new LoggedMessage(6, LogLevel.Information);
    internal static LoggedMessage NoResponseServed => new LoggedMessage(7, LogLevel.Information);
    internal static LoggedMessage ResponseCached => new LoggedMessage(8, LogLevel.Information);
    internal static LoggedMessage ResponseNotCached => new LoggedMessage(9, LogLevel.Information);
    internal static LoggedMessage ResponseContentLengthMismatchNotCached => new LoggedMessage(10, LogLevel.Warning);
    internal static LoggedMessage ExpirationExpiresExceeded => new LoggedMessage(11, LogLevel.Debug);

    private LoggedMessage(int evenId, LogLevel logLevel)
    {
        EventId = evenId;
        LogLevel = logLevel;
    }

    internal int EventId { get; }
    internal LogLevel LogLevel { get; }
}

internal class TestResponseCachingKeyProvider : IOutputCacheKeyProvider
{
    private readonly string _key;

    public TestResponseCachingKeyProvider(string key)
    {
        _key = key;
    }

    public string CreateStorageKey(OutputCacheContext? context)
    {
        return _key;
    }
}

internal class SimpleTestOutputCache : ITestOutputCacheStore
{
    private readonly Dictionary<string, byte[]?> _storage = new();
    public int GetCount { get; private set; }
    public int SetCount { get; private set; }
    private readonly object synLock = new();

    public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<byte[]?> GetAsync(string? key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        lock (synLock)
        {
            GetCount++;
            try
            {
                return ValueTask.FromResult(_storage[key]);
            }
            catch
            {
                return ValueTask.FromResult(default(byte[]));
            }
        }
    }

    public ValueTask SetAsync(string key, byte[] entry, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
    {
        lock (synLock)
        {
            SetCount++;
            _storage[key] = entry;

            return ValueTask.CompletedTask;
        }
    }
}

internal class BufferTestOutputCache : SimpleTestOutputCache, IOutputCacheBufferStore
{
    ValueTask IOutputCacheBufferStore.SetAsync(string key, ReadOnlySequence<byte> value, ReadOnlyMemory<string> tags, TimeSpan validFor, CancellationToken cancellationToken)
        => SetAsync(key, value.ToArray(), tags.ToArray(), validFor, cancellationToken);

    async ValueTask<bool> IOutputCacheBufferStore.TryGetAsync(string key, PipeWriter destination, CancellationToken cancellationToken)
    {
        var data = await GetAsync(key, cancellationToken); // in reality we expect this to be sync, but: meh
        if (data is null)
        {
            return false;
        }
        await destination.WriteAsync(data, cancellationToken);
        return true;
    }
}

internal class AllowTestPolicy : IOutputCachePolicy
{
    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        context.AllowCacheLookup = true;
        context.AllowCacheStorage = true;
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}

public interface ITestOutputCacheStore : IOutputCacheStore
{
    int GetCount { get; }
    int SetCount { get; }
}

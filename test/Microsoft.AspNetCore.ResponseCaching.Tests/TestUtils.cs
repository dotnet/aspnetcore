// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    internal class TestUtils
    {
        static TestUtils()
        {
            // Force sharding in tests
            StreamUtilities.BodySegmentSize = 10;
        }

        internal static RequestDelegate TestRequestDelegate = async (context) =>
        {
            var uniqueId = Guid.NewGuid().ToString();
            var headers = context.Response.GetTypedHeaders();
            headers.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(10)
            };
            headers.Date = DateTimeOffset.UtcNow;
            headers.Headers["X-Value"] = uniqueId;
            await context.Response.WriteAsync(uniqueId);
        };

        internal static IResponseCacheKeyProvider CreateTestKeyProvider()
        {
            return CreateTestKeyProvider(new ResponseCacheOptions());
        }

        internal static IResponseCacheKeyProvider CreateTestKeyProvider(ResponseCacheOptions options)
        {
            return new ResponseCacheKeyProvider(new DefaultObjectPoolProvider(), Options.Create(options));
        }

        internal static IEnumerable<IWebHostBuilder> CreateBuildersWithResponseCache(
            Action<IApplicationBuilder> configureDelegate = null,
            ResponseCacheOptions options = null,
            RequestDelegate requestDelegate = null)
        {
            if (configureDelegate == null)
            {
                configureDelegate = app => { };
            }
            if (options == null)
            {
                options = new ResponseCacheOptions();
            }
            if (requestDelegate == null)
            {
                requestDelegate = TestRequestDelegate;
            }

            // Test with MemoryResponseCacheStore
            yield return new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddMemoryResponseCacheStore();
                })
                .Configure(app =>
                {
                    configureDelegate(app);
                    app.UseResponseCache(options);
                    app.Run(requestDelegate);
                });
        }

        internal static ResponseCacheMiddleware CreateTestMiddleware(
            IResponseCacheStore store = null,
            ResponseCacheOptions options = null,
            TestSink testSink = null,
            IResponseCacheKeyProvider keyProvider = null,
            IResponseCachePolicyProvider policyProvider = null)
        {
            if (store == null)
            {
                store = new TestResponseCacheStore();
            }
            if (options == null)
            {
                options = new ResponseCacheOptions();
            }
            if (keyProvider == null)
            {
                keyProvider = new ResponseCacheKeyProvider(new DefaultObjectPoolProvider(), Options.Create(options));
            }
            if (policyProvider == null)
            {
                policyProvider = new TestResponseCachePolicyProvider();
            }

            return new ResponseCacheMiddleware(
                httpContext => TaskCache.CompletedTask,
                Options.Create(options),
                testSink == null ? (ILoggerFactory)NullLoggerFactory.Instance : new TestLoggerFactory(testSink, true),
                policyProvider,
                store,
                keyProvider);
        }

        internal static ResponseCacheContext CreateTestContext()
        {
            return new ResponseCacheContext(new DefaultHttpContext(), NullLogger.Instance)
            {
                ResponseTime = DateTimeOffset.UtcNow
            };
        }

        internal static ResponseCacheContext CreateTestContext(TestSink testSink)
        {
            return new ResponseCacheContext(new DefaultHttpContext(), new TestLogger("ResponseCachingTests", testSink, true))
            {
                ResponseTime = DateTimeOffset.UtcNow
            };
        }

        internal static void AssertLoggedMessages(List<WriteContext> messages, params LoggedMessage[] expectedMessages)
        {
            Assert.Equal(messages.Count, expectedMessages.Length);
            for (var i = 0; i < messages.Count; i++)
            {
                Assert.Equal(expectedMessages[i].EventId, messages[i].EventId);
                Assert.Equal(expectedMessages[i].LogLevel, messages[i].LogLevel);
            }
        }
    }

    internal static class HttpMethods
    {
        public static readonly string Connect = "CONNECT";
        public static readonly string Delete = "DELETE";
        public static readonly string Get = "GET";
        public static readonly string Head = "HEAD";
        public static readonly string Options = "OPTIONS";
        public static readonly string Patch = "PATCH";
        public static readonly string Post = "POST";
        public static readonly string Put = "PUT";
        public static readonly string Trace = "TRACE";
    }

    internal class LoggedMessage
    {
        internal static LoggedMessage RequestMethodNotCacheable => new LoggedMessage(1, LogLevel.Debug);
        internal static LoggedMessage RequestWithAuthorizationNotCacheable => new LoggedMessage(2, LogLevel.Debug);
        internal static LoggedMessage RequestWithNoCacheNotCacheable => new LoggedMessage(3, LogLevel.Debug);
        internal static LoggedMessage RequestWithPragmaNoCacheNotCacheable => new LoggedMessage(4, LogLevel.Debug);
        internal static LoggedMessage ExpirationMinFreshAdded => new LoggedMessage(5, LogLevel.Debug);
        internal static LoggedMessage ExpirationSharedMaxAgeExceeded => new LoggedMessage(6, LogLevel.Debug);
        internal static LoggedMessage ExpirationMustRevalidate => new LoggedMessage(7, LogLevel.Debug);
        internal static LoggedMessage ExpirationMaxStaleSatisfied => new LoggedMessage(8, LogLevel.Debug);
        internal static LoggedMessage ExpirationMaxAgeExceeded => new LoggedMessage(9, LogLevel.Debug);
        internal static LoggedMessage ExpirationExpiresExceeded => new LoggedMessage(10, LogLevel.Debug);
        internal static LoggedMessage ResponseWithoutPublicNotCacheable => new LoggedMessage(11, LogLevel.Debug);
        internal static LoggedMessage ResponseWithNoStoreNotCacheable => new LoggedMessage(12, LogLevel.Debug);
        internal static LoggedMessage ResponseWithNoCacheNotCacheable => new LoggedMessage(13, LogLevel.Debug);
        internal static LoggedMessage ResponseWithSetCookieNotCacheable => new LoggedMessage(14, LogLevel.Debug);
        internal static LoggedMessage ResponseWithVaryStarNotCacheable => new LoggedMessage(15, LogLevel.Debug);
        internal static LoggedMessage ResponseWithPrivateNotCacheable => new LoggedMessage(16, LogLevel.Debug);
        internal static LoggedMessage ResponseWithUnsuccessfulStatusCodeNotCacheable => new LoggedMessage(17, LogLevel.Debug);
        internal static LoggedMessage NotModifiedIfNoneMatchStar => new LoggedMessage(18, LogLevel.Debug);
        internal static LoggedMessage NotModifiedIfNoneMatchMatched => new LoggedMessage(19, LogLevel.Debug);
        internal static LoggedMessage NotModifiedIfUnmodifiedSinceSatisfied => new LoggedMessage(20, LogLevel.Debug);
        internal static LoggedMessage NotModifiedServed => new LoggedMessage(21, LogLevel.Information);
        internal static LoggedMessage CachedResponseServed => new LoggedMessage(22, LogLevel.Information);
        internal static LoggedMessage GatewayTimeoutServed => new LoggedMessage(23, LogLevel.Information);
        internal static LoggedMessage NoResponseServed => new LoggedMessage(24, LogLevel.Information);
        internal static LoggedMessage VaryByRulesUpdated => new LoggedMessage(25, LogLevel.Debug);
        internal static LoggedMessage ResponseCached => new LoggedMessage(26, LogLevel.Information);
        internal static LoggedMessage ResponseNotCached => new LoggedMessage(27, LogLevel.Information);
        internal static LoggedMessage ResponseContentLengthMismatchNotCached => new LoggedMessage(28, LogLevel.Warning);

        private LoggedMessage(int evenId, LogLevel logLevel)
        {
            EventId = evenId;
            LogLevel = logLevel;
        }

        internal int EventId { get; }
        internal LogLevel LogLevel { get; }
    }

    internal class DummySendFileFeature : IHttpSendFileFeature
    {
        public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
        {
            return TaskCache.CompletedTask;
        }
    }

    internal class TestResponseCachePolicyProvider : IResponseCachePolicyProvider
    {
        public bool IsCachedEntryFresh(ResponseCacheContext context) => true;

        public bool IsRequestCacheable(ResponseCacheContext context) => true;

        public bool IsResponseCacheable(ResponseCacheContext context) => true;
    }

    internal class TestResponseCacheKeyProvider : IResponseCacheKeyProvider
    {
        private readonly string _baseKey;
        private readonly StringValues _varyKey;

        public TestResponseCacheKeyProvider(string lookupBaseKey = null, StringValues? lookupVaryKey = null)
        {
            _baseKey = lookupBaseKey;
            if (lookupVaryKey.HasValue)
            {
                _varyKey = lookupVaryKey.Value;
            }
        }

        public IEnumerable<string> CreateLookupVaryByKeys(ResponseCacheContext context)
        {
            foreach (var varyKey in _varyKey)
            {
                yield return _baseKey + varyKey;
            }
        }

        public string CreateBaseKey(ResponseCacheContext context)
        {
            return _baseKey;
        }

        public string CreateStorageVaryByKey(ResponseCacheContext context)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestResponseCacheStore : IResponseCacheStore
    {
        private readonly IDictionary<string, IResponseCacheEntry> _storage = new Dictionary<string, IResponseCacheEntry>();
        public int GetCount { get; private set; }
        public int SetCount { get; private set; }

        public Task<IResponseCacheEntry> GetAsync(string key)
        {
            GetCount++;
            try
            {
                return Task.FromResult(_storage[key]);
            }
            catch
            {
                return Task.FromResult<IResponseCacheEntry>(null);
            }
        }

        public Task SetAsync(string key, IResponseCacheEntry entry, TimeSpan validFor)
        {
            SetCount++;
            _storage[key] = entry;
            return TaskCache.CompletedTask;
        }
    }
}

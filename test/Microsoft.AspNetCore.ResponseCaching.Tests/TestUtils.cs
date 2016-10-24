// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
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

        internal static RequestDelegate TestRequestDelegate = async context =>
        {
            var headers = context.Response.GetTypedHeaders();

            var expires = context.Request.Query["Expires"];
            if (!string.IsNullOrEmpty(expires))
            {
                headers.Expires = DateTimeOffset.Now.AddSeconds(int.Parse(expires));
            }

            var uniqueId = Guid.NewGuid().ToString();
            headers.CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = string.IsNullOrEmpty(expires) ? TimeSpan.FromSeconds(10) : (TimeSpan?)null
            };
            headers.Date = DateTimeOffset.UtcNow;
            headers.Headers["X-Value"] = uniqueId;

            if (context.Request.Method != "HEAD")
            {
                await context.Response.WriteAsync(uniqueId);
            }
        };

        internal static IResponseCachingKeyProvider CreateTestKeyProvider()
        {
            return CreateTestKeyProvider(new ResponseCachingOptions());
        }

        internal static IResponseCachingKeyProvider CreateTestKeyProvider(ResponseCachingOptions options)
        {
            return new ResponseCachingKeyProvider(new DefaultObjectPoolProvider(), Options.Create(options));
        }

        internal static IEnumerable<IWebHostBuilder> CreateBuildersWithResponseCaching(
            Action<IApplicationBuilder> configureDelegate = null,
            ResponseCachingOptions options = null,
            RequestDelegate requestDelegate = null)
        {
            if (configureDelegate == null)
            {
                configureDelegate = app => { };
            }
            if (requestDelegate == null)
            {
                requestDelegate = TestRequestDelegate;
            }

            // Test with in memory ResponseCache
            yield return new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddResponseCaching(responseCachingOptions =>
                    {
                        if (options != null)
                        {
                            responseCachingOptions.MaximumBodySize = options.MaximumBodySize;
                            responseCachingOptions.UseCaseSensitivePaths = options.UseCaseSensitivePaths;
                            responseCachingOptions.SystemClock = options.SystemClock;
                        }
                    });
                })
                .Configure(app =>
                {
                    configureDelegate(app);
                    app.UseResponseCaching();
                    app.Run(requestDelegate);
                });
        }

        internal static ResponseCachingMiddleware CreateTestMiddleware(
            IResponseCache cache = null,
            ResponseCachingOptions options = null,
            TestSink testSink = null,
            IResponseCachingKeyProvider keyProvider = null,
            IResponseCachingPolicyProvider policyProvider = null)
        {
            if (cache == null)
            {
                cache = new TestResponseCache();
            }
            if (options == null)
            {
                options = new ResponseCachingOptions();
            }
            if (keyProvider == null)
            {
                keyProvider = new ResponseCachingKeyProvider(new DefaultObjectPoolProvider(), Options.Create(options));
            }
            if (policyProvider == null)
            {
                policyProvider = new TestResponseCachingPolicyProvider();
            }

            return new ResponseCachingMiddleware(
                httpContext => TaskCache.CompletedTask,
                Options.Create(options),
                testSink == null ? (ILoggerFactory)NullLoggerFactory.Instance : new TestLoggerFactory(testSink, true),
                policyProvider,
                cache,
                keyProvider);
        }

        internal static ResponseCachingContext CreateTestContext()
        {
            return new ResponseCachingContext(new DefaultHttpContext(), NullLogger.Instance)
            {
                ResponseTime = DateTimeOffset.UtcNow
            };
        }

        internal static ResponseCachingContext CreateTestContext(ITestSink testSink)
        {
            return new ResponseCachingContext(new DefaultHttpContext(), new TestLogger("ResponseCachingTests", testSink, true))
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

        public static HttpRequestMessage CreateRequest(string method, string requestUri)
        {
            return new HttpRequestMessage(new HttpMethod(method), requestUri);
        }
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

    internal class TestResponseCachingPolicyProvider : IResponseCachingPolicyProvider
    {
        public bool IsCachedEntryFresh(ResponseCachingContext context) => true;

        public bool IsRequestCacheable(ResponseCachingContext context) => true;

        public bool IsResponseCacheable(ResponseCachingContext context) => true;
    }

    internal class TestResponseCachingKeyProvider : IResponseCachingKeyProvider
    {
        private readonly string _baseKey;
        private readonly StringValues _varyKey;

        public TestResponseCachingKeyProvider(string lookupBaseKey = null, StringValues? lookupVaryKey = null)
        {
            _baseKey = lookupBaseKey;
            if (lookupVaryKey.HasValue)
            {
                _varyKey = lookupVaryKey.Value;
            }
        }

        public IEnumerable<string> CreateLookupVaryByKeys(ResponseCachingContext context)
        {
            foreach (var varyKey in _varyKey)
            {
                yield return _baseKey + varyKey;
            }
        }

        public string CreateBaseKey(ResponseCachingContext context)
        {
            return _baseKey;
        }

        public string CreateStorageVaryByKey(ResponseCachingContext context)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestResponseCache : IResponseCache
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

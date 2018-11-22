// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    /// <summary>
    /// Defines *all* the logger messages produced by response caching
    /// </summary>
    internal static class LoggerExtensions
    {
        private static Action<ILogger, string, Exception> _logRequestMethodNotCacheable;
        private static Action<ILogger, Exception> _logRequestWithAuthorizationNotCacheable;
        private static Action<ILogger, Exception> _logRequestWithNoCacheNotCacheable;
        private static Action<ILogger, Exception> _logRequestWithPragmaNoCacheNotCacheable;
        private static Action<ILogger, TimeSpan, Exception> _logExpirationMinFreshAdded;
        private static Action<ILogger, TimeSpan, TimeSpan, Exception> _logExpirationSharedMaxAgeExceeded;
        private static Action<ILogger, TimeSpan, TimeSpan, Exception> _logExpirationMustRevalidate;
        private static Action<ILogger, TimeSpan, TimeSpan, TimeSpan, Exception> _logExpirationMaxStaleSatisfied;
        private static Action<ILogger, TimeSpan, TimeSpan, Exception> _logExpirationMaxAgeExceeded;
        private static Action<ILogger, DateTimeOffset, DateTimeOffset, Exception> _logExpirationExpiresExceeded;
        private static Action<ILogger, Exception> _logResponseWithoutPublicNotCacheable;
        private static Action<ILogger, Exception> _logResponseWithNoStoreNotCacheable;
        private static Action<ILogger, Exception> _logResponseWithNoCacheNotCacheable;
        private static Action<ILogger, Exception> _logResponseWithSetCookieNotCacheable;
        private static Action<ILogger, Exception> _logResponseWithVaryStarNotCacheable;
        private static Action<ILogger, Exception> _logResponseWithPrivateNotCacheable;
        private static Action<ILogger, int, Exception> _logResponseWithUnsuccessfulStatusCodeNotCacheable;
        private static Action<ILogger, Exception> _logNotModifiedIfNoneMatchStar;
        private static Action<ILogger, EntityTagHeaderValue, Exception> _logNotModifiedIfNoneMatchMatched;
        private static Action<ILogger, DateTimeOffset, DateTimeOffset, Exception> _logNotModifiedIfModifiedSinceSatisfied;
        private static Action<ILogger, Exception> _logNotModifiedServed;
        private static Action<ILogger, Exception> _logCachedResponseServed;
        private static Action<ILogger, Exception> _logGatewayTimeoutServed;
        private static Action<ILogger, Exception> _logNoResponseServed;
        private static Action<ILogger, string, string, Exception> _logVaryByRulesUpdated;
        private static Action<ILogger, Exception> _logResponseCached;
        private static Action<ILogger, Exception> _logResponseNotCached;
        private static Action<ILogger, Exception> _logResponseContentLengthMismatchNotCached;
        private static Action<ILogger, TimeSpan, TimeSpan, Exception> _logExpirationInfiniteMaxStaleSatisfied;

        static LoggerExtensions()
        {
            _logRequestMethodNotCacheable = LoggerMessage.Define<string>(
                logLevel: LogLevel.Debug,
                eventId: 1,
                formatString: "The request cannot be served from cache because it uses the HTTP method: {Method}.");
            _logRequestWithAuthorizationNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: 2,
                formatString: $"The request cannot be served from cache because it contains an '{HeaderNames.Authorization}' header.");
            _logRequestWithNoCacheNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: 3,
                formatString: "The request cannot be served from cache because it contains a 'no-cache' cache directive.");
            _logRequestWithPragmaNoCacheNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: 4,
                formatString: "The request cannot be served from cache because it contains a 'no-cache' pragma directive.");
            _logExpirationMinFreshAdded = LoggerMessage.Define<TimeSpan>(
                logLevel: LogLevel.Debug,
                eventId: 5,
                formatString: "Adding a minimum freshness requirement of {Duration} specified by the 'min-fresh' cache directive.");
            _logExpirationSharedMaxAgeExceeded = LoggerMessage.Define<TimeSpan, TimeSpan>(
                logLevel: LogLevel.Debug,
                eventId: 6,
                formatString: "The age of the entry is {Age} and has exceeded the maximum age for shared caches of {SharedMaxAge} specified by the 's-maxage' cache directive.");
            _logExpirationMustRevalidate = LoggerMessage.Define<TimeSpan, TimeSpan>(
                logLevel: LogLevel.Debug,
                eventId: 7,
                formatString: "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive. It must be revalidated because the 'must-revalidate' or 'proxy-revalidate' cache directive is specified.");
            _logExpirationMaxStaleSatisfied = LoggerMessage.Define<TimeSpan, TimeSpan, TimeSpan>(
                logLevel: LogLevel.Debug,
                eventId: 8,
                formatString: "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive. However, it satisfied the maximum stale allowance of {MaxStale} specified by the 'max-stale' cache directive.");
            _logExpirationMaxAgeExceeded = LoggerMessage.Define<TimeSpan, TimeSpan>(
                logLevel: LogLevel.Debug,
                eventId: 9,
                formatString: "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive.");
            _logExpirationExpiresExceeded = LoggerMessage.Define<DateTimeOffset, DateTimeOffset>(
                logLevel: LogLevel.Debug,
                eventId: 10,
                formatString: $"The response time of the entry is {{ResponseTime}} and has exceeded the expiry date of {{Expired}} specified by the '{HeaderNames.Expires}' header.");
            _logResponseWithoutPublicNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: 11,
                formatString: "Response is not cacheable because it does not contain the 'public' cache directive.");
            _logResponseWithNoStoreNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: 12,
                formatString: "Response is not cacheable because it or its corresponding request contains a 'no-store' cache directive.");
            _logResponseWithNoCacheNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: 13,
                formatString: "Response is not cacheable because it contains a 'no-cache' cache directive.");
            _logResponseWithSetCookieNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: 14,
                formatString: $"Response is not cacheable because it contains a '{HeaderNames.SetCookie}' header.");
            _logResponseWithVaryStarNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: 15,
                formatString: $"Response is not cacheable because it contains a '{HeaderNames.Vary}' header with a value of *.");
            _logResponseWithPrivateNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: 16,
                formatString: "Response is not cacheable because it contains the 'private' cache directive.");
            _logResponseWithUnsuccessfulStatusCodeNotCacheable = LoggerMessage.Define<int>(
                logLevel: LogLevel.Debug,
                eventId: 17,
                formatString: "Response is not cacheable because its status code {StatusCode} does not indicate success.");
            _logNotModifiedIfNoneMatchStar = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: 18,
                formatString: $"The '{HeaderNames.IfNoneMatch}' header of the request contains a value of *.");
            _logNotModifiedIfNoneMatchMatched = LoggerMessage.Define<EntityTagHeaderValue>(
                logLevel: LogLevel.Debug,
                eventId: 19,
                formatString: $"The ETag {{ETag}} in the '{HeaderNames.IfNoneMatch}' header matched the ETag of a cached entry.");
            _logNotModifiedIfModifiedSinceSatisfied = LoggerMessage.Define<DateTimeOffset, DateTimeOffset>(
                logLevel: LogLevel.Debug,
                eventId: 20,
                formatString: $"The last modified date of {{LastModified}} is before the date {{IfModifiedSince}} specified in the '{HeaderNames.IfModifiedSince}' header.");
            _logNotModifiedServed = LoggerMessage.Define(
                logLevel: LogLevel.Information,
                eventId: 21,
                formatString: "The content requested has not been modified.");
            _logCachedResponseServed = LoggerMessage.Define(
                logLevel: LogLevel.Information,
                eventId: 22,
                formatString: "Serving response from cache.");
            _logGatewayTimeoutServed = LoggerMessage.Define(
                logLevel: LogLevel.Information,
                eventId: 23,
                formatString: "No cached response available for this request and the 'only-if-cached' cache directive was specified.");
            _logNoResponseServed = LoggerMessage.Define(
                logLevel: LogLevel.Information,
                eventId: 24,
                formatString: "No cached response available for this request.");
            _logVaryByRulesUpdated = LoggerMessage.Define<string, string>(
                logLevel: LogLevel.Debug,
                eventId: 25,
                formatString: "Vary by rules were updated. Headers: {Headers}, Query keys: {QueryKeys}");
            _logResponseCached = LoggerMessage.Define(
                logLevel: LogLevel.Information,
                eventId: 26,
                formatString: "The response has been cached.");
            _logResponseNotCached = LoggerMessage.Define(
                logLevel: LogLevel.Information,
                eventId: 27,
                formatString: "The response could not be cached for this request.");
            _logResponseContentLengthMismatchNotCached = LoggerMessage.Define(
                logLevel: LogLevel.Warning,
                eventId: 28,
                formatString: $"The response could not be cached for this request because the '{HeaderNames.ContentLength}' did not match the body length.");
            _logExpirationInfiniteMaxStaleSatisfied = LoggerMessage.Define<TimeSpan, TimeSpan>(
                logLevel: LogLevel.Debug,
                eventId: 29,
                formatString: "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive. However, the 'max-stale' cache directive was specified without an assigned value and a stale response of any age is accepted.");
        }

        internal static void LogRequestMethodNotCacheable(this ILogger logger, string method)
        {
            _logRequestMethodNotCacheable(logger, method, null);
        }

        internal static void LogRequestWithAuthorizationNotCacheable(this ILogger logger)
        {
            _logRequestWithAuthorizationNotCacheable(logger, null);
        }

        internal static void LogRequestWithNoCacheNotCacheable(this ILogger logger)
        {
            _logRequestWithNoCacheNotCacheable(logger, null);
        }

        internal static void LogRequestWithPragmaNoCacheNotCacheable(this ILogger logger)
        {
            _logRequestWithPragmaNoCacheNotCacheable(logger, null);
        }

        internal static void LogExpirationMinFreshAdded(this ILogger logger, TimeSpan duration)
        {
            _logExpirationMinFreshAdded(logger, duration, null);
        }

        internal static void LogExpirationSharedMaxAgeExceeded(this ILogger logger, TimeSpan age, TimeSpan sharedMaxAge)
        {
            _logExpirationSharedMaxAgeExceeded(logger, age, sharedMaxAge, null);
        }

        internal static void LogExpirationMustRevalidate(this ILogger logger, TimeSpan age, TimeSpan maxAge)
        {
            _logExpirationMustRevalidate(logger, age, maxAge, null);
        }

        internal static void LogExpirationMaxStaleSatisfied(this ILogger logger, TimeSpan age, TimeSpan maxAge, TimeSpan maxStale)
        {
            _logExpirationMaxStaleSatisfied(logger, age, maxAge, maxStale, null);
        }

        internal static void LogExpirationMaxAgeExceeded(this ILogger logger, TimeSpan age, TimeSpan sharedMaxAge)
        {
            _logExpirationMaxAgeExceeded(logger, age, sharedMaxAge, null);
        }

        internal static void LogExpirationExpiresExceeded(this ILogger logger, DateTimeOffset responseTime, DateTimeOffset expires)
        {
            _logExpirationExpiresExceeded(logger, responseTime, expires, null);
        }

        internal static void LogResponseWithoutPublicNotCacheable(this ILogger logger)
        {
            _logResponseWithoutPublicNotCacheable(logger, null);
        }

        internal static void LogResponseWithNoStoreNotCacheable(this ILogger logger)
        {
            _logResponseWithNoStoreNotCacheable(logger, null);
        }

        internal static void LogResponseWithNoCacheNotCacheable(this ILogger logger)
        {
            _logResponseWithNoCacheNotCacheable(logger, null);
        }

        internal static void LogResponseWithSetCookieNotCacheable(this ILogger logger)
        {
            _logResponseWithSetCookieNotCacheable(logger, null);
        }

        internal static void LogResponseWithVaryStarNotCacheable(this ILogger logger)
        {
            _logResponseWithVaryStarNotCacheable(logger, null);
        }

        internal static void LogResponseWithPrivateNotCacheable(this ILogger logger)
        {
            _logResponseWithPrivateNotCacheable(logger, null);
        }

        internal static void LogResponseWithUnsuccessfulStatusCodeNotCacheable(this ILogger logger, int statusCode)
        {
            _logResponseWithUnsuccessfulStatusCodeNotCacheable(logger, statusCode, null);
        }

        internal static void LogNotModifiedIfNoneMatchStar(this ILogger logger)
        {
            _logNotModifiedIfNoneMatchStar(logger, null);
        }

        internal static void LogNotModifiedIfNoneMatchMatched(this ILogger logger, EntityTagHeaderValue etag)
        {
            _logNotModifiedIfNoneMatchMatched(logger, etag, null);
        }

        internal static void LogNotModifiedIfModifiedSinceSatisfied(this ILogger logger, DateTimeOffset lastModified, DateTimeOffset ifModifiedSince)
        {
            _logNotModifiedIfModifiedSinceSatisfied(logger, lastModified, ifModifiedSince, null);
        }

        internal static void LogNotModifiedServed(this ILogger logger)
        {
            _logNotModifiedServed(logger, null);
        }

        internal static void LogCachedResponseServed(this ILogger logger)
        {
            _logCachedResponseServed(logger, null);
        }

        internal static void LogGatewayTimeoutServed(this ILogger logger)
        {
            _logGatewayTimeoutServed(logger, null);
        }

        internal static void LogNoResponseServed(this ILogger logger)
        {
            _logNoResponseServed(logger, null);
        }

        internal static void LogVaryByRulesUpdated(this ILogger logger, string headers, string queryKeys)
        {
            _logVaryByRulesUpdated(logger, headers, queryKeys, null);
        }

        internal static void LogResponseCached(this ILogger logger)
        {
            _logResponseCached(logger, null);
        }

        internal static void LogResponseNotCached(this ILogger logger)
        {
            _logResponseNotCached(logger, null);
        }

        internal static void LogResponseContentLengthMismatchNotCached(this ILogger logger)
        {
            _logResponseContentLengthMismatchNotCached(logger, null);
        }

        internal static void LogExpirationInfiniteMaxStaleSatisfied(this ILogger logger, TimeSpan age, TimeSpan maxAge)
        {
            _logExpirationInfiniteMaxStaleSatisfied(logger, age, maxAge, null);
        }
    }
}

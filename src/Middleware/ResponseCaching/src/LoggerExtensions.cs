// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching
{
    /// <summary>
    /// Defines *all* the logger messages produced by response caching
    /// </summary>
    internal static class LoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _requestMethodNotCacheable;
        private static readonly Action<ILogger, Exception> _requestWithAuthorizationNotCacheable;
        private static readonly Action<ILogger, Exception> _requestWithNoCacheNotCacheable;
        private static readonly Action<ILogger, Exception> _requestWithPragmaNoCacheNotCacheable;
        private static readonly Action<ILogger, TimeSpan, Exception> _expirationMinFreshAdded;
        private static readonly Action<ILogger, TimeSpan, TimeSpan, Exception> _expirationSharedMaxAgeExceeded;
        private static readonly Action<ILogger, TimeSpan, TimeSpan, Exception> _expirationMustRevalidate;
        private static readonly Action<ILogger, TimeSpan, TimeSpan, TimeSpan, Exception> _expirationMaxStaleSatisfied;
        private static readonly Action<ILogger, TimeSpan, TimeSpan, Exception> _expirationMaxAgeExceeded;
        private static readonly Action<ILogger, DateTimeOffset, DateTimeOffset, Exception> _expirationExpiresExceeded;
        private static readonly Action<ILogger, Exception> _responseWithoutPublicNotCacheable;
        private static readonly Action<ILogger, Exception> _responseWithNoStoreNotCacheable;
        private static readonly Action<ILogger, Exception> _responseWithNoCacheNotCacheable;
        private static readonly Action<ILogger, Exception> _responseWithSetCookieNotCacheable;
        private static readonly Action<ILogger, Exception> _responseWithVaryStarNotCacheable;
        private static readonly Action<ILogger, Exception> _responseWithPrivateNotCacheable;
        private static readonly Action<ILogger, int, Exception> _responseWithUnsuccessfulStatusCodeNotCacheable;
        private static readonly Action<ILogger, Exception> _notModifiedIfNoneMatchStar;
        private static readonly Action<ILogger, EntityTagHeaderValue, Exception> _notModifiedIfNoneMatchMatched;
        private static readonly Action<ILogger, DateTimeOffset, DateTimeOffset, Exception> _notModifiedIfModifiedSinceSatisfied;
        private static readonly Action<ILogger, Exception> _notModifiedServed;
        private static readonly Action<ILogger, Exception> _cachedResponseServed;
        private static readonly Action<ILogger, Exception> _gatewayTimeoutServed;
        private static readonly Action<ILogger, Exception> _noResponseServed;
        private static readonly Action<ILogger, string, string, Exception> _varyByRulesUpdated;
        private static readonly Action<ILogger, Exception> _responseCached;
        private static readonly Action<ILogger, Exception> _responseNotCached;
        private static readonly Action<ILogger, Exception> _responseContentLengthMismatchNotCached;
        private static readonly Action<ILogger, TimeSpan, TimeSpan, Exception> _expirationInfiniteMaxStaleSatisfied;

        static LoggerExtensions()
        {
            _requestMethodNotCacheable = LoggerMessage.Define<string>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(1, "RequestMethodNotCacheable"),
                formatString: "The request cannot be served from cache because it uses the HTTP method: {Method}.");
            _requestWithAuthorizationNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: new EventId(2, "RequestWithAuthorizationNotCacheable"),
                formatString: $"The request cannot be served from cache because it contains an '{HeaderNames.Authorization}' header.");
            _requestWithNoCacheNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: new EventId(3, "RequestWithNoCacheNotCacheable"),
                formatString: "The request cannot be served from cache because it contains a 'no-cache' cache directive.");
            _requestWithPragmaNoCacheNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: new EventId(4, "RequestWithPragmaNoCacheNotCacheable"),
                formatString: "The request cannot be served from cache because it contains a 'no-cache' pragma directive.");
            _expirationMinFreshAdded = LoggerMessage.Define<TimeSpan>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(5, "LogRequestMethodNotCacheable"),
                formatString: "Adding a minimum freshness requirement of {Duration} specified by the 'min-fresh' cache directive.");
            _expirationSharedMaxAgeExceeded = LoggerMessage.Define<TimeSpan, TimeSpan>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(6, "ExpirationSharedMaxAgeExceeded"),
                formatString: "The age of the entry is {Age} and has exceeded the maximum age for shared caches of {SharedMaxAge} specified by the 's-maxage' cache directive.");
            _expirationMustRevalidate = LoggerMessage.Define<TimeSpan, TimeSpan>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(7, "ExpirationMustRevalidate"),
                formatString: "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive. It must be revalidated because the 'must-revalidate' or 'proxy-revalidate' cache directive is specified.");
            _expirationMaxStaleSatisfied = LoggerMessage.Define<TimeSpan, TimeSpan, TimeSpan>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(8, "ExpirationMaxStaleSatisfied"),
                formatString: "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive. However, it satisfied the maximum stale allowance of {MaxStale} specified by the 'max-stale' cache directive.");
            _expirationMaxAgeExceeded = LoggerMessage.Define<TimeSpan, TimeSpan>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(9, "ExpirationMaxAgeExceeded"),
                formatString: "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive.");
            _expirationExpiresExceeded = LoggerMessage.Define<DateTimeOffset, DateTimeOffset>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(10, "ExpirationExpiresExceeded"),
                formatString: $"The response time of the entry is {{ResponseTime}} and has exceeded the expiry date of {{Expired}} specified by the '{HeaderNames.Expires}' header.");
            _responseWithoutPublicNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: new EventId(11, "ResponseWithoutPublicNotCacheable"),
                formatString: "Response is not cacheable because it does not contain the 'public' cache directive.");
            _responseWithNoStoreNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: new EventId(12, "ResponseWithNoStoreNotCacheable"),
                formatString: "Response is not cacheable because it or its corresponding request contains a 'no-store' cache directive.");
            _responseWithNoCacheNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: new EventId(13, "ResponseWithNoCacheNotCacheable"),
                formatString: "Response is not cacheable because it contains a 'no-cache' cache directive.");
            _responseWithSetCookieNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: new EventId(14, "ResponseWithSetCookieNotCacheable"),
                formatString: $"Response is not cacheable because it contains a '{HeaderNames.SetCookie}' header.");
            _responseWithVaryStarNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: new EventId(15, "ResponseWithVaryStarNotCacheable"),
                formatString: $"Response is not cacheable because it contains a '{HeaderNames.Vary}' header with a value of *.");
            _responseWithPrivateNotCacheable = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: new EventId(16, "ResponseWithPrivateNotCacheable"),
                formatString: "Response is not cacheable because it contains the 'private' cache directive.");
            _responseWithUnsuccessfulStatusCodeNotCacheable = LoggerMessage.Define<int>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(17, "ResponseWithUnsuccessfulStatusCodeNotCacheable"),
                formatString: "Response is not cacheable because its status code {StatusCode} does not indicate success.");
            _notModifiedIfNoneMatchStar = LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: new EventId(18, "ExpirationExpiresExceeded"),
                formatString: $"The '{HeaderNames.IfNoneMatch}' header of the request contains a value of *.");
            _notModifiedIfNoneMatchMatched = LoggerMessage.Define<EntityTagHeaderValue>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(19, "NotModifiedIfNoneMatchMatched"),
                formatString: $"The ETag {{ETag}} in the '{HeaderNames.IfNoneMatch}' header matched the ETag of a cached entry.");
            _notModifiedIfModifiedSinceSatisfied = LoggerMessage.Define<DateTimeOffset, DateTimeOffset>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(20, "NotModifiedIfModifiedSinceSatisfied"),
                formatString: $"The last modified date of {{LastModified}} is before the date {{IfModifiedSince}} specified in the '{HeaderNames.IfModifiedSince}' header.");
            _notModifiedServed = LoggerMessage.Define(
                logLevel: LogLevel.Information,
                eventId: new EventId(21, "NotModifiedServed"),
                formatString: "The content requested has not been modified.");
            _cachedResponseServed = LoggerMessage.Define(
                logLevel: LogLevel.Information,
                eventId: new EventId(22, "CachedResponseServed"),
                formatString: "Serving response from cache.");
            _gatewayTimeoutServed = LoggerMessage.Define(
                logLevel: LogLevel.Information,
                eventId: new EventId(23, "GatewayTimeoutServed"),
                formatString: "No cached response available for this request and the 'only-if-cached' cache directive was specified.");
            _noResponseServed = LoggerMessage.Define(
                logLevel: LogLevel.Information,
                eventId: new EventId(24, "NoResponseServed"),
                formatString: "No cached response available for this request.");
            _varyByRulesUpdated = LoggerMessage.Define<string, string>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(25, "VaryByRulesUpdated"),
                formatString: "Vary by rules were updated. Headers: {Headers}, Query keys: {QueryKeys}");
            _responseCached = LoggerMessage.Define(
                logLevel: LogLevel.Information,
                eventId: new EventId(26, "ResponseCached"),
                formatString: "The response has been cached.");
            _responseNotCached = LoggerMessage.Define(
                logLevel: LogLevel.Information,
                eventId: new EventId(27, "ResponseNotCached"),
                formatString: "The response could not be cached for this request.");
            _responseContentLengthMismatchNotCached = LoggerMessage.Define(
                logLevel: LogLevel.Warning,
                eventId: new EventId(28, "responseContentLengthMismatchNotCached"),
                formatString: $"The response could not be cached for this request because the '{HeaderNames.ContentLength}' did not match the body length.");
            _expirationInfiniteMaxStaleSatisfied = LoggerMessage.Define<TimeSpan, TimeSpan>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(29, "ExpirationInfiniteMaxStaleSatisfied"),
                formatString: "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive. However, the 'max-stale' cache directive was specified without an assigned value and a stale response of any age is accepted.");
        }

        internal static void RequestMethodNotCacheable(this ILogger logger, string method)
        {
            _requestMethodNotCacheable(logger, method, null);
        }

        internal static void RequestWithAuthorizationNotCacheable(this ILogger logger)
        {
            _requestWithAuthorizationNotCacheable(logger, null);
        }

        internal static void RequestWithNoCacheNotCacheable(this ILogger logger)
        {
            _requestWithNoCacheNotCacheable(logger, null);
        }

        internal static void RequestWithPragmaNoCacheNotCacheable(this ILogger logger)
        {
            _requestWithPragmaNoCacheNotCacheable(logger, null);
        }

        internal static void ExpirationMinFreshAdded(this ILogger logger, TimeSpan duration)
        {
            _expirationMinFreshAdded(logger, duration, null);
        }

        internal static void ExpirationSharedMaxAgeExceeded(this ILogger logger, TimeSpan age, TimeSpan sharedMaxAge)
        {
            _expirationSharedMaxAgeExceeded(logger, age, sharedMaxAge, null);
        }

        internal static void ExpirationMustRevalidate(this ILogger logger, TimeSpan age, TimeSpan maxAge)
        {
            _expirationMustRevalidate(logger, age, maxAge, null);
        }

        internal static void ExpirationMaxStaleSatisfied(this ILogger logger, TimeSpan age, TimeSpan maxAge, TimeSpan maxStale)
        {
            _expirationMaxStaleSatisfied(logger, age, maxAge, maxStale, null);
        }

        internal static void ExpirationMaxAgeExceeded(this ILogger logger, TimeSpan age, TimeSpan sharedMaxAge)
        {
            _expirationMaxAgeExceeded(logger, age, sharedMaxAge, null);
        }

        internal static void ExpirationExpiresExceeded(this ILogger logger, DateTimeOffset responseTime, DateTimeOffset expires)
        {
            _expirationExpiresExceeded(logger, responseTime, expires, null);
        }

        internal static void ResponseWithoutPublicNotCacheable(this ILogger logger)
        {
            _responseWithoutPublicNotCacheable(logger, null);
        }

        internal static void ResponseWithNoStoreNotCacheable(this ILogger logger)
        {
            _responseWithNoStoreNotCacheable(logger, null);
        }

        internal static void ResponseWithNoCacheNotCacheable(this ILogger logger)
        {
            _responseWithNoCacheNotCacheable(logger, null);
        }

        internal static void ResponseWithSetCookieNotCacheable(this ILogger logger)
        {
            _responseWithSetCookieNotCacheable(logger, null);
        }

        internal static void ResponseWithVaryStarNotCacheable(this ILogger logger)
        {
            _responseWithVaryStarNotCacheable(logger, null);
        }

        internal static void ResponseWithPrivateNotCacheable(this ILogger logger)
        {
            _responseWithPrivateNotCacheable(logger, null);
        }

        internal static void ResponseWithUnsuccessfulStatusCodeNotCacheable(this ILogger logger, int statusCode)
        {
            _responseWithUnsuccessfulStatusCodeNotCacheable(logger, statusCode, null);
        }

        internal static void NotModifiedIfNoneMatchStar(this ILogger logger)
        {
            _notModifiedIfNoneMatchStar(logger, null);
        }

        internal static void NotModifiedIfNoneMatchMatched(this ILogger logger, EntityTagHeaderValue etag)
        {
            _notModifiedIfNoneMatchMatched(logger, etag, null);
        }

        internal static void NotModifiedIfModifiedSinceSatisfied(this ILogger logger, DateTimeOffset lastModified, DateTimeOffset ifModifiedSince)
        {
            _notModifiedIfModifiedSinceSatisfied(logger, lastModified, ifModifiedSince, null);
        }

        internal static void NotModifiedServed(this ILogger logger)
        {
            _notModifiedServed(logger, null);
        }

        internal static void CachedResponseServed(this ILogger logger)
        {
            _cachedResponseServed(logger, null);
        }

        internal static void GatewayTimeoutServed(this ILogger logger)
        {
            _gatewayTimeoutServed(logger, null);
        }

        internal static void NoResponseServed(this ILogger logger)
        {
            _noResponseServed(logger, null);
        }

        internal static void VaryByRulesUpdated(this ILogger logger, string headers, string queryKeys)
        {
            _varyByRulesUpdated(logger, headers, queryKeys, null);
        }

        internal static void ResponseCached(this ILogger logger)
        {
            _responseCached(logger, null);
        }

        internal static void LogResponseNotCached(this ILogger logger)
        {
            _responseNotCached(logger, null);
        }

        internal static void ResponseContentLengthMismatchNotCached(this ILogger logger)
        {
            _responseContentLengthMismatchNotCached(logger, null);
        }

        internal static void ExpirationInfiniteMaxStaleSatisfied(this ILogger logger, TimeSpan age, TimeSpan maxAge)
        {
            _expirationInfiniteMaxStaleSatisfied(logger, age, maxAge, null);
        }
    }
}

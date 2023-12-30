// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching;

/// <summary>
/// Defines *all* the logger messages produced by response caching
/// </summary>
internal static partial class LoggerExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "The request cannot be served from cache because it uses the HTTP method: {Method}.",
        EventName = "RequestMethodNotCacheable")]
    internal static partial void RequestMethodNotCacheable(this ILogger logger, string method);

    [LoggerMessage(2, LogLevel.Debug, "The request cannot be served from cache because it contains an 'Authorization' header.",
        EventName = "RequestWithAuthorizationNotCacheable")]
    internal static partial void RequestWithAuthorizationNotCacheable(this ILogger logger);

    [LoggerMessage(3, LogLevel.Debug, "The request cannot be served from cache because it contains a 'no-cache' cache directive.",
        EventName = "RequestWithNoCacheNotCacheable")]
    internal static partial void RequestWithNoCacheNotCacheable(this ILogger logger);

    [LoggerMessage(4, LogLevel.Debug, "The request cannot be served from cache because it contains a 'no-cache' pragma directive.",
        EventName = "RequestWithPragmaNoCacheNotCacheable")]
    internal static partial void RequestWithPragmaNoCacheNotCacheable(this ILogger logger);

    [LoggerMessage(5, LogLevel.Debug, "Adding a minimum freshness requirement of {Duration} specified by the 'min-fresh' cache directive.",
        EventName = "LogRequestMethodNotCacheable")]
    internal static partial void ExpirationMinFreshAdded(this ILogger logger, TimeSpan duration);

    [LoggerMessage(6, LogLevel.Debug, "The age of the entry is {Age} and has exceeded the maximum age for shared caches of {SharedMaxAge} specified by the 's-maxage' cache directive.",
        EventName = "ExpirationSharedMaxAgeExceeded")]
    internal static partial void ExpirationSharedMaxAgeExceeded(this ILogger logger, TimeSpan age, TimeSpan sharedMaxAge);

    [LoggerMessage(7, LogLevel.Debug, "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive. " +
        "It must be revalidated because the 'must-revalidate' or 'proxy-revalidate' cache directive is specified.",
        EventName = "ExpirationMustRevalidate")]
    internal static partial void ExpirationMustRevalidate(this ILogger logger, TimeSpan age, TimeSpan maxAge);

    [LoggerMessage(8, LogLevel.Debug, "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive. " +
        "However, it satisfied the maximum stale allowance of {MaxStale} specified by the 'max-stale' cache directive.",
        EventName = "ExpirationMaxStaleSatisfied")]
    internal static partial void ExpirationMaxStaleSatisfied(this ILogger logger, TimeSpan age, TimeSpan maxAge, TimeSpan maxStale);

    [LoggerMessage(9, LogLevel.Debug, "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive.", EventName = "ExpirationMaxAgeExceeded")]
    internal static partial void ExpirationMaxAgeExceeded(this ILogger logger, TimeSpan age, TimeSpan maxAge);

    [LoggerMessage(10, LogLevel.Debug, "The response time of the entry is {ResponseTime} and has exceeded the expiry date of {Expired} specified by the 'Expires' header.",
        EventName = "ExpirationExpiresExceeded")]
    internal static partial void ExpirationExpiresExceeded(this ILogger logger, DateTimeOffset responseTime, DateTimeOffset expired);

    [LoggerMessage(11, LogLevel.Debug, "Response is not cacheable because it does not contain the 'public' cache directive.",
        EventName = "ResponseWithoutPublicNotCacheable")]
    internal static partial void ResponseWithoutPublicNotCacheable(this ILogger logger);

    [LoggerMessage(12, LogLevel.Debug, "Response is not cacheable because it or its corresponding request contains a 'no-store' cache directive.",
        EventName = "ResponseWithNoStoreNotCacheable")]
    internal static partial void ResponseWithNoStoreNotCacheable(this ILogger logger);
    [LoggerMessage(13, LogLevel.Debug, "Response is not cacheable because it contains a 'no-cache' cache directive.",
        EventName = "ResponseWithNoCacheNotCacheable")]
    internal static partial void ResponseWithNoCacheNotCacheable(this ILogger logger);

    [LoggerMessage(14, LogLevel.Debug, "Response is not cacheable because it contains a 'SetCookie' header.", EventName = "ResponseWithSetCookieNotCacheable")]
    internal static partial void ResponseWithSetCookieNotCacheable(this ILogger logger);

    [LoggerMessage(15, LogLevel.Debug, "Response is not cacheable because it contains a '.Vary' header with a value of *.",
        EventName = "ResponseWithVaryStarNotCacheable")]
    internal static partial void ResponseWithVaryStarNotCacheable(this ILogger logger);

    [LoggerMessage(16, LogLevel.Debug, "Response is not cacheable because it contains the 'private' cache directive.",
        EventName = "ResponseWithPrivateNotCacheable")]
    internal static partial void ResponseWithPrivateNotCacheable(this ILogger logger);

    [LoggerMessage(17, LogLevel.Debug, "Response is not cacheable because its status code {StatusCode} does not indicate success.",
        EventName = "ResponseWithUnsuccessfulStatusCodeNotCacheable")]
    internal static partial void ResponseWithUnsuccessfulStatusCodeNotCacheable(this ILogger logger, int statusCode);

    [LoggerMessage(18, LogLevel.Debug, "The 'IfNoneMatch' header of the request contains a value of *.", EventName = "NotModifiedIfNoneMatchStar")]
    internal static partial void NotModifiedIfNoneMatchStar(this ILogger logger);

    [LoggerMessage(19, LogLevel.Debug, "The ETag {ETag} in the 'IfNoneMatch' header matched the ETag of a cached entry.",
        EventName = "NotModifiedIfNoneMatchMatched")]
    internal static partial void NotModifiedIfNoneMatchMatched(this ILogger logger, EntityTagHeaderValue etag);

    [LoggerMessage(20, LogLevel.Debug, "The last modified date of {LastModified} is before the date {IfModifiedSince} specified in the 'IfModifiedSince' header.",
        EventName = "NotModifiedIfModifiedSinceSatisfied")]
    internal static partial void NotModifiedIfModifiedSinceSatisfied(this ILogger logger, DateTimeOffset lastModified, DateTimeOffset ifModifiedSince);

    [LoggerMessage(21, LogLevel.Information, "The content requested has not been modified.", EventName = "NotModifiedServed")]
    internal static partial void NotModifiedServed(this ILogger logger);

    [LoggerMessage(22, LogLevel.Information, "Serving response from cache.", EventName = "CachedResponseServed")]
    internal static partial void CachedResponseServed(this ILogger logger);

    [LoggerMessage(23, LogLevel.Information, "No cached response available for this request and the 'only-if-cached' cache directive was specified.",
        EventName = "GatewayTimeoutServed")]
    internal static partial void GatewayTimeoutServed(this ILogger logger);

    [LoggerMessage(24, LogLevel.Information, "No cached response available for this request.", EventName = "NoResponseServed")]
    internal static partial void NoResponseServed(this ILogger logger);

    [LoggerMessage(25, LogLevel.Debug, "Vary by rules were updated. Headers: {Headers}, Query keys: {QueryKeys}", EventName = "VaryByRulesUpdated")]
    internal static partial void VaryByRulesUpdated(this ILogger logger, string headers, string queryKeys);

    [LoggerMessage(26, LogLevel.Information, "The response has been cached.", EventName = "ResponseCached")]
    internal static partial void ResponseCached(this ILogger logger);

    [LoggerMessage(27, LogLevel.Information, "The response could not be cached for this request.", EventName = "ResponseNotCached")]
    internal static partial void LogResponseNotCached(this ILogger logger);

    [LoggerMessage(28, LogLevel.Warning, "The response could not be cached for this request because the 'Content-Length' did not match the body length.",
        EventName = "responseContentLengthMismatchNotCached")]
    internal static partial void ResponseContentLengthMismatchNotCached(this ILogger logger);

    [LoggerMessage(29, LogLevel.Debug,
        "The age of the entry is {Age} and has exceeded the maximum age of {MaxAge} specified by the 'max-age' cache directive. " +
        "However, the 'max-stale' cache directive was specified without an assigned value and a stale response of any age is accepted.",
        EventName = "ExpirationInfiniteMaxStaleSatisfied")]
    internal static partial void ExpirationInfiniteMaxStaleSatisfied(this ILogger logger, TimeSpan age, TimeSpan maxAge);
}

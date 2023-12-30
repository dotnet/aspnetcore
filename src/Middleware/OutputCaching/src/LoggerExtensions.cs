// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Defines the logger messages produced by output caching
/// </summary>
internal static partial class LoggerExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "The 'IfNoneMatch' header of the request contains a value of *.", EventName = "NotModifiedIfNoneMatchStar")]
    internal static partial void NotModifiedIfNoneMatchStar(this ILogger logger);

    [LoggerMessage(2, LogLevel.Debug, "The ETag {ETag} in the 'IfNoneMatch' header matched the ETag of a cached entry.",
        EventName = "NotModifiedIfNoneMatchMatched")]
    internal static partial void NotModifiedIfNoneMatchMatched(this ILogger logger, EntityTagHeaderValue etag);

    [LoggerMessage(3, LogLevel.Debug, "The last modified date of {LastModified} is before the date {IfModifiedSince} specified in the 'IfModifiedSince' header.",
        EventName = "NotModifiedIfModifiedSinceSatisfied")]
    internal static partial void NotModifiedIfModifiedSinceSatisfied(this ILogger logger, DateTimeOffset lastModified, DateTimeOffset ifModifiedSince);

    [LoggerMessage(4, LogLevel.Information, "The content requested has not been modified.", EventName = "NotModifiedServed")]
    internal static partial void NotModifiedServed(this ILogger logger);

    [LoggerMessage(5, LogLevel.Information, "Serving response from cache.", EventName = "CachedResponseServed")]
    internal static partial void CachedResponseServed(this ILogger logger);

    [LoggerMessage(6, LogLevel.Information, "No cached response available for this request and the 'only-if-cached' cache directive was specified.",
        EventName = "GatewayTimeoutServed")]
    internal static partial void GatewayTimeoutServed(this ILogger logger);

    [LoggerMessage(7, LogLevel.Information, "No cached response available for this request.", EventName = "NoResponseServed")]
    internal static partial void NoResponseServed(this ILogger logger);

    [LoggerMessage(8, LogLevel.Information, "The response has been cached.", EventName = "ResponseCached")]
    internal static partial void ResponseCached(this ILogger logger);

    [LoggerMessage(9, LogLevel.Information, "The response could not be cached for this request.", EventName = "ResponseNotCached")]
    internal static partial void ResponseNotCached(this ILogger logger);

    [LoggerMessage(10, LogLevel.Warning, "The response could not be cached for this request because the 'Content-Length' did not match the body length.",
        EventName = "ResponseContentLengthMismatchNotCached")]
    internal static partial void ResponseContentLengthMismatchNotCached(this ILogger logger);

    [LoggerMessage(11, LogLevel.Debug, "The response time of the entry is {ResponseTime} and has exceeded its expiry date.",
        EventName = "ExpirationExpiresExceeded")]
    internal static partial void ExpirationExpiresExceeded(this ILogger logger, DateTimeOffset responseTime);

    [LoggerMessage(12, LogLevel.Error, "Unable to query output cache.", EventName = "UnableToQueryOutputCache")]
    internal static partial void UnableToQueryOutputCache(this ILogger logger, Exception exception);

    [LoggerMessage(13, LogLevel.Error, "Unable to write to output-cache.", EventName = "UnableToWriteToOutputCache")]
    internal static partial void UnableToWriteToOutputCache(this ILogger logger, Exception exception);

}

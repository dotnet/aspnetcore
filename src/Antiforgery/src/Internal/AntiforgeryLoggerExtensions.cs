// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Antiforgery;

internal static partial class AntiforgeryLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "Antiforgery validation failed with message '{Message}'.", EventName = "ValidationFailed")]
    public static partial void ValidationFailed(this ILogger logger, string message);

    [LoggerMessage(2, LogLevel.Debug, "Antiforgery successfully validated a request.", EventName = "Validated")]
    public static partial void ValidatedAntiforgeryToken(this ILogger logger);

    [LoggerMessage(3, LogLevel.Warning, "The required antiforgery cookie '{CookieName}' is not present.", EventName = "MissingCookieToken")]
    public static partial void MissingCookieToken(this ILogger logger, string? cookieName);

    [LoggerMessage(4, LogLevel.Warning, "The required antiforgery request token was not provided in either form field '{FormFieldName}' "
                + "or header '{HeaderName}'.", EventName = "MissingRequestToken")]
    public static partial void MissingRequestToken(this ILogger logger, string formFieldName, string? headerName);

    [LoggerMessage(5, LogLevel.Debug, "A new antiforgery cookie token was created.", EventName = "NewCookieToken")]
    public static partial void NewCookieToken(this ILogger logger);

    [LoggerMessage(6, LogLevel.Debug, "An antiforgery cookie token was reused.", EventName = "ReusedCookieToken")]
    public static partial void ReusedCookieToken(this ILogger logger);

    [LoggerMessage(7, LogLevel.Error, "An exception was thrown while deserializing the token.", EventName = "TokenDeserializeException")]
    public static partial void TokenDeserializeException(this ILogger logger, Exception exception);

    [LoggerMessage(8, LogLevel.Warning, "The 'Cache-Control' and 'Pragma' headers have been overridden and set to 'no-cache, no-store' and " +
            "'no-cache' respectively to prevent caching of this response. Any response that uses antiforgery " +
            "should not be cached.", EventName = "ResponseCacheHeadersOverridenToNoCache")]
    public static partial void ResponseCacheHeadersOverridenToNoCache(this ILogger logger);

    [LoggerMessage(9, LogLevel.Debug, "Failed to deserialize antiforgery tokens.", EventName = "FailedToDeserialzeTokens")]
    public static partial void FailedToDeserialzeTokens(this ILogger logger, Exception exception);
}

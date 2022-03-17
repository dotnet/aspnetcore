// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging;

internal static partial class LoggingExtensions
{
    [LoggerMessage(1, LogLevel.Trace, "Needs consent: {needsConsent}.", EventName = "NeedsConsent")]
    public static partial void NeedsConsent(this ILogger logger, bool needsConsent);

    [LoggerMessage(2, LogLevel.Trace, "Has consent: {hasConsent}.", EventName = "HasConsent")]
    public static partial void HasConsent(this ILogger logger, bool hasConsent);

    [LoggerMessage(3, LogLevel.Debug, "Consent granted.", EventName = "ConsentGranted")]
    public static partial void ConsentGranted(this ILogger logger);

    [LoggerMessage(4, LogLevel.Debug, "Consent withdrawn.", EventName = "ConsentWithdrawn")]
    public static partial void ConsentWithdrawn(this ILogger logger);

    [LoggerMessage(5, LogLevel.Debug, "Cookie '{key}' suppressed due to consent policy.", EventName = "CookieSuppressed")]
    public static partial void CookieSuppressed(this ILogger logger, string key);

    [LoggerMessage(6, LogLevel.Debug, "Delete cookie '{key}' suppressed due to developer policy.", EventName = "DeleteCookieSuppressed")]
    public static partial void DeleteCookieSuppressed(this ILogger logger, string key);

    [LoggerMessage(7, LogLevel.Debug, "Cookie '{key}' upgraded to 'secure'.", EventName = "UpgradedToSecure")]
    public static partial void CookieUpgradedToSecure(this ILogger logger, string key);

    [LoggerMessage(8, LogLevel.Debug, "Cookie '{key}' same site mode upgraded to '{mode}'.", EventName = "UpgradedSameSite")]
    public static partial void CookieSameSiteUpgraded(this ILogger logger, string key, string mode);

    [LoggerMessage(9, LogLevel.Debug, "Cookie '{key}' upgraded to 'httponly'.", EventName = "UpgradedToHttpOnly")]
    public static partial void CookieUpgradedToHttpOnly(this ILogger logger, string key);
}

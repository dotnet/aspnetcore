// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging;

internal static partial class LoggingExtensions
{
    [LoggerMessage(1, LogLevel.Error, "Error closing the session.", EventName = "ErrorClosingTheSession")]
    public static partial void ErrorClosingTheSession(this ILogger logger, Exception exception);

    [LoggerMessage(2, LogLevel.Information, "Accessing expired session, Key:{sessionKey}", EventName = "AccessingExpiredSession")]
    public static partial void AccessingExpiredSession(this ILogger logger, string sessionKey);

    [LoggerMessage(3, LogLevel.Information, "Session started; Key:{sessionKey}, Id:{sessionId}", EventName = "SessionStarted", SkipEnabledCheck = true)]
    public static partial void SessionStarted(this ILogger logger, string sessionKey, string sessionId);

    [LoggerMessage(4, LogLevel.Debug, "Session loaded; Key:{sessionKey}, Id:{sessionId}, Count:{count}", EventName = "SessionLoaded", SkipEnabledCheck = true)]
    public static partial void SessionLoaded(this ILogger logger, string sessionKey, string sessionId, int count);

    [LoggerMessage(5, LogLevel.Debug, "Session stored; Key:{sessionKey}, Id:{sessionId}, Count:{count}", EventName = "SessionStored")]
    public static partial void SessionStored(this ILogger logger, string sessionKey, string sessionId, int count);

    [LoggerMessage(6, LogLevel.Error, "Session cache read exception, Key:{sessionKey}", EventName = "SessionCacheReadException", SkipEnabledCheck = true)]
    public static partial void SessionCacheReadException(this ILogger logger, string sessionKey, Exception exception);

    [LoggerMessage(7, LogLevel.Warning, "Error unprotecting the session cookie.", EventName = "ErrorUnprotectingCookie")]
    public static partial void ErrorUnprotectingSessionCookie(this ILogger logger, Exception exception);

    [LoggerMessage(8, LogLevel.Warning, "Loading the session timed out.", EventName = "SessionLoadingTimeout")]
    public static partial void SessionLoadingTimeout(this ILogger logger);

    [LoggerMessage(9, LogLevel.Warning, "Committing the session timed out.", EventName = "SessionCommitTimeout")]
    public static partial void SessionCommitTimeout(this ILogger logger);

    [LoggerMessage(10, LogLevel.Information, "Committing the session was canceled.", EventName = "SessionCommitCanceled")]
    public static partial void SessionCommitCanceled(this ILogger logger);

    [LoggerMessage(11, LogLevel.Warning, "Refreshing the session timed out.", EventName = "SessionRefreshTimeout")]
    public static partial void SessionRefreshTimeout(this ILogger logger);

    [LoggerMessage(12, LogLevel.Information, "Refreshing the session was canceled.", EventName = "SessionRefreshCanceled")]
    public static partial void SessionRefreshCanceled(this ILogger logger);

    [LoggerMessage(13, LogLevel.Information, "Session cannot be committed since it is unavailable.", EventName = "SessionCommitNotAvailable")]
    public static partial void SessionNotAvailable(this ILogger logger);
}

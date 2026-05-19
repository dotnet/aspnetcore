// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging;

internal static partial class LoggingExtensions
{
    [LoggerMessage(4, LogLevel.Information, "Error from RemoteAuthentication: {ErrorMessage}.", EventName = "RemoteAuthenticationFailed")]
    public static partial void RemoteAuthenticationError(this ILogger logger, string errorMessage);

    [LoggerMessage(5, LogLevel.Debug, "The SigningIn event returned Handled.", EventName = "SignInHandled")]
    public static partial void SignInHandled(this ILogger logger);

    [LoggerMessage(6, LogLevel.Debug, "The SigningIn event returned Skipped.", EventName = "SignInSkipped")]
    public static partial void SignInSkipped(this ILogger logger);

    [LoggerMessage(7, LogLevel.Information, "{AuthenticationScheme} was not authenticated. Failure message: {FailureMessage}", EventName = "AuthenticationSchemeNotAuthenticatedWithFailure")]
    public static partial void AuthenticationSchemeNotAuthenticatedWithFailure(this ILogger logger, string authenticationScheme, string failureMessage);

    [LoggerMessage(8, LogLevel.Debug, "AuthenticationScheme: {AuthenticationScheme} was successfully authenticated.", EventName = "AuthenticationSchemeAuthenticated")]
    public static partial void AuthenticationSchemeAuthenticated(this ILogger logger, string authenticationScheme);

    [LoggerMessage(9, LogLevel.Debug, "AuthenticationScheme: {AuthenticationScheme} was not authenticated.", EventName = "AuthenticationSchemeNotAuthenticated")]
    public static partial void AuthenticationSchemeNotAuthenticated(this ILogger logger, string authenticationScheme);

    [LoggerMessage(12, LogLevel.Information, "AuthenticationScheme: {AuthenticationScheme} was challenged.", EventName = "AuthenticationSchemeChallenged")]
    public static partial void AuthenticationSchemeChallenged(this ILogger logger, string authenticationScheme);

    [LoggerMessage(13, LogLevel.Information, "AuthenticationScheme: {AuthenticationScheme} was forbidden.", EventName = "AuthenticationSchemeForbidden")]
    public static partial void AuthenticationSchemeForbidden(this ILogger logger, string authenticationScheme);

    [LoggerMessage(14, LogLevel.Warning, "{CorrelationProperty} state property not found.", EventName = "CorrelationPropertyNotFound")]
    public static partial void CorrelationPropertyNotFound(this ILogger logger, string correlationProperty);

    [LoggerMessage(15, LogLevel.Warning, "'{CorrelationCookieName}' cookie not found.", EventName = "CorrelationCookieNotFound")]
    public static partial void CorrelationCookieNotFound(this ILogger logger, string correlationCookieName);

    [LoggerMessage(16, LogLevel.Warning, "The correlation cookie value '{CorrelationCookieName}' did not match the expected value '{CorrelationCookieValue}'.", EventName = "UnexpectedCorrelationCookieValue")]
    public static partial void UnexpectedCorrelationCookieValue(this ILogger logger, string correlationCookieName, string correlationCookieValue);

    [LoggerMessage(17, LogLevel.Information, "Access was denied by the resource owner or by the remote server.", EventName = "AccessDenied")]
    public static partial void AccessDeniedError(this ILogger logger);

    [LoggerMessage(18, LogLevel.Debug, "The AccessDenied event returned Handled.", EventName = "AccessDeniedContextHandled")]
    public static partial void AccessDeniedContextHandled(this ILogger logger);

    [LoggerMessage(19, LogLevel.Debug, "The AccessDenied event returned Skipped.", EventName = "AccessDeniedContextSkipped")]
    public static partial void AccessDeniedContextSkipped(this ILogger logger);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging;

internal static class LoggingExtensions
{
    private static readonly Action<ILogger, Exception> _tokenValidationFailed = LoggerMessage.Define(
        eventId: new EventId(1, "TokenValidationFailed"),
        logLevel: LogLevel.Information,
        formatString: "Failed to validate the token.");
    private static readonly Action<ILogger, Exception?> _tokenValidationSucceeded = LoggerMessage.Define(
            eventId: new EventId(2, "TokenValidationSucceeded"),
            logLevel: LogLevel.Debug,
            formatString: "Successfully validated the token.");
    private static readonly Action<ILogger, Exception> _errorProcessingMessage = LoggerMessage.Define(
            eventId: new EventId(3, "ProcessingMessageFailed"),
            logLevel: LogLevel.Error,
            formatString: "Exception occurred while processing message.");

    public static void TokenValidationFailed(this ILogger logger, Exception ex)
        => _tokenValidationFailed(logger, ex);

    public static void TokenValidationSucceeded(this ILogger logger)
        => _tokenValidationSucceeded(logger, null);

    public static void ErrorProcessingMessage(this ILogger logger, Exception ex)
        => _errorProcessingMessage(logger, ex);
}

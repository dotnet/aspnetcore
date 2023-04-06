// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging;

internal static partial class LoggingExtensions
{
    [LoggerMessage(1, LogLevel.Information, "Failed to validate the token.", EventName = "TokenValidationFailed")]
    public static partial void TokenValidationFailed(this ILogger logger, Exception ex);

    [LoggerMessage(2, LogLevel.Debug, "Successfully validated the token.", EventName = "TokenValidationSucceeded")]
    public static partial void TokenValidationSucceeded(this ILogger logger);

    [LoggerMessage(3, LogLevel.Error, "Exception occurred while processing message.", EventName = "ProcessingMessageFailed")]
    public static partial void ErrorProcessingMessage(this ILogger logger, Exception ex);

    [LoggerMessage(4, LogLevel.Debug, "Unable to reject the response as forbidden, it has already started.", EventName = "ForbiddenResponseHasStarted")]
    public static partial void ForbiddenResponseHasStarted(this ILogger logger);
}

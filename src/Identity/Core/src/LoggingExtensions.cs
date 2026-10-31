// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging;

internal static partial class LoggingExtensions
{
    [LoggerMessage(0, LogLevel.Debug, "ValidateAsync failed: the expiration time is invalid.", EventName = "InvalidExpirationTime")]
    public static partial void InvalidExpirationTime(this ILogger logger);

    [LoggerMessage(1, LogLevel.Debug, "ValidateAsync failed: did not find expected UserId.", EventName = "UserIdsNotEquals")]
    public static partial void UserIdsNotEquals(this ILogger logger);

    [LoggerMessage(2, LogLevel.Debug, "ValidateAsync failed: did not find expected purpose. '{ActualPurpose}' does not match the expected purpose '{ExpectedPurpose}'.", EventName = "PurposeNotEquals")]
    public static partial void PurposeNotEquals(this ILogger logger, string actualPurpose, string expectedPurpose);

    [LoggerMessage(3, LogLevel.Debug, "ValidateAsync failed: unexpected end of input.", EventName = "UnexpectedEndOfInput")]
    public static partial void UnexpectedEndOfInput(this ILogger logger);

    [LoggerMessage(4, LogLevel.Debug, "ValidateAsync failed: did not find expected security stamp.", EventName = "SecurityStampNotEquals")]
    public static partial void SecurityStampNotEquals(this ILogger logger);

    [LoggerMessage(5, LogLevel.Debug, "ValidateAsync failed: the expected stamp is not empty.", EventName = "SecurityStampIsNotEmpty")]
    public static partial void SecurityStampIsNotEmpty(this ILogger logger);

    [LoggerMessage(6, LogLevel.Debug, "ValidateAsync failed: unhandled exception was thrown.", EventName = "UnhandledException")]
    public static partial void UnhandledException(this ILogger logger);
}

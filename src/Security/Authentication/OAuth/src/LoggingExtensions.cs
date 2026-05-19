// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging;

internal static partial class LoggingExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "HandleChallenge with Location: {Location}; and Set-Cookie: {Cookie}.", EventName = "HandleChallenge")]
    public static partial void HandleChallenge(this ILogger logger, string location, string cookie);
}

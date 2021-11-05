// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging;

internal static partial class LoggingExtensions
{

    [LoggerMessage(2, LogLevel.Debug, "ObtainAccessToken", EventName = "ObtainAccessToken")]
    public static partial void ObtainAccessToken(this ILogger logger);

    [LoggerMessage(1, LogLevel.Debug, "ObtainRequestToken", EventName = "ObtainRequestToken")]
    public static partial void ObtainRequestToken(this ILogger logger);

    [LoggerMessage(3, LogLevel.Debug, "RetrieveUserDetails", EventName = "RetrieveUserDetails")]
    public static partial void RetrieveUserDetails(this ILogger logger);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal static partial class RequestContextLog
{
    [LoggerMessage(LoggerEventIds.RequestError, LogLevel.Error, "ProcessRequestAsync", EventName = "RequestError")]
    public static partial void RequestError(ILogger logger, Exception exception);

    [LoggerMessage(LoggerEventIds.RequestProcessError, LogLevel.Error, "ProcessRequestAsync", EventName = "RequestProcessError")]
    public static partial void RequestProcessError(ILogger logger, Exception exception);

    [LoggerMessage(LoggerEventIds.RequestsDrained, LogLevel.Information, "All requests drained.", EventName = "RequestsDrained")]
    public static partial void RequestsDrained(ILogger logger);

    [LoggerMessage(LoggerEventIds.RequestAborted, LogLevel.Debug, "The request was aborted by the client.", EventName = "RequestAborted")]
    public static partial void RequestAborted(ILogger logger);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal partial class RequestContext
{
    private static partial class Log
    {
        [LoggerMessage(LoggerEventIds.AbortError, LogLevel.Debug, "Abort", EventName = "AbortError")]
        public static partial void AbortError(ILogger logger, Exception exception);

        [LoggerMessage(LoggerEventIds.ChannelBindingNeedsHttps, LogLevel.Debug, "TryGetChannelBinding; Channel binding requires HTTPS.", EventName = "ChannelBindingNeedsHttps")]
        public static partial void ChannelBindingNeedsHttps(ILogger logger);

        [LoggerMessage(LoggerEventIds.ChannelBindingRetrieved, LogLevel.Debug, "Channel binding retrieved.", EventName = "ChannelBindingRetrieved")]
        public static partial void ChannelBindingRetrieved(ILogger logger);

        [LoggerMessage(LoggerEventIds.RequestParsingError, LogLevel.Debug, "Failed to parse request.", EventName = "RequestParsingError")]
        public static partial void RequestParsingError(ILogger logger, Exception exception);
    }
}

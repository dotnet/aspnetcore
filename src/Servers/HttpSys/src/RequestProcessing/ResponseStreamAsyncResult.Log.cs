// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal partial class ResponseStreamAsyncResult
{
    private static partial class Log
    {
        [LoggerMessage(LoggerEventIds.WriteCancelled, LogLevel.Debug, "FlushAsync.IOCompleted; Write cancelled with error code: {ErrorCode}", EventName = "WriteCancelled")]
        public static partial void WriteCancelled(ILogger logger, uint errorCode);

        [LoggerMessage(LoggerEventIds.WriteError, LogLevel.Error, "FlushAsync.IOCompleted", EventName = "WriteError")]
        public static partial void WriteError(ILogger logger, Exception exception);

        [LoggerMessage(LoggerEventIds.WriteErrorIgnored, LogLevel.Debug, "FlushAsync.IOCompleted; Ignored write exception: {ErrorCode}", EventName = "WriteErrorIgnored")]
        public static partial void WriteErrorIgnored(ILogger logger, uint errorCode);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal partial class ResponseStreamAsyncResult
    {
        private static class Log
        {
            private static readonly Action<ILogger, uint, Exception?> _writeCancelled =
                LoggerMessage.Define<uint>(LogLevel.Debug, LoggerEventIds.WriteCancelled, "FlushAsync.IOCompleted; Write cancelled with error code: {ErrorCode}");

            private static readonly Action<ILogger, Exception?> _writeError =
                LoggerMessage.Define(LogLevel.Error, LoggerEventIds.WriteError, "FlushAsync.IOCompleted");

            private static readonly Action<ILogger, uint, Exception?> _writeErrorIgnored =
                LoggerMessage.Define<uint>(LogLevel.Debug, LoggerEventIds.WriteErrorIgnored, "FlushAsync.IOCompleted; Ignored write exception: {ErrorCode}");

            public static void WriteCancelled(ILogger logger, uint errorCode)
            {
                _writeCancelled(logger, errorCode, null);
            }

            public static void WriteError(ILogger logger, Exception exception)
            {
                _writeError(logger, exception);
            }

            public static void WriteErrorIgnored(ILogger logger, uint errorCode)
            {
                _writeErrorIgnored(logger, errorCode, null);
            }
        }
    }
}

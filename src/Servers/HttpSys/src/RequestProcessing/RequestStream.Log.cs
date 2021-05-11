// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal partial class RequestStream
    {
        private static class Log
        {
            private static readonly Action<ILogger, Exception?> _errorWhenReadAsync =
                LoggerMessage.Define(LogLevel.Error, LoggerEventIds.ErrorWhenReadAsync, "ReadAsync");

            private static readonly Action<ILogger, Exception?> _errorWhenReadBegun =
                LoggerMessage.Define(LogLevel.Error, LoggerEventIds.ErrorWhenReadBegun, "BeginRead");

            private static readonly Action<ILogger, Exception?> _errorWhileRead =
                LoggerMessage.Define(LogLevel.Error, LoggerEventIds.ErrorWhileRead, "Read");

            public static void ErrorWhenReadAsync(ILogger logger, Exception exception)
            {
                _errorWhenReadAsync(logger, exception);
            }

            public static void ErrorWhenReadBegun(ILogger logger, Exception exception)
            {
                _errorWhenReadBegun(logger, exception);
            }

            public static void ErrorWhileRead(ILogger logger, Exception exception)
            {
                _errorWhileRead(logger, exception);
            }
        }
    }
}

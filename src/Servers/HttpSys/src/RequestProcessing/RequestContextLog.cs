// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal static class RequestContextLog
    {
        private static readonly Action<ILogger, Exception?> _requestError =
            LoggerMessage.Define(LogLevel.Error, LoggerEventIds.RequestError, "ProcessRequestAsync");

        private static readonly Action<ILogger, Exception?> _requestProcessError =
            LoggerMessage.Define(LogLevel.Error, LoggerEventIds.RequestProcessError, "ProcessRequestAsync");

        private static readonly Action<ILogger, Exception?> _requestsDrained =
            LoggerMessage.Define(LogLevel.Information, LoggerEventIds.RequestsDrained, "All requests drained.");

        public static void RequestError(ILogger logger, Exception exception)
        {
            _requestError(logger, exception);
        }

        public static void RequestProcessError(ILogger logger, Exception exception)
        {
            _requestProcessError(logger, exception);
        }

        public static void RequestsDrained(ILogger logger)
        {
            _requestsDrained(logger, null);
        }
    }
}

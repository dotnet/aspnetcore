// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal partial class UrlGroup
    {
        private static class Log
        {
            private static readonly Action<ILogger, uint, Exception?> _closeUrlGroupError =
                LoggerMessage.Define<uint>(LogLevel.Error, LoggerEventIds.CloseUrlGroupError, "HttpCloseUrlGroup; Result: {StatusCode}");

            private static readonly Action<ILogger, string, Exception?> _registeringPrefix =
                LoggerMessage.Define<string>(LogLevel.Debug, LoggerEventIds.RegisteringPrefix, "Listening on prefix: {UriPrefix}");

            private static readonly Action<ILogger, Exception?> _setUrlPropertyError =
                LoggerMessage.Define(LogLevel.Error, LoggerEventIds.SetUrlPropertyError, "SetUrlGroupProperty");

            private static readonly Action<ILogger, string, Exception?> _unregisteringPrefix =
                LoggerMessage.Define<string>(LogLevel.Information, LoggerEventIds.UnregisteringPrefix, "Stop listening on prefix: {UriPrefix}");

            public static void CloseUrlGroupError(ILogger logger, uint statusCode)
            {
                _closeUrlGroupError(logger, statusCode, null);
            }

            public static void RegisteringPrefix(ILogger logger, string uriPrefix)
            {
                _registeringPrefix(logger, uriPrefix, null);
            }

            public static void SetUrlPropertyError(ILogger logger, Exception exception)
            {
                _setUrlPropertyError(logger, exception);
            }

            public static void UnregisteringPrefix(ILogger logger, string uriPrefix)
            {
                _unregisteringPrefix(logger, uriPrefix, null);
            }
        }
    }
}

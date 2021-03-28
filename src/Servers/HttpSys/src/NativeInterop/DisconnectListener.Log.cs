// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal partial class DisconnectListener
    {
        private static class Log
        {
            private static readonly Action<ILogger, Exception?> _disconnectHandlerError =
                LoggerMessage.Define(LogLevel.Error, LoggerEventIds.DisconnectHandlerError, "CreateDisconnectToken Callback");

            private static readonly Action<ILogger, Exception?> _disconnectRegistrationCreateDisconnectTokenError =
                LoggerMessage.Define(LogLevel.Error, LoggerEventIds.DisconnectRegistrationError, "CreateDisconnectToken");

            private static readonly Action<ILogger, Exception?> _disconnectRegistrationError =
                LoggerMessage.Define(LogLevel.Error, LoggerEventIds.DisconnectRegistrationError, "Unable to register for disconnect notifications.");

            private static readonly Action<ILogger, ulong, Exception?> _disconnectTriggered =
                LoggerMessage.Define<ulong>(LogLevel.Debug, LoggerEventIds.DisconnectTriggered, "CreateDisconnectToken; http.sys disconnect callback fired for connection ID: {ConnectionId}");

            private static readonly Action<ILogger, ulong, Exception?> _registerDisconnectListener =
                LoggerMessage.Define<ulong>(LogLevel.Debug, LoggerEventIds.RegisterDisconnectListener, "CreateDisconnectToken; Registering connection for disconnect for connection ID: {ConnectionId}");

            private static readonly Action<ILogger, Exception?> _unknownDisconnectError =
                LoggerMessage.Define(LogLevel.Debug, LoggerEventIds.UnknownDisconnectError, "HttpWaitForDisconnectEx");

            public static void DisconnectHandlerError(ILogger logger, Exception exception)
            {
                _disconnectHandlerError(logger, exception);
            }

            public static void DisconnectRegistrationError(ILogger logger, Exception exception)
            {
                _disconnectRegistrationError(logger, exception);
            }

            public static void DisconnectRegistrationCreateDisconnectTokenError(ILogger logger, Exception exception)
            {
                _disconnectRegistrationCreateDisconnectTokenError(logger, exception);
            }

            public static void DisconnectTriggered(ILogger logger, ulong connectionId)
            {
                _disconnectTriggered(logger, connectionId, null);
            }

            public static void RegisterDisconnectListener(ILogger logger, ulong connectionId)
            {
                _registerDisconnectListener(logger, connectionId, null);
            }

            public static void UnknownDisconnectError(ILogger logger, Exception exception)
            {
                _unknownDisconnectError(logger, exception);
            }
        }
    }
}

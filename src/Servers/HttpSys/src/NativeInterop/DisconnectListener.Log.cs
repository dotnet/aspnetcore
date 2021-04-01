// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            private static readonly Action<ILogger, Exception?> _createDisconnectTokenError =
                LoggerMessage.Define(LogLevel.Error, LoggerEventIds.CreateDisconnectTokenError, "CreateDisconnectToken");

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

            public static void CreateDisconnectTokenError(ILogger logger, Exception exception)
            {
                _createDisconnectTokenError(logger, exception);
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

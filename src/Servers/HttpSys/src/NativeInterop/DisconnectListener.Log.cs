// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal partial class DisconnectListener
{
    private static partial class Log
    {
        [LoggerMessage(LoggerEventIds.DisconnectHandlerError, LogLevel.Error, "CreateDisconnectToken Callback", EventName = "DisconnectHandlerError")]
        public static partial void DisconnectHandlerError(ILogger logger, Exception exception);

        [LoggerMessage(LoggerEventIds.DisconnectRegistrationError, LogLevel.Error, "Unable to register for disconnect notifications.", EventName = "DisconnectRegistrationError")]
        public static partial void DisconnectRegistrationError(ILogger logger, Exception exception);

        [LoggerMessage(LoggerEventIds.CreateDisconnectTokenError, LogLevel.Error, "CreateDisconnectToken", EventName = "CreateDisconnectTokenError")]
        public static partial void CreateDisconnectTokenError(ILogger logger, Exception exception);

        [LoggerMessage(LoggerEventIds.DisconnectTriggered, LogLevel.Debug, "CreateDisconnectToken; http.sys disconnect callback fired for connection ID: {ConnectionId}", EventName = "DisconnectTriggered")]
        public static partial void DisconnectTriggered(ILogger logger, ulong connectionId);

        [LoggerMessage(LoggerEventIds.RegisterDisconnectListener, LogLevel.Debug, "CreateDisconnectToken; Registering connection for disconnect for connection ID: {ConnectionId}", EventName = "RegisterDisconnectListener")]
        public static partial void RegisterDisconnectListener(ILogger logger, ulong connectionId);

        [LoggerMessage(LoggerEventIds.UnknownDisconnectError, LogLevel.Debug, "HttpWaitForDisconnectEx", EventName = "UnknownDisconnectError")]
        public static partial void UnknownDisconnectError(ILogger logger, Exception exception);
    }
}

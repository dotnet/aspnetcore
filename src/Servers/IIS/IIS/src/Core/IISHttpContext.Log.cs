// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IIS.Core
{
    internal abstract partial class IISHttpContext
    {
        private static class Log
        {
            private static readonly Action<ILogger, string, Exception?> _connectionDisconnect =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, "ConnectionDisconnect"), @"Connection ID ""{ConnectionId}"" disconnecting.");

            private static readonly Action<ILogger, string, string, Exception> _applicationError =
                LoggerMessage.Define<string, string>(LogLevel.Error, new EventId(2, "ApplicationError"), @"Connection ID ""{ConnectionId}"", Request ID ""{TraceIdentifier}"": An unhandled exception was thrown by the application.");

            private static readonly Action<ILogger, string, string?, Exception> _unexpectedError =
                LoggerMessage.Define<string, string?>(LogLevel.Error, new EventId(3, "UnexpectedError"), @"Unexpected exception in ""{ClassName}.{MethodName}"".");

            private static readonly Action<ILogger, string, string, Exception> _connectionBadRequest =
                LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(4, nameof(ConnectionBadRequest)), @"Connection id ""{ConnectionId}"" bad request data: ""{message}""");

            public static void ConnectionDisconnect(ILogger logger, string connectionId)
            {
                _connectionDisconnect(logger, connectionId, null);
            }

            public static void ApplicationError(ILogger logger, string connectionId, string traceIdentifier, Exception ex)
            {
                _applicationError(logger, connectionId, traceIdentifier, ex);
            }

            public static void UnexpectedError(ILogger logger, string className, Exception ex, [CallerMemberName] string? methodName = null)
            {
                _unexpectedError(logger, className, methodName, ex);
            }

            public static void ConnectionBadRequest(ILogger logger, string connectionId, Microsoft.AspNetCore.Http.BadHttpRequestException ex)
            {
                _connectionBadRequest(logger, connectionId, ex.Message, ex);
            }
        }
    }
}

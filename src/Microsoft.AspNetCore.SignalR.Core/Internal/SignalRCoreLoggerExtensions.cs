// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Core.Internal
{
    internal static class SignalRCoreLoggerExtensions
    {
        // Category: HubEndPoint<THub>
        private static readonly Action<ILogger, string, Exception> _usingHubProtocol =
            LoggerMessage.Define<string>(LogLevel.Information, 0, "Using HubProtocol '{protocol}'.");

        private static readonly Action<ILogger, Exception> _negotiateCanceled =
            LoggerMessage.Define(LogLevel.Debug, 1, "Negotiate was canceled.");

        private static readonly Action<ILogger, Exception> _errorProcessingRequest =
            LoggerMessage.Define(LogLevel.Error, 2, "Error when processing requests.");

        private static readonly Action<ILogger, string, Exception> _errorInvokingHubMethod =
            LoggerMessage.Define<string>(LogLevel.Error, 3, "Error when invoking '{hubMethod}' on hub.");

        private static readonly Action<ILogger, InvocationMessage, Exception> _receivedHubInvocation =
            LoggerMessage.Define<InvocationMessage>(LogLevel.Debug, 4, "Received hub invocation: {invocationMessage}.");

        private static readonly Action<ILogger, string, Exception> _unsupportedMessageReceived =
            LoggerMessage.Define<string>(LogLevel.Error, 5, "Received unsupported message of type '{messageType}'.");

        private static readonly Action<ILogger, string, Exception> _unknownHubMethod =
            LoggerMessage.Define<string>(LogLevel.Error, 6, "Unknown hub method '{hubMethod}'.");

        private static readonly Action<ILogger, Exception> _outboundChannelClosed =
            LoggerMessage.Define(LogLevel.Warning, 7, "Outbound channel was closed while trying to write hub message.");

        private static readonly Action<ILogger, string, Exception> _hubMethodNotAuthorized =
            LoggerMessage.Define<string>(LogLevel.Debug, 8, "Failed to invoke '{hubMethod}' because user is unauthorized.");

        private static readonly Action<ILogger, string, string, Exception> _streamingResult =
            LoggerMessage.Define<string, string>(LogLevel.Trace, 9, "{invocationId}: Streaming result of type '{resultType}'.");

        private static readonly Action<ILogger, string, string, Exception> _sendingResult =
            LoggerMessage.Define<string, string>(LogLevel.Trace, 10, "{invocationId}: Sending result of type '{resultType}'.");

        private static readonly Action<ILogger, string, Exception> _failedInvokingHubMethod =
            LoggerMessage.Define<string>(LogLevel.Error, 11, "Failed to invoke hub method '{hubMethod}'.");

        private static readonly Action<ILogger, string, Exception> _hubMethodBound =
            LoggerMessage.Define<string>(LogLevel.Trace, 12, "Hub method '{hubMethod}' is bound.");

        public static void UsingHubProtocol(this ILogger logger, string hubProtocol)
        {
            _usingHubProtocol(logger, hubProtocol, null);
        }

        public static void NegotiateCanceled(this ILogger logger)
        {
            _negotiateCanceled(logger, null);
        }

        public static void ErrorProcessingRequest(this ILogger logger, Exception exception)
        {
            _errorProcessingRequest(logger, exception);
        }

        public static void ErrorInvokingHubMethod(this ILogger logger, string hubMethod, Exception exception)
        {
            _errorInvokingHubMethod(logger, hubMethod, exception);
        }

        public static void ReceivedHubInvocation(this ILogger logger, InvocationMessage invocationMessage)
        {
            _receivedHubInvocation(logger, invocationMessage, null);
        }

        public static void UnsupportedMessageReceived(this ILogger logger, string messageType)
        {
            _unsupportedMessageReceived(logger, messageType, null);
        }

        public static void UnknownHubMethod(this ILogger logger, string hubMethod)
        {
            _unknownHubMethod(logger, hubMethod, null);
        }

        public static void OutboundChannelClosed(this ILogger logger)
        {
            _outboundChannelClosed(logger, null);
        }

        public static void HubMethodNotAuthorized(this ILogger logger, string hubMethod)
        {
            _hubMethodNotAuthorized(logger, hubMethod, null);
        }

        public static void StreamingResult(this ILogger logger, string invocationId, string resultType)
        {
            _streamingResult(logger, invocationId, resultType, null);
        }

        public static void SendingResult(this ILogger logger, string invocationId, string resultType)
        {
            _sendingResult(logger, invocationId, resultType, null);
        }

        public static void FailedInvokingHubMethod(this ILogger logger, string hubMethod, Exception exception)
        {
            _failedInvokingHubMethod(logger, hubMethod, exception);
        }

        public static void HubMethodBound(this ILogger logger, string hubMethod)
        {
            _hubMethodBound(logger, hubMethod, null);
        }
    }
}
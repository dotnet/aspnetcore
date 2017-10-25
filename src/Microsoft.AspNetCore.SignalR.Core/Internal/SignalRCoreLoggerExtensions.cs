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
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(0, nameof(UsingHubProtocol)), "Using HubProtocol '{protocol}'.");

        private static readonly Action<ILogger, Exception> _negotiateCanceled =
            LoggerMessage.Define(LogLevel.Debug, new EventId(1, nameof(NegotiateCanceled)), "Negotiate was canceled.");

        private static readonly Action<ILogger, Exception> _errorProcessingRequest =
            LoggerMessage.Define(LogLevel.Error, new EventId(2, nameof(ErrorProcessingRequest)), "Error when processing requests.");

        private static readonly Action<ILogger, string, Exception> _errorInvokingHubMethod =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(3, nameof(ErrorInvokingHubMethod)), "Error when invoking '{hubMethod}' on hub.");

        private static readonly Action<ILogger, InvocationMessage, Exception> _receivedHubInvocation =
            LoggerMessage.Define<InvocationMessage>(LogLevel.Debug, new EventId(4, nameof(ReceivedHubInvocation)), "Received hub invocation: {invocationMessage}.");

        private static readonly Action<ILogger, string, Exception> _unsupportedMessageReceived =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(5, nameof(UnsupportedMessageReceived)), "Received unsupported message of type '{messageType}'.");

        private static readonly Action<ILogger, string, Exception> _unknownHubMethod =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(6, nameof(UnknownHubMethod)), "Unknown hub method '{hubMethod}'.");

        private static readonly Action<ILogger, Exception> _outboundChannelClosed =
            LoggerMessage.Define(LogLevel.Warning, new EventId(7, nameof(OutboundChannelClosed)), "Outbound channel was closed while trying to write hub message.");

        private static readonly Action<ILogger, string, Exception> _hubMethodNotAuthorized =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(8, nameof(HubMethodNotAuthorized)), "Failed to invoke '{hubMethod}' because user is unauthorized.");

        private static readonly Action<ILogger, string, string, Exception> _streamingResult =
            LoggerMessage.Define<string, string>(LogLevel.Trace, new EventId(9, nameof(StreamingResult)), "{invocationId}: Streaming result of type '{resultType}'.");

        private static readonly Action<ILogger, string, string, Exception> _sendingResult =
            LoggerMessage.Define<string, string>(LogLevel.Trace, new EventId(10, nameof(SendingResult)), "{invocationId}: Sending result of type '{resultType}'.");

        private static readonly Action<ILogger, string, Exception> _failedInvokingHubMethod =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(11, nameof(FailedInvokingHubMethod)), "Failed to invoke hub method '{hubMethod}'.");

        private static readonly Action<ILogger, string, Exception> _hubMethodBound =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(12, nameof(HubMethodBound)), "Hub method '{hubMethod}' is bound.");

        private static readonly Action<ILogger, string, Exception> _cancelStream =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(13, nameof(CancelStream)), "Canceling stream for invocation {invocationId}.");

        private static readonly Action<ILogger, Exception> _unexpectedCancel =
            LoggerMessage.Define(LogLevel.Debug, new EventId(14, nameof(UnexpectedCancel)), "CancelInvocationMessage received unexpectedly.");

        private static readonly Action<ILogger, Exception> _abortFailed =
            LoggerMessage.Define(LogLevel.Trace, new EventId(15, nameof(AbortFailed)), "Abort callback failed.");

        private static readonly Action<ILogger, StreamInvocationMessage, Exception> _receivedStreamHubInvocation =
            LoggerMessage.Define<StreamInvocationMessage>(LogLevel.Debug, new EventId(16, nameof(ReceivedStreamHubInvocation)), "Received stream hub invocation: {invocationMessage}.");

        private static readonly Action<ILogger, HubMethodInvocationMessage, Exception> _streamingMethodCalledWithInvoke =
            LoggerMessage.Define<HubMethodInvocationMessage>(LogLevel.Error, new EventId(17, nameof(StreamingMethodCalledWithInvoke)), "A streaming method was invoked in the non-streaming fashion : {invocationMessage}.");

        private static readonly Action<ILogger, HubMethodInvocationMessage, Exception> _nonStreamingMethodCalledWithStream =
            LoggerMessage.Define<HubMethodInvocationMessage>(LogLevel.Error, new EventId(18, nameof(NonStreamingMethodCalledWithStream)), "A non-streaming method was invoked in the streaming fashion : {invocationMessage}.");

        private static readonly Action<ILogger, string, Exception> _invalidReturnValueFromStreamingMethod =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(19, nameof(InvalidReturnValueFromStreamingMethod)), "A streaming method returned a value that cannot be used to build enumerator {hubMethod}.");

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

        public static void CancelStream(this ILogger logger, string invocationId)
        {
            _cancelStream(logger, invocationId, null);
        }

        public static void UnexpectedCancel(this ILogger logger)
        {
            _unexpectedCancel(logger, null);
        }

        public static void AbortFailed(this ILogger logger, Exception exception)
        {
            _abortFailed(logger, exception);
        }

        public static void ReceivedStreamHubInvocation(this ILogger logger, StreamInvocationMessage invocationMessage)
        {
            _receivedStreamHubInvocation(logger, invocationMessage, null);
        }

        public static void StreamingMethodCalledWithInvoke(this ILogger logger, HubMethodInvocationMessage invocationMessage)
        {
            _streamingMethodCalledWithInvoke(logger, invocationMessage, null);
        }

        public static void NonStreamingMethodCalledWithStream(this ILogger logger, HubMethodInvocationMessage invocationMessage)
        {
            _nonStreamingMethodCalledWithStream(logger, invocationMessage, null);
        }

        public static void InvalidReturnValueFromStreamingMethod(this ILogger logger, string hubMethod)
        {
            _invalidReturnValueFromStreamingMethod(logger, hubMethod, null);
        }
    }
}

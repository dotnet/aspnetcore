// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public partial class HubConnection
    {
        private static class Log
        {
            private static readonly Action<ILogger, string, int, Exception> _preparingNonBlockingInvocation =
            LoggerMessage.Define<string, int>(LogLevel.Trace, new EventId(1, "PreparingNonBlockingInvocation"), "Preparing non-blocking invocation of '{Target}', with {ArgumentCount} argument(s).");

            private static readonly Action<ILogger, string, string, string, int, Exception> _preparingBlockingInvocation =
                LoggerMessage.Define<string, string, string, int>(LogLevel.Trace, new EventId(2, "PreparingBlockingInvocation"), "Preparing blocking invocation '{InvocationId}' of '{Target}', with return type '{ReturnType}' and {ArgumentCount} argument(s).");

            private static readonly Action<ILogger, string, Exception> _registerInvocation =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(3, "RegisterInvocation"), "Registering Invocation ID '{InvocationId}' for tracking.");

            private static readonly Action<ILogger, string, string, string, string, Exception> _issueInvocation =
                LoggerMessage.Define<string, string, string, string>(LogLevel.Trace, new EventId(4, "IssueInvocation"), "Issuing Invocation '{InvocationId}': {ReturnType} {MethodName}({Args}).");

            private static readonly Action<ILogger, string, Exception> _sendInvocation =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(5, "SendInvocation"), "Sending Invocation '{InvocationId}'.");

            private static readonly Action<ILogger, string, Exception> _sendInvocationCompleted =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(6, "SendInvocationCompleted"), "Sending Invocation '{InvocationId}' completed.");

            private static readonly Action<ILogger, string, Exception> _sendInvocationFailed =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(7, "SendInvocationFailed"), "Sending Invocation '{InvocationId}' failed.");

            private static readonly Action<ILogger, string, string, string, Exception> _receivedInvocation =
                LoggerMessage.Define<string, string, string>(LogLevel.Trace, new EventId(8, "ReceivedInvocation"), "Received Invocation '{InvocationId}': {MethodName}({Args}).");

            private static readonly Action<ILogger, string, Exception> _dropCompletionMessage =
                LoggerMessage.Define<string>(LogLevel.Warning, new EventId(9, "DropCompletionMessage"), "Dropped unsolicited Completion message for invocation '{InvocationId}'.");

            private static readonly Action<ILogger, string, Exception> _dropStreamMessage =
                LoggerMessage.Define<string>(LogLevel.Warning, new EventId(10, "DropStreamMessage"), "Dropped unsolicited StreamItem message for invocation '{InvocationId}'.");

            private static readonly Action<ILogger, Exception> _shutdownConnection =
                LoggerMessage.Define(LogLevel.Trace, new EventId(11, "ShutdownConnection"), "Shutting down connection.");

            private static readonly Action<ILogger, Exception> _shutdownWithError =
                LoggerMessage.Define(LogLevel.Error, new EventId(12, "ShutdownWithError"), "Connection is shutting down due to an error.");

            private static readonly Action<ILogger, string, Exception> _removeInvocation =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(13, "RemoveInvocation"), "Removing pending invocation {InvocationId}.");

            private static readonly Action<ILogger, string, Exception> _missingHandler =
                LoggerMessage.Define<string>(LogLevel.Warning, new EventId(14, "MissingHandler"), "Failed to find handler for '{Target}' method.");

            private static readonly Action<ILogger, string, Exception> _receivedStreamItem =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(15, "ReceivedStreamItem"), "Received StreamItem for Invocation {InvocationId}.");

            private static readonly Action<ILogger, string, Exception> _cancelingStreamItem =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(16, "CancelingStreamItem"), "Canceling dispatch of StreamItem message for Invocation {InvocationId}. The invocation was canceled.");

            private static readonly Action<ILogger, string, Exception> _receivedStreamItemAfterClose =
                LoggerMessage.Define<string>(LogLevel.Warning, new EventId(17, "ReceivedStreamItemAfterClose"), "Invocation {InvocationId} received stream item after channel was closed.");

            private static readonly Action<ILogger, string, Exception> _receivedInvocationCompletion =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(18, "ReceivedInvocationCompletion"), "Received Completion for Invocation {InvocationId}.");

            private static readonly Action<ILogger, string, Exception> _cancelingInvocationCompletion =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(19, "CancelingInvocationCompletion"), "Canceling dispatch of Completion message for Invocation {InvocationId}. The invocation was canceled.");

            private static readonly Action<ILogger, string, Exception> _cancelingCompletion =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(20, "CancelingCompletion"), "Canceling dispatch of Completion message for Invocation {InvocationId}. The invocation was canceled.");

            private static readonly Action<ILogger, string, Exception> _invokeAfterTermination =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(21, "InvokeAfterTermination"), "Invoke for Invocation '{InvocationId}' was called after the connection was terminated.");

            private static readonly Action<ILogger, string, Exception> _invocationAlreadyInUse =
                LoggerMessage.Define<string>(LogLevel.Critical, new EventId(22, "InvocationAlreadyInUse"), "Invocation ID '{InvocationId}' is already in use.");

            private static readonly Action<ILogger, string, Exception> _receivedUnexpectedResponse =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(23, "ReceivedUnexpectedResponse"), "Unsolicited response received for invocation '{InvocationId}'.");

            private static readonly Action<ILogger, string, Exception> _hubProtocol =
                LoggerMessage.Define<string>(LogLevel.Information, new EventId(24, "HubProtocol"), "Using HubProtocol '{Protocol}'.");

            private static readonly Action<ILogger, string, string, string, int, Exception> _preparingStreamingInvocation =
                LoggerMessage.Define<string, string, string, int>(LogLevel.Trace, new EventId(25, "PreparingStreamingInvocation"), "Preparing streaming invocation '{InvocationId}' of '{Target}', with return type '{ReturnType}' and {ArgumentCount} argument(s).");

            private static readonly Action<ILogger, Exception> _resettingKeepAliveTimer =
                LoggerMessage.Define(LogLevel.Trace, new EventId(26, "ResettingKeepAliveTimer"), "Resetting keep-alive timer, received a message from the server.");

            private static readonly Action<ILogger, Exception> _errorDuringClosedEvent =
                LoggerMessage.Define(LogLevel.Error, new EventId(27, "ErrorDuringClosedEvent"), "An exception was thrown in the handler for the Closed event.");

            private static readonly Action<ILogger, Exception> _sendingHubHandshake =
                LoggerMessage.Define(LogLevel.Debug, new EventId(28, "SendingHubHandshake"), "Sending Hub Handshake.");

            private static readonly Action<ILogger, int, Exception> _parsingMessages =
                LoggerMessage.Define<int>(LogLevel.Debug, new EventId(29, "ParsingMessages"), "Received {Count} bytes. Parsing message(s).");

            private static readonly Action<ILogger, int, Exception> _receivingMessages =
                LoggerMessage.Define<int>(LogLevel.Debug, new EventId(30, "ReceivingMessages"), "Received {MessageCount} message(s).");

            private static readonly Action<ILogger, Exception> _receivedPing =
                LoggerMessage.Define(LogLevel.Trace, new EventId(31, "ReceivedPing"), "Received a ping message.");

            private static readonly Action<ILogger, int, Exception> _processedMessages =
                LoggerMessage.Define<int>(LogLevel.Debug, new EventId(32, "ProcessedMessages"), "Finished processing {MessageCount} message(s).");

            private static readonly Action<ILogger, int, Exception> _failedParsing =
                LoggerMessage.Define<int>(LogLevel.Warning, new EventId(33, "FailedParsing"), "No messages parsed from {Count} byte(s).");

            private static readonly Action<ILogger, string, Exception> _errorInvokingClientSideMethod =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(34, "ErrorInvokingClientSideMethod"), "Invoking client side method '{MethodName}' failed.");

            private static readonly Action<ILogger, Exception> _errorProcessingHandshakeResponse =
                LoggerMessage.Define(LogLevel.Error, new EventId(35, "ErrorReceivingHandshakeResponse"), "Error processing the handshake response.");

            private static readonly Action<ILogger, string, Exception> _handshakeServerError =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(36, "HandshakeServerError"), "Server returned handshake error: {Error}");

            private static readonly Action<ILogger, Exception> _receivedClose =
                LoggerMessage.Define(LogLevel.Debug, new EventId(37, "ReceivedClose"), "Received close message.");

            private static readonly Action<ILogger, string, Exception> _receivedCloseWithError =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(38, "ReceivedCloseWithError"), "Received close message with an error: {Error}");

            public static void PreparingNonBlockingInvocation(ILogger logger, string target, int count)
            {
                _preparingNonBlockingInvocation(logger, target, count, null);
            }

            public static void PreparingBlockingInvocation(ILogger logger, string invocationId, string target, string returnType, int count)
            {
                _preparingBlockingInvocation(logger, invocationId, target, returnType, count, null);
            }

            public static void PreparingStreamingInvocation(ILogger logger, string invocationId, string target, string returnType, int count)
            {
                _preparingStreamingInvocation(logger, invocationId, target, returnType, count, null);
            }

            public static void RegisterInvocation(ILogger logger, string invocationId)
            {
                _registerInvocation(logger, invocationId, null);
            }

            public static void IssueInvocation(ILogger logger, string invocationId, string returnType, string methodName, object[] args)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    var argsList = args == null ? string.Empty : string.Join(", ", args.Select(a => a?.GetType().FullName ?? "(null)"));
                    _issueInvocation(logger, invocationId, returnType, methodName, argsList, null);
                }
            }

            public static void SendInvocation(ILogger logger, string invocationId)
            {
                _sendInvocation(logger, invocationId, null);
            }

            public static void SendInvocationCompleted(ILogger logger, string invocationId)
            {
                _sendInvocationCompleted(logger, invocationId, null);
            }

            public static void SendInvocationFailed(ILogger logger, string invocationId, Exception exception)
            {
                _sendInvocationFailed(logger, invocationId, exception);
            }

            public static void ReceivedInvocation(ILogger logger, string invocationId, string methodName, object[] args)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    var argsList = args == null ? string.Empty : string.Join(", ", args.Select(a => a?.GetType().FullName ?? "(null)"));
                    _receivedInvocation(logger, invocationId, methodName, argsList, null);
                }
            }

            public static void DropCompletionMessage(ILogger logger, string invocationId)
            {
                _dropCompletionMessage(logger, invocationId, null);
            }

            public static void DropStreamMessage(ILogger logger, string invocationId)
            {
                _dropStreamMessage(logger, invocationId, null);
            }

            public static void ShutdownConnection(ILogger logger)
            {
                _shutdownConnection(logger, null);
            }

            public static void ShutdownWithError(ILogger logger, Exception exception)
            {
                _shutdownWithError(logger, exception);
            }

            public static void RemoveInvocation(ILogger logger, string invocationId)
            {
                _removeInvocation(logger, invocationId, null);
            }

            public static void MissingHandler(ILogger logger, string target)
            {
                _missingHandler(logger, target, null);
            }

            public static void ReceivedStreamItem(ILogger logger, string invocationId)
            {
                _receivedStreamItem(logger, invocationId, null);
            }

            public static void CancelingStreamItem(ILogger logger, string invocationId)
            {
                _cancelingStreamItem(logger, invocationId, null);
            }

            public static void ReceivedStreamItemAfterClose(ILogger logger, string invocationId)
            {
                _receivedStreamItemAfterClose(logger, invocationId, null);
            }

            public static void ReceivedInvocationCompletion(ILogger logger, string invocationId)
            {
                _receivedInvocationCompletion(logger, invocationId, null);
            }

            public static void CancelingInvocationCompletion(ILogger logger, string invocationId)
            {
                _cancelingInvocationCompletion(logger, invocationId, null);
            }

            public static void CancelingCompletion(ILogger logger, string invocationId)
            {
                _cancelingCompletion(logger, invocationId, null);
            }

            public static void InvokeAfterTermination(ILogger logger, string invocationId)
            {
                _invokeAfterTermination(logger, invocationId, null);
            }

            public static void InvocationAlreadyInUse(ILogger logger, string invocationId)
            {
                _invocationAlreadyInUse(logger, invocationId, null);
            }

            public static void ReceivedUnexpectedResponse(ILogger logger, string invocationId)
            {
                _receivedUnexpectedResponse(logger, invocationId, null);
            }

            public static void HubProtocol(ILogger logger, string hubProtocol)
            {
                _hubProtocol(logger, hubProtocol, null);
            }

            public static void ResettingKeepAliveTimer(ILogger logger)
            {
                _resettingKeepAliveTimer(logger, null);
            }

            public static void ErrorDuringClosedEvent(ILogger logger, Exception exception)
            {
                _errorDuringClosedEvent(logger, exception);
            }

            public static void SendingHubHandshake(ILogger logger)
            {
                _sendingHubHandshake(logger, null);
            }

            public static void ParsingMessages(ILogger logger, int byteCount)
            {
                _parsingMessages(logger, byteCount, null);
            }

            public static void ReceivingMessages(ILogger logger, int messageCount)
            {
                _receivingMessages(logger, messageCount, null);
            }

            public static void ReceivedPing(ILogger logger)
            {
                _receivedPing(logger, null);
            }

            public static void ProcessedMessages(ILogger logger, int messageCount)
            {
                _processedMessages(logger, messageCount, null);
            }

            public static void FailedParsing(ILogger logger, int byteCount)
            {
                _failedParsing(logger, byteCount, null);
            }

            public static void ErrorInvokingClientSideMethod(ILogger logger, string methodName, Exception exception)
            {
                _errorInvokingClientSideMethod(logger, methodName, exception);
            }

            public static void ErrorReceivingHandshakeResponse(ILogger logger, Exception exception)
            {
                _errorProcessingHandshakeResponse(logger, exception);
            }

            public static void HandshakeServerError(ILogger logger, string error)
            {
                _handshakeServerError(logger, error, null);
            }

            public static void ReceivedClose(ILogger logger)
            {
                _receivedClose(logger, null);
            }

            public static void ReceivedCloseWithError(ILogger logger, string error)
            {
                _receivedCloseWithError(logger, error, null);
            }
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client.Internal
{
    internal static class SignalRClientLoggerExtensions
    {
        // Category: HubConnection
        private static readonly Action<ILogger, string, string, int, Exception> _preparingNonBlockingInvocation =
            LoggerMessage.Define<string, string, int>(LogLevel.Trace, new EventId(0, nameof(PreparingNonBlockingInvocation)), "Preparing non-blocking invocation '{invocationId}' of '{target}', with {argumentCount} argument(s).");

        private static readonly Action<ILogger, string, string, string, int, Exception> _preparingBlockingInvocation =
            LoggerMessage.Define<string, string, string, int>(LogLevel.Trace, new EventId(1, nameof(PreparingBlockingInvocation)), "Preparing blocking invocation '{invocationId}' of '{target}', with return type '{returnType}' and {argumentCount} argument(s).");

        private static readonly Action<ILogger, string, Exception> _registerInvocation =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(2, nameof(RegisterInvocation)), "Registering Invocation ID '{invocationId}' for tracking.");

        private static readonly Action<ILogger, string, string, string, string, Exception> _issueInvocation =
            LoggerMessage.Define<string, string, string, string>(LogLevel.Trace, new EventId(3, nameof(IssueInvocation)), "Issuing Invocation '{invocationId}': {returnType} {methodName}({args}).");

        private static readonly Action<ILogger, string, Exception> _sendInvocation =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(4, nameof(SendInvocation)), "Sending Invocation '{invocationId}'.");

        private static readonly Action<ILogger, string, Exception> _sendInvocationCompleted =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(5, nameof(SendInvocationCompleted)), "Sending Invocation '{invocationId}' completed.");

        private static readonly Action<ILogger, string, Exception> _sendInvocationFailed =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(6, nameof(SendInvocationFailed)), "Sending Invocation '{invocationId}' failed.");

        private static readonly Action<ILogger, string, string, string, Exception> _receivedInvocation =
            LoggerMessage.Define<string, string, string>(LogLevel.Trace, new EventId(7, nameof(ReceivedInvocation)), "Received Invocation '{invocationId}': {methodName}({args}).");

        private static readonly Action<ILogger, string, Exception> _dropCompletionMessage =
            LoggerMessage.Define<string>(LogLevel.Warning, new EventId(8, nameof(DropCompletionMessage)), "Dropped unsolicited Completion message for invocation '{invocationId}'.");

        private static readonly Action<ILogger, string, Exception> _dropStreamMessage =
            LoggerMessage.Define<string>(LogLevel.Warning, new EventId(9, nameof(DropStreamMessage)), "Dropped unsolicited StreamItem message for invocation '{invocationId}'.");

        private static readonly Action<ILogger, Exception> _shutdownConnection =
            LoggerMessage.Define(LogLevel.Trace, new EventId(10, nameof(ShutdownConnection)), "Shutting down connection.");

        private static readonly Action<ILogger, Exception> _shutdownWithError =
            LoggerMessage.Define(LogLevel.Error, new EventId(11, nameof(ShutdownWithError)), "Connection is shutting down due to an error.");

        private static readonly Action<ILogger, string, Exception> _removeInvocation =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(12, nameof(RemoveInvocation)), "Removing pending invocation {invocationId}.");

        private static readonly Action<ILogger, string, Exception> _missingHandler =
            LoggerMessage.Define<string>(LogLevel.Warning, new EventId(13, nameof(MissingHandler)), "Failed to find handler for '{target}' method.");

        private static readonly Action<ILogger, string, Exception> _receivedStreamItem =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(14, nameof(ReceivedStreamItem)), "Received StreamItem for Invocation {invocationId}.");

        private static readonly Action<ILogger, string, Exception> _cancelingStreamItem =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(15, nameof(CancelingStreamItem)), "Canceling dispatch of StreamItem message for Invocation {invocationId}. The invocation was canceled.");

        private static readonly Action<ILogger, string, Exception> _receivedStreamItemAfterClose =
            LoggerMessage.Define<string>(LogLevel.Warning, new EventId(16, nameof(ReceivedStreamItemAfterClose)), "Invocation {invocationId} received stream item after channel was closed.");

        private static readonly Action<ILogger, string, Exception> _receivedInvocationCompletion =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(17, nameof(ReceivedInvocationCompletion)), "Received Completion for Invocation {invocationId}.");

        private static readonly Action<ILogger, string, Exception> _cancelingInvocationCompletion =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(18, nameof(CancelingInvocationCompletion)), "Canceling dispatch of Completion message for Invocation {invocationId}. The invocation was canceled.");

        private static readonly Action<ILogger, string, Exception> _cancelingCompletion =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(19, nameof(CancelingCompletion)), "Canceling dispatch of Completion message for Invocation {invocationId}. The invocation was canceled.");

        private static readonly Action<ILogger, string, Exception> _invokeAfterTermination =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(20, nameof(InvokeAfterTermination)), "Invoke for Invocation '{invocationId}' was called after the connection was terminated.");

        private static readonly Action<ILogger, string, Exception> _invocationAlreadyInUse =
            LoggerMessage.Define<string>(LogLevel.Critical, new EventId(21, nameof(InvocationAlreadyInUse)), "Invocation ID '{invocationId}' is already in use.");

        private static readonly Action<ILogger, string, Exception> _receivedUnexpectedResponse =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(22, nameof(ReceivedUnexpectedResponse)), "Unsolicited response received for invocation '{invocationId}'.");

        private static readonly Action<ILogger, string, Exception> _hubProtocol =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(23, nameof(HubProtocol)), "Using HubProtocol '{protocol}'.");

        private static readonly Action<ILogger, string, string, string, int, Exception> _preparingStreamingInvocation =
            LoggerMessage.Define<string, string, string, int>(LogLevel.Trace, new EventId(24, nameof(PreparingStreamingInvocation)), "Preparing streaming invocation '{invocationId}' of '{target}', with return type '{returnType}' and {argumentCount} argument(s).");

        private static readonly Action<ILogger, Exception> _resettingKeepAliveTimer =
            LoggerMessage.Define(LogLevel.Trace, new EventId(25, nameof(ResettingKeepAliveTimer)), "Resetting keep-alive timer, received a message from the server.");

        private static readonly Action<ILogger, Exception> _errorDuringClosedEvent =
            LoggerMessage.Define(LogLevel.Error, new EventId(26, nameof(ErrorDuringClosedEvent)), "An exception was thrown in the handler for the Closed event.");

        // Category: Streaming and NonStreaming
        private static readonly Action<ILogger, string, Exception> _invocationCreated =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(0, nameof(InvocationCreated)), "Invocation {invocationId} created.");

        private static readonly Action<ILogger, string, Exception> _invocationDisposed =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(1, nameof(InvocationDisposed)), "Invocation {invocationId} disposed.");

        private static readonly Action<ILogger, string, Exception> _invocationCompleted =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(2, nameof(InvocationCompleted)), "Invocation {invocationId} marked as completed.");

        private static readonly Action<ILogger, string, Exception> _invocationFailed =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(3, nameof(InvocationFailed)), "Invocation {invocationId} marked as failed.");

        // Category: Streaming
        private static readonly Action<ILogger, string, Exception> _errorWritingStreamItem =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(4, nameof(ErrorWritingStreamItem)), "Invocation {invocationId} caused an error trying to write a stream item.");

        private static readonly Action<ILogger, string, Exception> _receivedUnexpectedComplete =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(5, nameof(ReceivedUnexpectedComplete)), "Invocation {invocationId} received a completion result, but was invoked as a streaming invocation.");

        // Category: NonStreaming
        private static readonly Action<ILogger, string, Exception> _streamItemOnNonStreamInvocation =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(4, nameof(StreamItemOnNonStreamInvocation)), "Invocation {invocationId} received stream item but was invoked as a non-streamed invocation.");

        private static readonly Action<ILogger, string, Exception> _errorInvokingClientSideMethod =
           LoggerMessage.Define<string>(LogLevel.Error, new EventId(5, nameof(ErrorInvokingClientSideMethod)), "Invoking client side method '{methodName}' failed.");

        public static void PreparingNonBlockingInvocation(this ILogger logger, string invocationId, string target, int count)
        {
            _preparingNonBlockingInvocation(logger, invocationId, target, count, null);
        }

        public static void PreparingBlockingInvocation(this ILogger logger, string invocationId, string target, string returnType, int count)
        {
            _preparingBlockingInvocation(logger, invocationId, target, returnType, count, null);
        }

        public static void PreparingStreamingInvocation(this ILogger logger, string invocationId, string target, string returnType, int count)
        {
            _preparingStreamingInvocation(logger, invocationId, target, returnType, count, null);
        }

        public static void RegisterInvocation(this ILogger logger, string invocationId)
        {
            _registerInvocation(logger, invocationId, null);
        }

        public static void IssueInvocation(this ILogger logger, string invocationId, string returnType, string methodName, object[] args)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                var argsList = args == null ? string.Empty : string.Join(", ", args.Select(a => a?.GetType().FullName ?? "(null)"));
                _issueInvocation(logger, invocationId, returnType, methodName, argsList, null);
            }
        }

        public static void SendInvocation(this ILogger logger, string invocationId)
        {
            _sendInvocation(logger, invocationId, null);
        }

        public static void SendInvocationCompleted(this ILogger logger, string invocationId)
        {
            _sendInvocationCompleted(logger, invocationId, null);
        }

        public static void SendInvocationFailed(this ILogger logger, string invocationId, Exception exception)
        {
            _sendInvocationFailed(logger, invocationId, exception);
        }

        public static void ReceivedInvocation(this ILogger logger, string invocationId, string methodName, object[] args)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                var argsList = args == null ? string.Empty : string.Join(", ", args.Select(a => a?.GetType().FullName ?? "(null)"));
                _receivedInvocation(logger, invocationId, methodName, argsList, null);
            }
        }

        public static void DropCompletionMessage(this ILogger logger, string invocationId)
        {
            _dropCompletionMessage(logger, invocationId, null);
        }

        public static void DropStreamMessage(this ILogger logger, string invocationId)
        {
            _dropStreamMessage(logger, invocationId, null);
        }

        public static void ShutdownConnection(this ILogger logger)
        {
            _shutdownConnection(logger, null);
        }

        public static void ShutdownWithError(this ILogger logger, Exception exception)
        {
            _shutdownWithError(logger, exception);
        }

        public static void RemoveInvocation(this ILogger logger, string invocationId)
        {
            _removeInvocation(logger, invocationId, null);
        }

        public static void MissingHandler(this ILogger logger, string target)
        {
            _missingHandler(logger, target, null);
        }

        public static void ReceivedStreamItem(this ILogger logger, string invocationId)
        {
            _receivedStreamItem(logger, invocationId, null);
        }

        public static void CancelingStreamItem(this ILogger logger, string invocationId)
        {
            _cancelingStreamItem(logger, invocationId, null);
        }

        public static void ReceivedStreamItemAfterClose(this ILogger logger, string invocationId)
        {
            _receivedStreamItemAfterClose(logger, invocationId, null);
        }

        public static void ReceivedInvocationCompletion(this ILogger logger, string invocationId)
        {
            _receivedInvocationCompletion(logger, invocationId, null);
        }

        public static void CancelingInvocationCompletion(this ILogger logger, string invocationId)
        {
            _cancelingInvocationCompletion(logger, invocationId, null);
        }

        public static void CancelingCompletion(this ILogger logger, string invocationId)
        {
            _cancelingCompletion(logger, invocationId, null);
        }

        public static void InvokeAfterTermination(this ILogger logger, string invocationId)
        {
            _invokeAfterTermination(logger, invocationId, null);
        }

        public static void InvocationAlreadyInUse(this ILogger logger, string invocationId)
        {
            _invocationAlreadyInUse(logger, invocationId, null);
        }

        public static void ReceivedUnexpectedResponse(this ILogger logger, string invocationId)
        {
            _receivedUnexpectedResponse(logger, invocationId, null);
        }

        public static void HubProtocol(this ILogger logger, string hubProtocol)
        {
            _hubProtocol(logger, hubProtocol, null);
        }

        public static void InvocationCreated(this ILogger logger, string invocationId)
        {
            _invocationCreated(logger, invocationId, null);
        }

        public static void InvocationDisposed(this ILogger logger, string invocationId)
        {
            _invocationDisposed(logger, invocationId, null);
        }

        public static void InvocationCompleted(this ILogger logger, string invocationId)
        {
            _invocationCompleted(logger, invocationId, null);
        }

        public static void InvocationFailed(this ILogger logger, string invocationId)
        {
            _invocationFailed(logger, invocationId, null);
        }

        public static void ErrorWritingStreamItem(this ILogger logger, string invocationId, Exception exception)
        {
            _errorWritingStreamItem(logger, invocationId, exception);
        }

        public static void ReceivedUnexpectedComplete(this ILogger logger, string invocationId)
        {
            _receivedUnexpectedComplete(logger, invocationId, null);
        }

        public static void StreamItemOnNonStreamInvocation(this ILogger logger, string invocationId)
        {
            _streamItemOnNonStreamInvocation(logger, invocationId, null);
        }

        public static void ErrorInvokingClientSideMethod(this ILogger logger, string methodName, Exception exception)
        {
            _errorInvokingClientSideMethod(logger, methodName, exception);
        }

        public static void ResettingKeepAliveTimer(this ILogger logger)
        {
            _resettingKeepAliveTimer(logger, null);
        }

        public static void ErrorDuringClosedEvent(this ILogger logger, Exception exception)
        {
            _errorDuringClosedEvent(logger, exception);
        }
    }
}

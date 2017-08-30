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
            LoggerMessage.Define<string, string, int>(LogLevel.Trace, 0, "Preparing non-blocking invocation '{invocationId}' of '{target}', with {argumentCount} argument(s).");

        private static readonly Action<ILogger, string, string, string, int, Exception> _preparingBlockingInvocation =
            LoggerMessage.Define<string, string, string, int>(LogLevel.Trace, 1, "Preparing blocking invocation '{invocationId}' of '{target}', with return type '{returnType}' and {argumentCount} argument(s).");

        private static readonly Action<ILogger, string, Exception> _registerInvocation =
            LoggerMessage.Define<string>(LogLevel.Debug, 2, "Registering Invocation ID '{invocationId}' for tracking.");

        private static readonly Action<ILogger, string, string, string, string, Exception> _issueInvocation =
            LoggerMessage.Define<string, string, string, string>(LogLevel.Trace, 3, "Issuing Invocation '{invocationId}': {returnType} {methodName}({args}).");

        private static readonly Action<ILogger, string, Exception> _sendInvocation =
            LoggerMessage.Define<string>(LogLevel.Information, 4, "Sending Invocation '{invocationId}'.");

        private static readonly Action<ILogger, string, Exception> _sendInvocationCompleted =
            LoggerMessage.Define<string>(LogLevel.Information, 5, "Sending Invocation '{invocationId}' completed.");

        private static readonly Action<ILogger, string, Exception> _sendInvocationFailed =
            LoggerMessage.Define<string>(LogLevel.Error, 6, "Sending Invocation '{invocationId}' failed.");

        private static readonly Action<ILogger, string, string, string, Exception> _receivedInvocation =
            LoggerMessage.Define<string, string, string>(LogLevel.Trace, 7, "Received Invocation '{invocationId}': {methodName}({args}).");

        private static readonly Action<ILogger, string, Exception> _dropCompletionMessage =
            LoggerMessage.Define<string>(LogLevel.Warning, 8, "Dropped unsolicited Completion message for invocation '{invocationId}'.");

        private static readonly Action<ILogger, string, Exception> _dropStreamMessage =
            LoggerMessage.Define<string>(LogLevel.Warning, 9, "Dropped unsolicited StreamItem message for invocation '{invocationId}'.");

        private static readonly Action<ILogger, Exception> _shutdownConnection =
            LoggerMessage.Define(LogLevel.Trace, 10, "Shutting down connection.");

        private static readonly Action<ILogger, Exception> _shutdownWithError =
            LoggerMessage.Define(LogLevel.Error, 11, "Connection is shutting down due to an error.");

        private static readonly Action<ILogger, string, Exception> _removeInvocation =
            LoggerMessage.Define<string>(LogLevel.Trace, 12, "Removing pending invocation {invocationId}.");

        private static readonly Action<ILogger, string, Exception> _missingHandler =
            LoggerMessage.Define<string>(LogLevel.Warning, 13, "Failed to find handler for '{target}' method.");

        private static readonly Action<ILogger, string, Exception> _receivedStreamItem =
            LoggerMessage.Define<string>(LogLevel.Trace, 14, "Received StreamItem for Invocation {invocationId}.");

        private static readonly Action<ILogger, string, Exception> _cancelingStreamItem =
            LoggerMessage.Define<string>(LogLevel.Trace, 15, "Canceling dispatch of StreamItem message for Invocation {invocationId}. The invocation was canceled.");

        private static readonly Action<ILogger, string, Exception> _receivedStreamItemAfterClose =
            LoggerMessage.Define<string>(LogLevel.Warning, 16, "Invocation {invocationId} received stream item after channel was closed.");

        private static readonly Action<ILogger, string, Exception> _receivedInvocationCompletion =
            LoggerMessage.Define<string>(LogLevel.Trace, 17, "Received Completion for Invocation {invocationId}.");

        private static readonly Action<ILogger, string, Exception> _cancelingCompletion =
            LoggerMessage.Define<string>(LogLevel.Trace, 18, "Canceling dispatch of Completion message for Invocation {invocationId}. The invocation was canceled.");

        private static readonly Action<ILogger, string, Exception> _invokeAfterTermination =
            LoggerMessage.Define<string>(LogLevel.Error, 19, "Invoke for Invocation '{invocationId}' was called after the connection was terminated.");

        private static readonly Action<ILogger, string, Exception> _invocationAlreadyInUse =
            LoggerMessage.Define<string>(LogLevel.Critical, 20, "Invocation ID '{invocationId}' is already in use.");

        private static readonly Action<ILogger, string, Exception> _receivedUnexpectedResponse =
            LoggerMessage.Define<string>(LogLevel.Error, 21, "Unsolicited response received for invocation '{invocationId}'.");

        // Category: Streaming and NonStreaming
        private static readonly Action<ILogger, string, Exception> _invocationCreated =
            LoggerMessage.Define<string>(LogLevel.Trace, 0, "Invocation {invocationId} created.");

        private static readonly Action<ILogger, string, Exception> _invocationDisposed =
            LoggerMessage.Define<string>(LogLevel.Trace, 1, "Invocation {invocationId} disposed.");

        private static readonly Action<ILogger, string, Exception> _invocationCompleted =
            LoggerMessage.Define<string>(LogLevel.Trace, 2, "Invocation {invocationId} marked as completed.");

        private static readonly Action<ILogger, string, Exception> _invocationFailed =
            LoggerMessage.Define<string>(LogLevel.Trace, 3, "Invocation {invocationId} marked as failed.");

        // Category: Streaming
        private static readonly Action<ILogger, string, Exception> _receivedUnexpectedComplete =
            LoggerMessage.Define<string>(LogLevel.Error, 4, "Invocation {invocationId} received a completion result, but was invoked as a streaming invocation.");

        private static readonly Action<ILogger, string, Exception> _errorWritingStreamItem =
            LoggerMessage.Define<string>(LogLevel.Error, 5, "Invocation {invocationId} caused an error trying to write a stream item.");

        // Category: NonStreaming
        private static readonly Action<ILogger, string, Exception> _streamItemOnNonStreamInvocation =
            LoggerMessage.Define<string>(LogLevel.Error, 4, "Invocation {invocationId} received stream item but was invoked as a non-streamed invocation.");

        public static void PreparingNonBlockingInvocation(this ILogger logger, string invocationId, string target, int count)
        {
            _preparingNonBlockingInvocation(logger, invocationId, target, count, null);
        }

        public static void PreparingBlockingInvocation(this ILogger logger, string invocationId, string target, string returnType, int count)
        {
            _preparingBlockingInvocation(logger, invocationId, target, returnType, count, null);
        }

        public static void RegisterInvocation(this ILogger logger, string invocationId)
        {
            _registerInvocation(logger, invocationId, null);
        }

        public static void IssueInvocation(this ILogger logger, string invocationId, string returnType, string methodName, object[] args)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                var argsList = string.Join(", ", args.Select(a => a.GetType().FullName));
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
                var argsList = string.Join(", ", args.Select(a => a.GetType().FullName));
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

        public static void ReceivedUnexpectedComplete(this ILogger logger, string invocationId)
        {
            _receivedUnexpectedComplete(logger, invocationId, null);
        }

        public static void ErrorWritingStreamItem(this ILogger logger, string invocationId, Exception exception)
        {
            _errorWritingStreamItem(logger, invocationId, exception);
        }

        public static void StreamItemOnNonStreamInvocation(this ILogger logger, string invocationId)
        {
            _streamItemOnNonStreamInvocation(logger, invocationId, null);
        }
    }
}

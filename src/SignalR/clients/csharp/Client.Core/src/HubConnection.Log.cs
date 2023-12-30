// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client;

public partial class HubConnection
{
    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Trace, "Preparing non-blocking invocation of '{Target}', with {ArgumentCount} argument(s).", EventName = "PreparingNonBlockingInvocation")]
        public static partial void PreparingNonBlockingInvocation(ILogger logger, string target, int argumentCount);

        [LoggerMessage(2, LogLevel.Trace, "Preparing blocking invocation '{InvocationId}' of '{Target}', with return type '{ReturnType}' and {ArgumentCount} argument(s).", EventName = "PreparingBlockingInvocation")]
        public static partial void PreparingBlockingInvocation(ILogger logger, string invocationId, string target, string returnType, int argumentCount);

        [LoggerMessage(3, LogLevel.Debug, "Registering Invocation ID '{InvocationId}' for tracking.", EventName = "RegisteringInvocation")]
        public static partial void RegisteringInvocation(ILogger logger, string invocationId);

        [LoggerMessage(4, LogLevel.Trace, "Issuing Invocation '{InvocationId}': {ReturnType} {MethodName}({Args}).", EventName = "IssuingInvocation", SkipEnabledCheck = true)]
        private static partial void IssuingInvocation(ILogger logger, string invocationId, string returnType, string methodName, string args);

        public static void IssuingInvocation(ILogger logger, string invocationId, string returnType, string methodName, object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                var argsList = args == null ? string.Empty : string.Join(", ", args.Select(a => a?.GetType().FullName ?? "(null)"));
                IssuingInvocation(logger, invocationId, returnType, methodName, argsList);
            }
        }

        public static void SendingMessage(ILogger logger, HubMessage message)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                if (message is HubInvocationMessage invocationMessage)
                {
                    SendingMessage(logger, message.GetType().Name, invocationMessage.InvocationId);
                }
                else
                {
                    SendingMessageGeneric(logger, message.GetType().Name);
                }
            }
        }

        [LoggerMessage(5, LogLevel.Debug, "Sending {MessageType} message '{InvocationId}'.", EventName = "SendingMessage", SkipEnabledCheck = true)]
        private static partial void SendingMessage(ILogger logger, string messageType, string? invocationId);

        [LoggerMessage(59, LogLevel.Debug, "Sending {MessageType} message.", EventName = "SendingMessageGeneric", SkipEnabledCheck = true)]
        private static partial void SendingMessageGeneric(ILogger logger, string messageType);

        public static void MessageSent(ILogger logger, HubMessage message)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                if (message is HubInvocationMessage invocationMessage)
                {
                    MessageSent(logger, message.GetType().Name, invocationMessage.InvocationId);
                }
                else
                {
                    MessageSentGeneric(logger, message.GetType().Name);
                }
            }
        }

        [LoggerMessage(6, LogLevel.Debug, "Sending {MessageType} message '{InvocationId}' completed.", EventName = "MessageSent", SkipEnabledCheck = true)]
        private static partial void MessageSent(ILogger logger, string messageType, string? invocationId);

        [LoggerMessage(60, LogLevel.Debug, "Sending {MessageType} message completed.", EventName = "MessageSentGeneric", SkipEnabledCheck = true)]
        private static partial void MessageSentGeneric(ILogger logger, string messageType);

        [LoggerMessage(7, LogLevel.Error, "Sending Invocation '{InvocationId}' failed.", EventName = "FailedToSendInvocation")]
        public static partial void FailedToSendInvocation(ILogger logger, string invocationId, Exception exception);

        [LoggerMessage(8, LogLevel.Trace, "Received Invocation '{InvocationId}': {MethodName}({Args}).", EventName = "ReceivedInvocation", SkipEnabledCheck = true)]
        private static partial void ReceivedInvocation(ILogger logger, string? invocationId, string methodName, string args);

        public static void ReceivedInvocation(ILogger logger, string? invocationId, string methodName, object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                var argsList = args == null ? string.Empty : string.Join(", ", args.Select(a => a?.GetType().FullName ?? "(null)"));
                ReceivedInvocation(logger, invocationId, methodName, argsList);
            }
        }

        [LoggerMessage(9, LogLevel.Warning, "Dropped unsolicited Completion message for invocation '{InvocationId}'.", EventName = "DroppedCompletionMessage")]
        public static partial void DroppedCompletionMessage(ILogger logger, string invocationId);

        [LoggerMessage(10, LogLevel.Warning, "Dropped unsolicited StreamItem message for invocation '{InvocationId}'.", EventName = "DroppedStreamMessage")]
        public static partial void DroppedStreamMessage(ILogger logger, string invocationId);

        [LoggerMessage(11, LogLevel.Trace, "Shutting down connection.", EventName = "ShutdownConnection")]
        public static partial void ShutdownConnection(ILogger logger);

        [LoggerMessage(12, LogLevel.Error, "Connection is shutting down due to an error.", EventName = "ShutdownWithError")]
        public static partial void ShutdownWithError(ILogger logger, Exception exception);

        [LoggerMessage(13, LogLevel.Trace, "Removing pending invocation {InvocationId}.", EventName = "RemovingInvocation")]
        public static partial void RemovingInvocation(ILogger logger, string invocationId);

        [LoggerMessage(14, LogLevel.Warning, "Failed to find handler for '{Target}' method.", EventName = "MissingHandler")]
        public static partial void MissingHandler(ILogger logger, string target);

        [LoggerMessage(15, LogLevel.Trace, "Received StreamItem for Invocation {InvocationId}.", EventName = "ReceivedStreamItem")]
        public static partial void ReceivedStreamItem(ILogger logger, string invocationId);

        [LoggerMessage(16, LogLevel.Trace, "Canceling dispatch of StreamItem message for Invocation {InvocationId}. The invocation was canceled.", EventName = "CancelingStreamItem")]
        public static partial void CancelingStreamItem(ILogger logger, string invocationId);

        [LoggerMessage(17, LogLevel.Warning, "Invocation {InvocationId} received stream item after channel was closed.", EventName = "ReceivedStreamItemAfterClose")]
        public static partial void ReceivedStreamItemAfterClose(ILogger logger, string invocationId);

        [LoggerMessage(18, LogLevel.Trace, "Received Completion for Invocation {InvocationId}.", EventName = "ReceivedInvocationCompletion")]
        public static partial void ReceivedInvocationCompletion(ILogger logger, string invocationId);

        [LoggerMessage(19, LogLevel.Trace, "Canceling dispatch of Completion message for Invocation {InvocationId}. The invocation was canceled.", EventName = "CancelingInvocationCompletion")]
        public static partial void CancelingInvocationCompletion(ILogger logger, string invocationId);

        [LoggerMessage(21, LogLevel.Debug, "HubConnection stopped.", EventName = "Stopped")]
        public static partial void Stopped(ILogger logger);

        [LoggerMessage(22, LogLevel.Critical, "Invocation ID '{InvocationId}' is already in use.", EventName = "InvocationAlreadyInUse")]
        public static partial void InvocationAlreadyInUse(ILogger logger, string invocationId);

        [LoggerMessage(23, LogLevel.Error, "Unsolicited response received for invocation '{InvocationId}'.", EventName = "ReceivedUnexpectedResponse")]
        public static partial void ReceivedUnexpectedResponse(ILogger logger, string invocationId);

        [LoggerMessage(24, LogLevel.Information, "Using HubProtocol '{Protocol} v{Version}'.", EventName = "HubProtocol")]
        public static partial void HubProtocol(ILogger logger, string protocol, int version);

        [LoggerMessage(25, LogLevel.Trace, "Preparing streaming invocation '{InvocationId}' of '{Target}', with return type '{ReturnType}' and {ArgumentCount} argument(s).", EventName = "PreparingStreamingInvocation")]
        public static partial void PreparingStreamingInvocation(ILogger logger, string invocationId, string target, string returnType, int argumentCount);

        [LoggerMessage(26, LogLevel.Trace, "Resetting keep-alive timer, received a message from the server.", EventName = "ResettingKeepAliveTimer")]
        public static partial void ResettingKeepAliveTimer(ILogger logger);

        [LoggerMessage(27, LogLevel.Error, "An exception was thrown in the handler for the Closed event.", EventName = "ErrorDuringClosedEvent")]
        public static partial void ErrorDuringClosedEvent(ILogger logger, Exception exception);

        [LoggerMessage(28, LogLevel.Debug, "Sending Hub Handshake.", EventName = "SendingHubHandshake")]
        public static partial void SendingHubHandshake(ILogger logger);

        [LoggerMessage(31, LogLevel.Trace, "Received a ping message.", EventName = "ReceivedPing")]
        public static partial void ReceivedPing(ILogger logger);

        [LoggerMessage(34, LogLevel.Error, "Invoking client side method '{MethodName}' failed.", EventName = "ErrorInvokingClientSideMethod")]
        public static partial void ErrorInvokingClientSideMethod(ILogger logger, string methodName, Exception exception);

        [LoggerMessage(35, LogLevel.Error, "The underlying connection closed while processing the handshake response. See exception for details.", EventName = "ErrorReceivingHandshakeResponse")]
        public static partial void ErrorReceivingHandshakeResponse(ILogger logger, Exception exception);

        [LoggerMessage(36, LogLevel.Error, "Server returned handshake error: {Error}", EventName = "HandshakeServerError")]
        public static partial void HandshakeServerError(ILogger logger, string error);

        [LoggerMessage(37, LogLevel.Debug, "Received close message.", EventName = "ReceivedClose")]
        public static partial void ReceivedClose(ILogger logger);

        [LoggerMessage(38, LogLevel.Error, "Received close message with an error: {Error}", EventName = "ReceivedCloseWithError")]
        public static partial void ReceivedCloseWithError(ILogger logger, string error);

        [LoggerMessage(39, LogLevel.Debug, "Handshake with server complete.", EventName = "HandshakeComplete")]
        public static partial void HandshakeComplete(ILogger logger);

        [LoggerMessage(40, LogLevel.Debug, "Registering handler for client method '{MethodName}'.", EventName = "RegisteringHandler")]
        public static partial void RegisteringHandler(ILogger logger, string methodName);

        [LoggerMessage(58, LogLevel.Debug, "Removing handlers for client method '{MethodName}'.", EventName = "RemovingHandlers")]
        public static partial void RemovingHandlers(ILogger logger, string methodName);

        [LoggerMessage(41, LogLevel.Debug, "Starting HubConnection.", EventName = "Starting")]
        public static partial void Starting(ILogger logger);

        [LoggerMessage(43, LogLevel.Error, "Error starting connection.", EventName = "ErrorStartingConnection")]
        public static partial void ErrorStartingConnection(ILogger logger, Exception ex);

        [LoggerMessage(44, LogLevel.Information, "HubConnection started.", EventName = "Started")]
        public static partial void Started(ILogger logger);

        [LoggerMessage(45, LogLevel.Debug, "Sending Cancellation for Invocation '{InvocationId}'.", EventName = "SendingCancellation")]
        public static partial void SendingCancellation(ILogger logger, string invocationId);

        [LoggerMessage(46, LogLevel.Debug, "Canceling all outstanding invocations.", EventName = "CancelingOutstandingInvocations")]
        public static partial void CancelingOutstandingInvocations(ILogger logger);

        [LoggerMessage(47, LogLevel.Debug, "Receive loop starting.", EventName = "ReceiveLoopStarting")]
        public static partial void ReceiveLoopStarting(ILogger logger);

        [LoggerMessage(48, LogLevel.Debug, "Starting server timeout timer. Duration: {ServerTimeout:0.00}ms", EventName = "StartingServerTimeoutTimer", SkipEnabledCheck = true)]
        public static partial void StartingServerTimeoutTimer(ILogger logger, double serverTimeout);

        public static void StartingServerTimeoutTimer(ILogger logger, TimeSpan serverTimeout)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                StartingServerTimeoutTimer(logger, serverTimeout.TotalMilliseconds);
            }
        }

        [LoggerMessage(49, LogLevel.Debug, "Not using server timeout because the transport inherently tracks server availability.", EventName = "NotUsingServerTimeout")]
        public static partial void NotUsingServerTimeout(ILogger logger);

        [LoggerMessage(50, LogLevel.Error, "The server connection was terminated with an error.", EventName = "ServerDisconnectedWithError")]
        public static partial void ServerDisconnectedWithError(ILogger logger, Exception ex);

        [LoggerMessage(51, LogLevel.Debug, "Invoking the Closed event handler.", EventName = "InvokingClosedEventHandler")]
        public static partial void InvokingClosedEventHandler(ILogger logger);

        [LoggerMessage(52, LogLevel.Debug, "Stopping HubConnection.", EventName = "Stopping")]
        public static partial void Stopping(ILogger logger);

        [LoggerMessage(53, LogLevel.Debug, "Terminating receive loop.", EventName = "TerminatingReceiveLoop")]
        public static partial void TerminatingReceiveLoop(ILogger logger);

        [LoggerMessage(54, LogLevel.Debug, "Waiting for the receive loop to terminate.", EventName = "WaitingForReceiveLoopToTerminate")]
        public static partial void WaitingForReceiveLoopToTerminate(ILogger logger);

        [LoggerMessage(56, LogLevel.Debug, "Processing {MessageLength} byte message from server.", EventName = "ProcessingMessage")]
        public static partial void ProcessingMessage(ILogger logger, long messageLength);

        [LoggerMessage(42, LogLevel.Trace, "Waiting on Connection Lock in {MethodName} ({FilePath}:{LineNumber}).", EventName = "WaitingOnConnectionLock")]
        public static partial void WaitingOnConnectionLock(ILogger logger, string? methodName, string? filePath, int lineNumber);

        [LoggerMessage(20, LogLevel.Trace, "Releasing Connection Lock in {MethodName} ({FilePath}:{LineNumber}).", EventName = "ReleasingConnectionLock")]
        public static partial void ReleasingConnectionLock(ILogger logger, string? methodName, string? filePath, int lineNumber);

        [LoggerMessage(55, LogLevel.Trace, "Unable to send cancellation for invocation '{InvocationId}'. The connection is inactive.", EventName = "UnableToSendCancellation")]
        public static partial void UnableToSendCancellation(ILogger logger, string invocationId);

        [LoggerMessage(57, LogLevel.Error, "Failed to bind arguments received in invocation '{InvocationId}' of '{MethodName}'.", EventName = "ArgumentBindingFailure")]
        public static partial void ArgumentBindingFailure(ILogger logger, string? invocationId, string methodName, Exception exception);

        [LoggerMessage(61, LogLevel.Trace, "Acquired the Connection Lock in order to ping the server.", EventName = "AcquiredConnectionLockForPing")]
        public static partial void AcquiredConnectionLockForPing(ILogger logger);

        [LoggerMessage(62, LogLevel.Trace, "Skipping ping because a send is already in progress.", EventName = "UnableToAcquireConnectionLockForPing")]
        public static partial void UnableToAcquireConnectionLockForPing(ILogger logger);

        [LoggerMessage(63, LogLevel.Trace, "Initiating stream '{StreamId}'.", EventName = "StartingStream")]
        public static partial void StartingStream(ILogger logger, string streamId);

        [LoggerMessage(64, LogLevel.Trace, "Sending item for stream '{StreamId}'.", EventName = "StreamItemSent")]
        public static partial void SendingStreamItem(ILogger logger, string streamId);

        [LoggerMessage(65, LogLevel.Trace, "Stream '{StreamId}' has been canceled by client.", EventName = "CancelingStream")]
        public static partial void CancelingStream(ILogger logger, string streamId);

        [LoggerMessage(66, LogLevel.Trace, "Sending completion message for stream '{StreamId}'.", EventName = "CompletingStream")]
        public static partial void CompletingStream(ILogger logger, string streamId);

        [LoggerMessage(67, LogLevel.Error, "The HubConnection failed to transition from the {ExpectedState} state to the {NewState} state because it was actually in the {ActualState} state.", EventName = "StateTransitionFailed")]
        public static partial void StateTransitionFailed(ILogger logger, HubConnectionState expectedState, HubConnectionState newState, HubConnectionState actualState);

        [LoggerMessage(68, LogLevel.Information, "HubConnection reconnecting.", EventName = "Reconnecting")]
        public static partial void Reconnecting(ILogger logger);

        [LoggerMessage(69, LogLevel.Error, "HubConnection reconnecting due to an error.", EventName = "ReconnectingWithError")]
        public static partial void ReconnectingWithError(ILogger logger, Exception exception);

        [LoggerMessage(70, LogLevel.Information, "HubConnection reconnected successfully after {ReconnectAttempts} attempts and {ElapsedTime} elapsed.", EventName = "Reconnected")]
        public static partial void Reconnected(ILogger logger, long reconnectAttempts, TimeSpan elapsedTime);

        [LoggerMessage(71, LogLevel.Information, "Reconnect retries have been exhausted after {ReconnectAttempts} failed attempts and {ElapsedTime} elapsed. Disconnecting.", EventName = "ReconnectAttemptsExhausted")]
        public static partial void ReconnectAttemptsExhausted(ILogger logger, long reconnectAttempts, TimeSpan elapsedTime);

        [LoggerMessage(72, LogLevel.Trace, "Reconnect attempt number {ReconnectAttempts} will start in {RetryDelay}.", EventName = "AwaitingReconnectRetryDelay")]
        public static partial void AwaitingReconnectRetryDelay(ILogger logger, long reconnectAttempts, TimeSpan retryDelay);

        [LoggerMessage(73, LogLevel.Trace, "Reconnect attempt failed.", EventName = "ReconnectAttemptFailed")]
        public static partial void ReconnectAttemptFailed(ILogger logger, Exception exception);

        [LoggerMessage(74, LogLevel.Error, "An exception was thrown in the handler for the Reconnecting event.", EventName = "ErrorDuringReconnectingEvent")]
        public static partial void ErrorDuringReconnectingEvent(ILogger logger, Exception exception);

        [LoggerMessage(75, LogLevel.Error, "An exception was thrown in the handler for the Reconnected event.", EventName = "ErrorDuringReconnectedEvent")]
        public static partial void ErrorDuringReconnectedEvent(ILogger logger, Exception exception);

        [LoggerMessage(76, LogLevel.Error, $"An exception was thrown from {nameof(IRetryPolicy)}.{nameof(IRetryPolicy.NextRetryDelay)}().", EventName = "ErrorDuringNextRetryDelay")]
        public static partial void ErrorDuringNextRetryDelay(ILogger logger, Exception exception);

        [LoggerMessage(77, LogLevel.Warning, "Connection not reconnecting because the IRetryPolicy returned null on the first reconnect attempt.", EventName = "FirstReconnectRetryDelayNull")]
        public static partial void FirstReconnectRetryDelayNull(ILogger logger);

        [LoggerMessage(78, LogLevel.Trace, "Connection stopped during reconnect delay. Done reconnecting.", EventName = "ReconnectingStoppedDueToStateChangeDuringRetryDelay")]
        public static partial void ReconnectingStoppedDuringRetryDelay(ILogger logger);

        [LoggerMessage(79, LogLevel.Trace, "Connection stopped during reconnect attempt. Done reconnecting.", EventName = "ReconnectingStoppedDueToStateChangeDuringReconnectAttempt")]
        public static partial void ReconnectingStoppedDuringReconnectAttempt(ILogger logger);

        [LoggerMessage(80, LogLevel.Trace, "The HubConnection is attempting to transition from the {ExpectedState} state to the {NewState} state.", EventName = "AttemptingStateTransition")]
        public static partial void AttemptingStateTransition(ILogger logger, HubConnectionState expectedState, HubConnectionState newState);

        [LoggerMessage(81, LogLevel.Error, "Received an invalid handshake response.", EventName = "ErrorInvalidHandshakeResponse")]
        public static partial void ErrorInvalidHandshakeResponse(ILogger logger, Exception exception);

        public static void ErrorHandshakeTimedOut(ILogger logger, TimeSpan handshakeTimeout, Exception exception)
            => ErrorHandshakeTimedOut(logger, handshakeTimeout.TotalSeconds, exception);

        [LoggerMessage(82, LogLevel.Error, "The handshake timed out after {HandshakeTimeoutSeconds} seconds.", EventName = "ErrorHandshakeTimedOut")]
        private static partial void ErrorHandshakeTimedOut(ILogger logger, double HandshakeTimeoutSeconds, Exception exception);

        [LoggerMessage(83, LogLevel.Error, "The handshake was canceled by the client.", EventName = "ErrorHandshakeCanceled")]
        public static partial void ErrorHandshakeCanceled(ILogger logger, Exception exception);

        [LoggerMessage(84, LogLevel.Trace, "Client threw an error for stream '{StreamId}'.", EventName = "ErroredStream")]
        public static partial void ErroredStream(ILogger logger, string streamId, Exception exception);

        [LoggerMessage(85, LogLevel.Warning, "Failed to find a value returning handler for '{Target}' method. Sending error to server.", EventName = "MissingResultHandler")]
        public static partial void MissingResultHandler(ILogger logger, string target);

        [LoggerMessage(86, LogLevel.Warning, "Result given for '{Target}' method but server is not expecting a result.", EventName = "ResultNotExpected")]
        public static partial void ResultNotExpected(ILogger logger, string target);

        [LoggerMessage(87, LogLevel.Trace, "Completion message for stream '{StreamId}' was not sent because the connection is closed.", EventName = "CompletingStreamNotSent")]
        public static partial void CompletingStreamNotSent(ILogger logger, string streamId);

        [LoggerMessage(88, LogLevel.Warning, "Error returning result for invocation '{InvocationId}' for method '{Target}' because the underlying connection is closed.", EventName = "ErrorSendingInvocationResult")]
        public static partial void ErrorSendingInvocationResult(ILogger logger, string invocationId, string target, Exception exception);

        [LoggerMessage(89, LogLevel.Trace, "Error sending Completion message for stream '{StreamId}'.", EventName = "ErrorSendingStreamCompletion")]
        public static partial void ErrorSendingStreamCompletion(ILogger logger, string streamId, Exception exception);

        [LoggerMessage(90, LogLevel.Trace, "Dropping {MessageType} with ID '{InvocationId}'.", EventName = "DroppingMessage")]
        public static partial void DroppingMessage(ILogger logger, string messageType, string? invocationId);

        [LoggerMessage(91, LogLevel.Trace, "Received AckMessage with Sequence ID '{SequenceId}'.", EventName = "ReceivedAckMessage")]
        public static partial void ReceivedAckMessage(ILogger logger, long sequenceId);

        [LoggerMessage(92, LogLevel.Trace, "Received SequenceMessage with Sequence ID '{SequenceId}'.", EventName = "ReceivedSequenceMessage")]
        public static partial void ReceivedSequenceMessage(ILogger logger, long sequenceId);

        [LoggerMessage(93, LogLevel.Debug, "HubProtocol '{Protocol} v{Version}' does not support Stateful Reconnect. Disabling the feature.", EventName = "DisablingReconnect")]
        public static partial void DisablingReconnect(ILogger logger, string protocol, int version);
    }
}

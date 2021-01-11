// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public partial class HubConnection
    {
        private static class Log
        {
            private static readonly Action<ILogger, string, int, Exception?> _preparingNonBlockingInvocation =
            LoggerMessage.Define<string, int>(LogLevel.Trace, new EventId(1, "PreparingNonBlockingInvocation"), "Preparing non-blocking invocation of '{Target}', with {ArgumentCount} argument(s).");

            private static readonly Action<ILogger, string, string, string, int, Exception?> _preparingBlockingInvocation =
                LoggerMessage.Define<string, string, string, int>(LogLevel.Trace, new EventId(2, "PreparingBlockingInvocation"), "Preparing blocking invocation '{InvocationId}' of '{Target}', with return type '{ReturnType}' and {ArgumentCount} argument(s).");

            private static readonly Action<ILogger, string, Exception?> _registeringInvocation =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(3, "RegisteringInvocation"), "Registering Invocation ID '{InvocationId}' for tracking.");

            private static readonly Action<ILogger, string, string, string, string, Exception?> _issuingInvocation =
                LoggerMessage.Define<string, string, string, string>(LogLevel.Trace, new EventId(4, "IssuingInvocation"), "Issuing Invocation '{InvocationId}': {ReturnType} {MethodName}({Args}).");

            private static readonly Action<ILogger, string, string?, Exception?> _sendingMessage =
                LoggerMessage.Define<string, string?>(LogLevel.Debug, new EventId(5, "SendingMessage"), "Sending {MessageType} message '{InvocationId}'.");

            private static readonly Action<ILogger, string, string?, Exception?> _messageSent =
                LoggerMessage.Define<string, string?>(LogLevel.Debug, new EventId(6, "MessageSent"), "Sending {MessageType} message '{InvocationId}' completed.");

            private static readonly Action<ILogger, string, Exception> _failedToSendInvocation =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(7, "FailedToSendInvocation"), "Sending Invocation '{InvocationId}' failed.");

            private static readonly Action<ILogger, string?, string, string, Exception?> _receivedInvocation =
                LoggerMessage.Define<string?, string, string>(LogLevel.Trace, new EventId(8, "ReceivedInvocation"), "Received Invocation '{InvocationId}': {MethodName}({Args}).");

            private static readonly Action<ILogger, string, Exception?> _droppedCompletionMessage =
                LoggerMessage.Define<string>(LogLevel.Warning, new EventId(9, "DroppedCompletionMessage"), "Dropped unsolicited Completion message for invocation '{InvocationId}'.");

            private static readonly Action<ILogger, string, Exception?> _droppedStreamMessage =
                LoggerMessage.Define<string>(LogLevel.Warning, new EventId(10, "DroppedStreamMessage"), "Dropped unsolicited StreamItem message for invocation '{InvocationId}'.");

            private static readonly Action<ILogger, Exception?> _shutdownConnection =
                LoggerMessage.Define(LogLevel.Trace, new EventId(11, "ShutdownConnection"), "Shutting down connection.");

            private static readonly Action<ILogger, Exception> _shutdownWithError =
                LoggerMessage.Define(LogLevel.Error, new EventId(12, "ShutdownWithError"), "Connection is shutting down due to an error.");

            private static readonly Action<ILogger, string, Exception?> _removingInvocation =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(13, "RemovingInvocation"), "Removing pending invocation {InvocationId}.");

            private static readonly Action<ILogger, string, Exception?> _missingHandler =
                LoggerMessage.Define<string>(LogLevel.Warning, new EventId(14, "MissingHandler"), "Failed to find handler for '{Target}' method.");

            private static readonly Action<ILogger, string, Exception?> _receivedStreamItem =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(15, "ReceivedStreamItem"), "Received StreamItem for Invocation {InvocationId}.");

            private static readonly Action<ILogger, string, Exception?> _cancelingStreamItem =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(16, "CancelingStreamItem"), "Canceling dispatch of StreamItem message for Invocation {InvocationId}. The invocation was canceled.");

            private static readonly Action<ILogger, string, Exception?> _receivedStreamItemAfterClose =
                LoggerMessage.Define<string>(LogLevel.Warning, new EventId(17, "ReceivedStreamItemAfterClose"), "Invocation {InvocationId} received stream item after channel was closed.");

            private static readonly Action<ILogger, string, Exception?> _receivedInvocationCompletion =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(18, "ReceivedInvocationCompletion"), "Received Completion for Invocation {InvocationId}.");

            private static readonly Action<ILogger, string, Exception?> _cancelingInvocationCompletion =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(19, "CancelingInvocationCompletion"), "Canceling dispatch of Completion message for Invocation {InvocationId}. The invocation was canceled.");

            private static readonly Action<ILogger, string, string, int, Exception?> _releasingConnectionLock =
                LoggerMessage.Define<string, string, int>(LogLevel.Trace, new EventId(20, "ReleasingConnectionLock"), "Releasing Connection Lock in {MethodName} ({FilePath}:{LineNumber}).");

            private static readonly Action<ILogger, Exception?> _stopped =
                LoggerMessage.Define(LogLevel.Debug, new EventId(21, "Stopped"), "HubConnection stopped.");

            private static readonly Action<ILogger, string, Exception?> _invocationAlreadyInUse =
                LoggerMessage.Define<string>(LogLevel.Critical, new EventId(22, "InvocationAlreadyInUse"), "Invocation ID '{InvocationId}' is already in use.");

            private static readonly Action<ILogger, string, Exception?> _receivedUnexpectedResponse =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(23, "ReceivedUnexpectedResponse"), "Unsolicited response received for invocation '{InvocationId}'.");

            private static readonly Action<ILogger, string, int, Exception?> _hubProtocol =
                LoggerMessage.Define<string, int>(LogLevel.Information, new EventId(24, "HubProtocol"), "Using HubProtocol '{Protocol} v{Version}'.");

            private static readonly Action<ILogger, string, string, string, int, Exception?> _preparingStreamingInvocation =
                LoggerMessage.Define<string, string, string, int>(LogLevel.Trace, new EventId(25, "PreparingStreamingInvocation"), "Preparing streaming invocation '{InvocationId}' of '{Target}', with return type '{ReturnType}' and {ArgumentCount} argument(s).");

            private static readonly Action<ILogger, Exception?> _resettingKeepAliveTimer =
                LoggerMessage.Define(LogLevel.Trace, new EventId(26, "ResettingKeepAliveTimer"), "Resetting keep-alive timer, received a message from the server.");

            private static readonly Action<ILogger, Exception> _errorDuringClosedEvent =
                LoggerMessage.Define(LogLevel.Error, new EventId(27, "ErrorDuringClosedEvent"), "An exception was thrown in the handler for the Closed event.");

            private static readonly Action<ILogger, Exception?> _sendingHubHandshake =
                LoggerMessage.Define(LogLevel.Debug, new EventId(28, "SendingHubHandshake"), "Sending Hub Handshake.");

            private static readonly Action<ILogger, Exception?> _receivedPing =
                LoggerMessage.Define(LogLevel.Trace, new EventId(31, "ReceivedPing"), "Received a ping message.");

            private static readonly Action<ILogger, string, Exception> _errorInvokingClientSideMethod =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(34, "ErrorInvokingClientSideMethod"), "Invoking client side method '{MethodName}' failed.");

            private static readonly Action<ILogger, Exception> _errorProcessingHandshakeResponse =
                LoggerMessage.Define(LogLevel.Error, new EventId(35, "ErrorReceivingHandshakeResponse"), "The underlying connection closed while processing the handshake response. See exception for details.");

            private static readonly Action<ILogger, string, Exception?> _handshakeServerError =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(36, "HandshakeServerError"), "Server returned handshake error: {Error}");

            private static readonly Action<ILogger, Exception?> _receivedClose =
                LoggerMessage.Define(LogLevel.Debug, new EventId(37, "ReceivedClose"), "Received close message.");

            private static readonly Action<ILogger, string, Exception?> _receivedCloseWithError =
                LoggerMessage.Define<string>(LogLevel.Error, new EventId(38, "ReceivedCloseWithError"), "Received close message with an error: {Error}");

            private static readonly Action<ILogger, Exception?> _handshakeComplete =
                LoggerMessage.Define(LogLevel.Debug, new EventId(39, "HandshakeComplete"), "Handshake with server complete.");

            private static readonly Action<ILogger, string, Exception?> _registeringHandler =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(40, "RegisteringHandler"), "Registering handler for client method '{MethodName}'.");

            private static readonly Action<ILogger, Exception?> _starting =
                LoggerMessage.Define(LogLevel.Debug, new EventId(41, "Starting"), "Starting HubConnection.");

            private static readonly Action<ILogger, string, string, int, Exception?> _waitingOnConnectionLock =
                LoggerMessage.Define<string, string, int>(LogLevel.Trace, new EventId(42, "WaitingOnConnectionLock"), "Waiting on Connection Lock in {MethodName} ({FilePath}:{LineNumber}).");

            private static readonly Action<ILogger, Exception> _errorStartingConnection =
                LoggerMessage.Define(LogLevel.Error, new EventId(43, "ErrorStartingConnection"), "Error starting connection.");

            private static readonly Action<ILogger, Exception?> _started =
                LoggerMessage.Define(LogLevel.Information, new EventId(44, "Started"), "HubConnection started.");

            private static readonly Action<ILogger, string, Exception?> _sendingCancellation =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(45, "SendingCancellation"), "Sending Cancellation for Invocation '{InvocationId}'.");

            private static readonly Action<ILogger, Exception?> _cancelingOutstandingInvocations =
                LoggerMessage.Define(LogLevel.Debug, new EventId(46, "CancelingOutstandingInvocations"), "Canceling all outstanding invocations.");

            private static readonly Action<ILogger, Exception?> _receiveLoopStarting =
                LoggerMessage.Define(LogLevel.Debug, new EventId(47, "ReceiveLoopStarting"), "Receive loop starting.");

            private static readonly Action<ILogger, double, Exception?> _startingServerTimeoutTimer =
                LoggerMessage.Define<double>(LogLevel.Debug, new EventId(48, "StartingServerTimeoutTimer"), "Starting server timeout timer. Duration: {ServerTimeout:0.00}ms");

            private static readonly Action<ILogger, Exception?> _notUsingServerTimeout =
                LoggerMessage.Define(LogLevel.Debug, new EventId(49, "NotUsingServerTimeout"), "Not using server timeout because the transport inherently tracks server availability.");

            private static readonly Action<ILogger, Exception> _serverDisconnectedWithError =
                LoggerMessage.Define(LogLevel.Error, new EventId(50, "ServerDisconnectedWithError"), "The server connection was terminated with an error.");

            private static readonly Action<ILogger, Exception?> _invokingClosedEventHandler =
                LoggerMessage.Define(LogLevel.Debug, new EventId(51, "InvokingClosedEventHandler"), "Invoking the Closed event handler.");

            private static readonly Action<ILogger, Exception?> _stopping =
                LoggerMessage.Define(LogLevel.Debug, new EventId(52, "Stopping"), "Stopping HubConnection.");

            private static readonly Action<ILogger, Exception?> _terminatingReceiveLoop =
                LoggerMessage.Define(LogLevel.Debug, new EventId(53, "TerminatingReceiveLoop"), "Terminating receive loop.");

            private static readonly Action<ILogger, Exception?> _waitingForReceiveLoopToTerminate =
                LoggerMessage.Define(LogLevel.Debug, new EventId(54, "WaitingForReceiveLoopToTerminate"), "Waiting for the receive loop to terminate.");

            private static readonly Action<ILogger, string, Exception?> _unableToSendCancellation =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(55, "UnableToSendCancellation"), "Unable to send cancellation for invocation '{InvocationId}'. The connection is inactive.");

            private static readonly Action<ILogger, long, Exception?> _processingMessage =
                LoggerMessage.Define<long>(LogLevel.Debug, new EventId(56, "ProcessingMessage"), "Processing {MessageLength} byte message from server.");

            private static readonly Action<ILogger, string?, string, Exception> _argumentBindingFailure =
                LoggerMessage.Define<string?, string>(LogLevel.Error, new EventId(57, "ArgumentBindingFailure"), "Failed to bind arguments received in invocation '{InvocationId}' of '{MethodName}'.");

            private static readonly Action<ILogger, string, Exception?> _removingHandlers =
               LoggerMessage.Define<string>(LogLevel.Debug, new EventId(58, "RemovingHandlers"), "Removing handlers for client method '{MethodName}'.");

            private static readonly Action<ILogger, string, Exception?> _sendingMessageGeneric =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(59, "SendingMessageGeneric"), "Sending {MessageType} message.");

            private static readonly Action<ILogger, string, Exception?> _messageSentGeneric =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(60, "MessageSentGeneric"), "Sending {MessageType} message completed.");

            private static readonly Action<ILogger, Exception?> _acquiredConnectionLockForPing =
                LoggerMessage.Define(LogLevel.Trace, new EventId(61, "AcquiredConnectionLockForPing"), "Acquired the Connection Lock in order to ping the server.");

            private static readonly Action<ILogger, Exception?> _unableToAcquireConnectionLockForPing =
                LoggerMessage.Define(LogLevel.Trace, new EventId(62, "UnableToAcquireConnectionLockForPing"), "Skipping ping because a send is already in progress.");

            private static readonly Action<ILogger, string, Exception?> _startingStream =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(63, "StartingStream"), "Initiating stream '{StreamId}'.");

            private static readonly Action<ILogger, string, Exception?> _sendingStreamItem =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(64, "StreamItemSent"), "Sending item for stream '{StreamId}'.");

            private static readonly Action<ILogger, string, Exception?> _cancelingStream =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(65, "CancelingStream"), "Stream '{StreamId}' has been canceled by client.");

            private static readonly Action<ILogger, string, Exception?> _completingStream =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(66, "CompletingStream"), "Sending completion message for stream '{StreamId}'.");

            private static readonly Action<ILogger, HubConnectionState, HubConnectionState, HubConnectionState, Exception?> _stateTransitionFailed =
                LoggerMessage.Define<HubConnectionState, HubConnectionState, HubConnectionState>(LogLevel.Error, new EventId(67, "StateTransitionFailed"), "The HubConnection failed to transition from the {ExpectedState} state to the {NewState} state because it was actually in the {ActualState} state.");

            private static readonly Action<ILogger, Exception?> _reconnecting =
                LoggerMessage.Define(LogLevel.Information, new EventId(68, "Reconnecting"), "HubConnection reconnecting.");

            private static readonly Action<ILogger, Exception> _reconnectingWithError =
                LoggerMessage.Define(LogLevel.Error, new EventId(69, "ReconnectingWithError"), "HubConnection reconnecting due to an error.");

            private static readonly Action<ILogger, long, TimeSpan, Exception?> _reconnected =
                LoggerMessage.Define<long, TimeSpan>(LogLevel.Information, new EventId(70, "Reconnected"), "HubConnection reconnected successfully after {ReconnectAttempts} attempts and {ElapsedTime} elapsed.");

            private static readonly Action<ILogger, long, TimeSpan, Exception?> _reconnectAttemptsExhausted =
                LoggerMessage.Define<long, TimeSpan>(LogLevel.Information, new EventId(71, "ReconnectAttemptsExhausted"), "Reconnect retries have been exhausted after {ReconnectAttempts} failed attempts and {ElapsedTime} elapsed. Disconnecting.");

            private static readonly Action<ILogger, long, TimeSpan, Exception?> _awaitingReconnectRetryDelay =
                LoggerMessage.Define<long, TimeSpan>(LogLevel.Trace, new EventId(72, "AwaitingReconnectRetryDelay"), "Reconnect attempt number {ReconnectAttempts} will start in {RetryDelay}.");

            private static readonly Action<ILogger, Exception> _reconnectAttemptFailed =
                LoggerMessage.Define(LogLevel.Trace, new EventId(73, "ReconnectAttemptFailed"), "Reconnect attempt failed.");

            private static readonly Action<ILogger, Exception> _errorDuringReconnectingEvent =
                LoggerMessage.Define(LogLevel.Error, new EventId(74, "ErrorDuringReconnectingEvent"), "An exception was thrown in the handler for the Reconnecting event.");

            private static readonly Action<ILogger, Exception> _errorDuringReconnectedEvent =
                LoggerMessage.Define(LogLevel.Error, new EventId(75, "ErrorDuringReconnectedEvent"), "An exception was thrown in the handler for the Reconnected event.");

            private static readonly Action<ILogger, Exception> _errorDuringNextRetryDelay  =
                LoggerMessage.Define(LogLevel.Error, new EventId(76, "ErrorDuringNextRetryDelay"), $"An exception was thrown from {nameof(IRetryPolicy)}.{nameof(IRetryPolicy.NextRetryDelay)}().");

            private static readonly Action<ILogger, Exception?> _firstReconnectRetryDelayNull =
                LoggerMessage.Define(LogLevel.Warning, new EventId(77, "FirstReconnectRetryDelayNull"), "Connection not reconnecting because the IRetryPolicy returned null on the first reconnect attempt.");

            private static readonly Action<ILogger, Exception?> _reconnectingStoppedDuringRetryDelay =
                LoggerMessage.Define(LogLevel.Trace, new EventId(78, "ReconnectingStoppedDueToStateChangeDuringRetryDelay"), "Connection stopped during reconnect delay. Done reconnecting.");

            private static readonly Action<ILogger, Exception?> _reconnectingStoppedDuringReconnectAttempt =
                LoggerMessage.Define(LogLevel.Trace, new EventId(79, "ReconnectingStoppedDueToStateChangeDuringReconnectAttempt"), "Connection stopped during reconnect attempt. Done reconnecting.");

            private static readonly Action<ILogger, HubConnectionState, HubConnectionState, Exception?> _attemptingStateTransition =
                LoggerMessage.Define<HubConnectionState, HubConnectionState>(LogLevel.Trace, new EventId(80, "AttemptingStateTransition"), "The HubConnection is attempting to transition from the {ExpectedState} state to the {NewState} state.");

            private static readonly Action<ILogger, Exception> _errorInvalidHandshakeResponse =
                LoggerMessage.Define(LogLevel.Error, new EventId(81, "ErrorInvalidHandshakeResponse"), "Received an invalid handshake response.");

            private static readonly Action<ILogger, double, Exception> _errorHandshakeTimedOut =
                LoggerMessage.Define<double>(LogLevel.Error, new EventId(82, "ErrorHandshakeTimedOut"), "The handshake timed out after {HandshakeTimeoutSeconds} seconds.");

            private static readonly Action<ILogger, Exception> _errorHandshakeCanceled =
                LoggerMessage.Define(LogLevel.Error, new EventId(83, "ErrorHandshakeCanceled"), "The handshake was canceled by the client.");

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

            public static void RegisteringInvocation(ILogger logger, string invocationId)
            {
                _registeringInvocation(logger, invocationId, null);
            }

            public static void IssuingInvocation(ILogger logger, string invocationId, string returnType, string methodName, object?[] args)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    var argsList = args == null ? string.Empty : string.Join(", ", args.Select(a => a?.GetType().FullName ?? "(null)"));
                    _issuingInvocation(logger, invocationId, returnType, methodName, argsList, null);
                }
            }

            public static void SendingMessage(ILogger logger, HubMessage message)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    if (message is HubInvocationMessage invocationMessage)
                    {
                        _sendingMessage(logger, message.GetType().Name, invocationMessage.InvocationId, null);
                    }
                    else
                    {
                        _sendingMessageGeneric(logger, message.GetType().Name, null);
                    }
                }
            }

            public static void MessageSent(ILogger logger, HubMessage message)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    if (message is HubInvocationMessage invocationMessage)
                    {
                        _messageSent(logger, message.GetType().Name, invocationMessage.InvocationId, null);
                    }
                    else
                    {
                        _messageSentGeneric(logger, message.GetType().Name, null);
                    }
                }
            }

            public static void FailedToSendInvocation(ILogger logger, string invocationId, Exception exception)
            {
                _failedToSendInvocation(logger, invocationId, exception);
            }

            public static void ReceivedInvocation(ILogger logger, string? invocationId, string methodName, object?[] args)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    var argsList = args == null ? string.Empty : string.Join(", ", args.Select(a => a?.GetType().FullName ?? "(null)"));
                    _receivedInvocation(logger, invocationId, methodName, argsList, null);
                }
            }

            public static void DroppedCompletionMessage(ILogger logger, string invocationId)
            {
                _droppedCompletionMessage(logger, invocationId, null);
            }

            public static void DroppedStreamMessage(ILogger logger, string invocationId)
            {
                _droppedStreamMessage(logger, invocationId, null);
            }

            public static void ShutdownConnection(ILogger logger)
            {
                _shutdownConnection(logger, null);
            }

            public static void ShutdownWithError(ILogger logger, Exception exception)
            {
                _shutdownWithError(logger, exception);
            }

            public static void RemovingInvocation(ILogger logger, string invocationId)
            {
                _removingInvocation(logger, invocationId, null);
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

            public static void Stopped(ILogger logger)
            {
                _stopped(logger, null);
            }

            public static void InvocationAlreadyInUse(ILogger logger, string invocationId)
            {
                _invocationAlreadyInUse(logger, invocationId, null);
            }

            public static void ReceivedUnexpectedResponse(ILogger logger, string invocationId)
            {
                _receivedUnexpectedResponse(logger, invocationId, null);
            }

            public static void HubProtocol(ILogger logger, string hubProtocol, int version)
            {
                _hubProtocol(logger, hubProtocol, version, null);
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

            public static void ReceivedPing(ILogger logger)
            {
                _receivedPing(logger, null);
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

            public static void HandshakeComplete(ILogger logger)
            {
                _handshakeComplete(logger, null);
            }

            public static void RegisteringHandler(ILogger logger, string methodName)
            {
                _registeringHandler(logger, methodName, null);
            }

            public static void RemovingHandlers(ILogger logger, string methodName)
            {
                _removingHandlers(logger, methodName, null);
            }

            public static void Starting(ILogger logger)
            {
                _starting(logger, null);
            }

            public static void ErrorStartingConnection(ILogger logger, Exception ex)
            {
                _errorStartingConnection(logger, ex);
            }

            public static void Started(ILogger logger)
            {
                _started(logger, null);
            }

            public static void SendingCancellation(ILogger logger, string invocationId)
            {
                _sendingCancellation(logger, invocationId, null);
            }

            public static void CancelingOutstandingInvocations(ILogger logger)
            {
                _cancelingOutstandingInvocations(logger, null);
            }

            public static void ReceiveLoopStarting(ILogger logger)
            {
                _receiveLoopStarting(logger, null);
            }

            public static void StartingServerTimeoutTimer(ILogger logger, TimeSpan serverTimeout)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    _startingServerTimeoutTimer(logger, serverTimeout.TotalMilliseconds, null);
                }
            }

            public static void NotUsingServerTimeout(ILogger logger)
            {
                _notUsingServerTimeout(logger, null);
            }

            public static void ServerDisconnectedWithError(ILogger logger, Exception ex)
            {
                _serverDisconnectedWithError(logger, ex);
            }

            public static void InvokingClosedEventHandler(ILogger logger)
            {
                _invokingClosedEventHandler(logger, null);
            }

            public static void Stopping(ILogger logger)
            {
                _stopping(logger, null);
            }

            public static void TerminatingReceiveLoop(ILogger logger)
            {
                _terminatingReceiveLoop(logger, null);
            }

            public static void WaitingForReceiveLoopToTerminate(ILogger logger)
            {
                _waitingForReceiveLoopToTerminate(logger, null);
            }

            public static void ProcessingMessage(ILogger logger, long length)
            {
                _processingMessage(logger, length, null);
            }

            public static void WaitingOnConnectionLock(ILogger logger, string memberName, string filePath, int lineNumber)
            {
                _waitingOnConnectionLock(logger, memberName, filePath, lineNumber, null);
            }

            public static void ReleasingConnectionLock(ILogger logger, string memberName, string filePath, int lineNumber)
            {
                _releasingConnectionLock(logger, memberName, filePath, lineNumber, null);
            }

            public static void UnableToSendCancellation(ILogger logger, string invocationId)
            {
                _unableToSendCancellation(logger, invocationId, null);
            }

            public static void ArgumentBindingFailure(ILogger logger, string? invocationId, string target, Exception exception)
            {
                _argumentBindingFailure(logger, invocationId, target, exception);
            }

            public static void AcquiredConnectionLockForPing(ILogger logger)
            {
                _acquiredConnectionLockForPing(logger, null);
            }

            public static void UnableToAcquireConnectionLockForPing(ILogger logger)
            {
                _unableToAcquireConnectionLockForPing(logger, null);
            }

            public static void StartingStream(ILogger logger, string streamId)
            {
                _startingStream(logger, streamId, null);
            }

            public static void SendingStreamItem(ILogger logger, string streamId)
            {
                _sendingStreamItem(logger, streamId, null);
            }

            public static void CancelingStream(ILogger logger, string streamId)
            {
                _cancelingStream(logger, streamId, null);
            }

            public static void CompletingStream(ILogger logger, string streamId)
            {
                _completingStream(logger, streamId, null);
            }

            public static void StateTransitionFailed(ILogger logger, HubConnectionState expectedState, HubConnectionState newState, HubConnectionState actualState)
            {
                _stateTransitionFailed(logger, expectedState, newState, actualState, null);
            }

            public static void Reconnecting(ILogger logger)
            {
                _reconnecting(logger, null);
            }

            public static void ReconnectingWithError(ILogger logger, Exception exception)
            {
                _reconnectingWithError(logger, exception);
            }

            public static void Reconnected(ILogger logger, long reconnectAttempts, TimeSpan elapsedTime)
            {
                _reconnected(logger, reconnectAttempts, elapsedTime, null);
            }

            public static void ReconnectAttemptsExhausted(ILogger logger, long reconnectAttempts, TimeSpan elapsedTime)
            {
                _reconnectAttemptsExhausted(logger, reconnectAttempts, elapsedTime, null);
            }

            public static void AwaitingReconnectRetryDelay(ILogger logger, long reconnectAttempts, TimeSpan retryDelay)
            {
                _awaitingReconnectRetryDelay(logger, reconnectAttempts, retryDelay, null);
            }

            public static void ReconnectAttemptFailed(ILogger logger, Exception exception)
            {
                _reconnectAttemptFailed(logger, exception);
            }

            public static void ErrorDuringReconnectingEvent(ILogger logger, Exception exception)
            {
                _errorDuringReconnectingEvent(logger, exception);
            }

            public static void ErrorDuringReconnectedEvent(ILogger logger, Exception exception)
            {
                _errorDuringReconnectedEvent(logger, exception);
            }

            public static void ErrorDuringNextRetryDelay(ILogger logger, Exception exception)
            {
                _errorDuringNextRetryDelay(logger, exception);
            }

            public static void FirstReconnectRetryDelayNull(ILogger logger)
            {
                _firstReconnectRetryDelayNull(logger, null);
            }

            public static void ReconnectingStoppedDuringRetryDelay(ILogger logger)
            {
                _reconnectingStoppedDuringRetryDelay(logger, null);
            }

            public static void ReconnectingStoppedDuringReconnectAttempt(ILogger logger)
            {
                _reconnectingStoppedDuringReconnectAttempt(logger, null);
            }

            public static void AttemptingStateTransition(ILogger logger, HubConnectionState expectedState, HubConnectionState newState)
            {
                _attemptingStateTransition(logger, expectedState, newState, null);
            }

            public static void ErrorInvalidHandshakeResponse(ILogger logger, Exception exception)
            {
                _errorInvalidHandshakeResponse(logger, exception);
            }

            public static void ErrorHandshakeTimedOut(ILogger logger, TimeSpan handshakeTimeout, Exception exception)
            {
                _errorHandshakeTimedOut(logger, handshakeTimeout.TotalSeconds, exception);
            }

            public static void ErrorHandshakeCanceled(ILogger logger, Exception exception)
            {
                _errorHandshakeCanceled(logger, exception);
            }
        }
    }
}


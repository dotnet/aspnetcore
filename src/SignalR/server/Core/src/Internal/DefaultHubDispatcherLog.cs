// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal static partial class DefaultHubDispatcherLog
{
    [LoggerMessage(1, LogLevel.Debug, "Received hub invocation: {InvocationMessage}.", EventName = "ReceivedHubInvocation")]
    public static partial void ReceivedHubInvocation(ILogger logger, InvocationMessage invocationMessage);

    [LoggerMessage(2, LogLevel.Debug, "Received unsupported message of type '{MessageType}'.", EventName = "UnsupportedMessageReceived")]
    public static partial void UnsupportedMessageReceived(ILogger logger, string messageType);

    [LoggerMessage(3, LogLevel.Debug, "Unknown hub method '{HubMethod}'.", EventName = "UnknownHubMethod")]
    public static partial void UnknownHubMethod(ILogger logger, string hubMethod);

    // 4, OutboundChannelClosed - removed

    [LoggerMessage(5, LogLevel.Debug, "Failed to invoke '{HubMethod}' because user is unauthorized.", EventName = "HubMethodNotAuthorized")]
    public static partial void HubMethodNotAuthorized(ILogger logger, string hubMethod);

    public static void StreamingResult(ILogger logger, string invocationId, ObjectMethodExecutor objectMethodExecutor)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            var resultType = objectMethodExecutor.AsyncResultType ?? objectMethodExecutor.MethodReturnType;
            StreamingResult(logger, invocationId, resultType.FullName);
        }
    }

    [LoggerMessage(6, LogLevel.Trace, "InvocationId {InvocationId}: Streaming result of type '{ResultType}'.", EventName = "StreamingResult", SkipEnabledCheck = true)]
    private static partial void StreamingResult(ILogger logger, string invocationId, string? resultType);

    public static void SendingResult(ILogger logger, string? invocationId, ObjectMethodExecutor objectMethodExecutor)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            var resultType = objectMethodExecutor.AsyncResultType ?? objectMethodExecutor.MethodReturnType;
            SendingResult(logger, invocationId, resultType.FullName);
        }
    }

    [LoggerMessage(7, LogLevel.Trace, "InvocationId {InvocationId}: Sending result of type '{ResultType}'.", EventName = "SendingResult", SkipEnabledCheck = true)]
    private static partial void SendingResult(ILogger logger, string? invocationId, string? resultType);

    [LoggerMessage(8, LogLevel.Error, "Failed to invoke hub method '{HubMethod}'.", EventName = "FailedInvokingHubMethod")]
    public static partial void FailedInvokingHubMethod(ILogger logger, string hubMethod, Exception exception);

    [LoggerMessage(9, LogLevel.Trace, "'{HubName}' hub method '{HubMethod}' is bound.", EventName = "HubMethodBound")]
    public static partial void HubMethodBound(ILogger logger, string hubName, string hubMethod);

    [LoggerMessage(10, LogLevel.Debug, "Canceling stream for invocation {InvocationId}.", EventName = "CancelStream")]
    public static partial void CancelStream(ILogger logger, string invocationId);

    [LoggerMessage(11, LogLevel.Debug, "CancelInvocationMessage received unexpectedly.", EventName = "UnexpectedCancel")]
    public static partial void UnexpectedCancel(ILogger logger);

    [LoggerMessage(12, LogLevel.Debug, "Received stream hub invocation: {InvocationMessage}.", EventName = "ReceivedStreamHubInvocation")]
    public static partial void ReceivedStreamHubInvocation(ILogger logger, StreamInvocationMessage invocationMessage);

    [LoggerMessage(13, LogLevel.Debug, "A streaming method was invoked with a non-streaming invocation : {InvocationMessage}.", EventName = "StreamingMethodCalledWithInvoke")]
    public static partial void StreamingMethodCalledWithInvoke(ILogger logger, HubMethodInvocationMessage invocationMessage);

    [LoggerMessage(14, LogLevel.Debug, "A non-streaming method was invoked with a streaming invocation : {InvocationMessage}.", EventName = "NonStreamingMethodCalledWithStream")]
    public static partial void NonStreamingMethodCalledWithStream(ILogger logger, HubMethodInvocationMessage invocationMessage);

    [LoggerMessage(15, LogLevel.Debug, "A streaming method returned a value that cannot be used to build enumerator {HubMethod}.", EventName = "InvalidReturnValueFromStreamingMethod")]
    public static partial void InvalidReturnValueFromStreamingMethod(ILogger logger, string hubMethod);

    public static void ReceivedStreamItem(ILogger logger, StreamItemMessage message)
        => ReceivedStreamItem(logger, message.InvocationId);

    [LoggerMessage(16, LogLevel.Trace, "Received item for stream '{StreamId}'.", EventName = "ReceivedStreamItem")]
    private static partial void ReceivedStreamItem(ILogger logger, string? streamId);

    [LoggerMessage(17, LogLevel.Trace, "Creating streaming parameter channel '{StreamId}'.", EventName = "StartingParameterStream")]
    public static partial void StartingParameterStream(ILogger logger, string streamId);

    public static void CompletingStream(ILogger logger, CompletionMessage message)
        => CompletingStream(logger, message.InvocationId);

    [LoggerMessage(18, LogLevel.Trace, "Stream '{StreamId}' has been completed by client.", EventName = "CompletingStream")]
    private static partial void CompletingStream(ILogger logger, string? streamId);

    public static void ClosingStreamWithBindingError(ILogger logger, CompletionMessage message)
        => ClosingStreamWithBindingError(logger, message.InvocationId, message.Error);

    [LoggerMessage(19, LogLevel.Debug, "Stream '{StreamId}' closed with error '{Error}'.", EventName = "ClosingStreamWithBindingError")]
    private static partial void ClosingStreamWithBindingError(ILogger logger, string? streamId, string? error);

    // Retired [20]UnexpectedStreamCompletion, replaced with more generic [24]UnexpectedCompletion

    [LoggerMessage(21, LogLevel.Debug, "StreamItemMessage received unexpectedly.", EventName = "UnexpectedStreamItem")]
    public static partial void UnexpectedStreamItem(ILogger logger);

    [LoggerMessage(22, LogLevel.Debug, "Parameters to hub method '{HubMethod}' are incorrect.", EventName = "InvalidHubParameters")]
    public static partial void InvalidHubParameters(ILogger logger, string hubMethod, Exception exception);

    [LoggerMessage(23, LogLevel.Debug, "Invocation ID '{InvocationId}' is already in use.", EventName = "InvocationIdInUse")]
    public static partial void InvocationIdInUse(ILogger logger, string InvocationId);

    [LoggerMessage(24, LogLevel.Debug, "CompletionMessage for invocation ID '{InvocationId}' received unexpectedly.", EventName = "UnexpectedCompletion")]
    public static partial void UnexpectedCompletion(ILogger logger, string invocationId);

    [LoggerMessage(25, LogLevel.Error, "Invocation ID {InvocationId}: Failed while sending stream items from hub method {HubMethod}.", EventName = "FailedStreaming")]
    public static partial void FailedStreaming(ILogger logger, string invocationId, string hubMethod, Exception exception);

    [LoggerMessage(26, LogLevel.Trace, "Dropping {MessageType} with ID '{InvocationId}'.", EventName = "DroppingMessage")]
    public static partial void DroppingMessage(ILogger logger, string messageType, string? invocationId);

    [LoggerMessage(27, LogLevel.Trace, "Received AckMessage with Sequence ID '{SequenceId}'.", EventName = "ReceivedAckMessage")]
     public static partial void ReceivedAckMessage(ILogger logger, long sequenceId);

    [LoggerMessage(28, LogLevel.Trace, "Received SequenceMessage with Sequence ID '{SequenceId}'.", EventName = "ReceivedSequenceMessage")]
    public static partial void ReceivedSequenceMessage(ILogger logger, long sequenceId);
}

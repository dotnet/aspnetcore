// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed partial class KestrelTrace : ILogger
{
    public void Http2ConnectionError(string connectionId, Http2ConnectionErrorException ex)
    {
        Http2Log.Http2ConnectionError(_http2Logger, connectionId, ex);
    }

    public void Http2StreamError(string connectionId, Http2StreamErrorException ex)
    {
        Http2Log.Http2StreamError(_http2Logger, connectionId, ex);
    }

    public void HPackDecodingError(string connectionId, int streamId, Exception ex)
    {
        Http2Log.HPackDecodingError(_http2Logger, connectionId, streamId, ex);
    }

    public void Http2StreamResetAbort(string traceIdentifier, Http2ErrorCode error, ConnectionAbortedException abortReason)
    {
        Http2Log.Http2StreamResetAbort(_http2Logger, traceIdentifier, error, abortReason);
    }

    public void Http2ConnectionClosing(string connectionId)
    {
        Http2Log.Http2ConnectionClosing(_http2Logger, connectionId);
    }

    public void Http2FrameReceived(string connectionId, Http2Frame frame)
    {
        if (_http2Logger.IsEnabled(LogLevel.Trace))
        {
            Http2Log.Http2FrameReceived(_http2Logger, connectionId, frame.Type, frame.StreamId, frame.PayloadLength, frame.ShowFlags());
        }
    }

    public void HPackEncodingError(string connectionId, int streamId, Exception ex)
    {
        Http2Log.HPackEncodingError(_http2Logger, connectionId, streamId, ex);
    }

    public void Http2MaxConcurrentStreamsReached(string connectionId)
    {
        Http2Log.Http2MaxConcurrentStreamsReached(_http2Logger, connectionId);
    }

    public void Http2ConnectionClosed(string connectionId, int highestOpenedStreamId)
    {
        Http2Log.Http2ConnectionClosed(_http2Logger, connectionId, highestOpenedStreamId);
    }

    public void Http2FrameSending(string connectionId, Http2Frame frame)
    {
        if (_http2Logger.IsEnabled(LogLevel.Trace))
        {
            Http2Log.Http2FrameSending(_http2Logger, connectionId, frame.Type, frame.StreamId, frame.PayloadLength, frame.ShowFlags());
        }
    }

    public void Http2QueueOperationsExceeded(string connectionId, ConnectionAbortedException ex)
    {
        Http2Log.Http2QueueOperationsExceeded(_http2Logger, connectionId, ex);
    }

    public void Http2UnexpectedDataRemaining(int streamId, string connectionId)
    {
        Http2Log.Http2UnexpectedDataRemaining(_http2Logger, streamId, connectionId);
    }

    public void Http2ConnectionQueueProcessingCompleted(string connectionId)
    {
        Http2Log.Http2ConnectionQueueProcessingCompleted(_http2Logger, connectionId);
    }

    public void Http2UnexpectedConnectionQueueError(string connectionId, Exception ex)
    {
        Http2Log.Http2UnexpectedConnectionQueueError(_http2Logger, connectionId, ex);
    }

    public void Http2TooManyEnhanceYourCalms(string connectionId, int count)
    {
        Http2Log.Http2TooManyEnhanceYourCalms(_http2Logger, connectionId, count);
    }

    public void Http2FlowControlQueueOperationsExceeded(string connectionId, int count)
    {
        Http2Log.Http2FlowControlQueueOperationsExceeded(_http2Logger, connectionId, count);
    }

    public void Http2FlowControlQueueMaximumTooLow(string connectionId, int expected, int actual)
    {
        Http2Log.Http2FlowControlQueueMaximumTooLow(_http2Logger, connectionId, expected, actual);
    }

    private static partial class Http2Log
    {
        [LoggerMessage(29, LogLevel.Debug, @"Connection id ""{ConnectionId}"": HTTP/2 connection error.", EventName = "Http2ConnectionError")]
        public static partial void Http2ConnectionError(ILogger logger, string connectionId, Http2ConnectionErrorException ex);

        [LoggerMessage(30, LogLevel.Debug, @"Connection id ""{ConnectionId}"": HTTP/2 stream error.", EventName = "Http2StreamError")]
        public static partial void Http2StreamError(ILogger logger, string connectionId, Http2StreamErrorException ex);

        [LoggerMessage(31, LogLevel.Debug, @"Connection id ""{ConnectionId}"": HPACK decoding error while decoding headers for stream ID {StreamId}.", EventName = "HPackDecodingError")]
        public static partial void HPackDecodingError(ILogger logger, string connectionId, int streamId, Exception ex);

        [LoggerMessage(35, LogLevel.Debug, @"Trace id ""{TraceIdentifier}"": HTTP/2 stream error ""{error}"". A Reset is being sent to the stream.", EventName = "Http2StreamResetAbort")]
        public static partial void Http2StreamResetAbort(ILogger logger, string traceIdentifier, Http2ErrorCode error, ConnectionAbortedException abortReason);

        [LoggerMessage(36, LogLevel.Debug, @"Connection id ""{ConnectionId}"" is closing.", EventName = "Http2ConnectionClosing")]
        public static partial void Http2ConnectionClosing(ILogger logger, string connectionId);

        [LoggerMessage(37, LogLevel.Trace, @"Connection id ""{ConnectionId}"" received {type} frame for stream ID {id} with length {length} and flags {flags}.", EventName = "Http2FrameReceived", SkipEnabledCheck = true)]
        public static partial void Http2FrameReceived(ILogger logger, string connectionId, Http2FrameType type, int id, int length, object flags);

        [LoggerMessage(38, LogLevel.Information, @"Connection id ""{ConnectionId}"": HPACK encoding error while encoding headers for stream ID {StreamId}.", EventName = "HPackEncodingError")]
        public static partial void HPackEncodingError(ILogger logger, string connectionId, int streamId, Exception ex);

        [LoggerMessage(40, LogLevel.Debug, @"Connection id ""{ConnectionId}"" reached the maximum number of concurrent HTTP/2 streams allowed.", EventName = "Http2MaxConcurrentStreamsReached")]
        public static partial void Http2MaxConcurrentStreamsReached(ILogger logger, string connectionId);

        [LoggerMessage(48, LogLevel.Debug, @"Connection id ""{ConnectionId}"" is closed. The last processed stream ID was {HighestOpenedStreamId}.", EventName = "Http2ConnectionClosed")]
        public static partial void Http2ConnectionClosed(ILogger logger, string connectionId, int highestOpenedStreamId);

        [LoggerMessage(49, LogLevel.Trace, @"Connection id ""{ConnectionId}"" sending {type} frame for stream ID {id} with length {length} and flags {flags}.", EventName = "Http2FrameSending", SkipEnabledCheck = true)]
        public static partial void Http2FrameSending(ILogger logger, string connectionId, Http2FrameType type, int id, int length, object flags);

        [LoggerMessage(60, LogLevel.Critical, @"Connection id ""{ConnectionId}"" exceeded the output operations maximum queue size.", EventName = "Http2QueueOperationsExceeded")]
        public static partial void Http2QueueOperationsExceeded(ILogger logger, string connectionId, ConnectionAbortedException ex);

        [LoggerMessage(61, LogLevel.Critical, @"Stream {StreamId} on connection id ""{ConnectionId}"" observed an unexpected state where the streams output ended with data still remaining in the pipe.", EventName = "Http2UnexpectedDataRemaining")]
        public static partial void Http2UnexpectedDataRemaining(ILogger logger, int streamId, string connectionId);

        [LoggerMessage(62, LogLevel.Debug, @"The connection queue processing loop for {ConnectionId} completed.", EventName = "Http2ConnectionQueueProcessingCompleted")]
        public static partial void Http2ConnectionQueueProcessingCompleted(ILogger logger, string connectionId);

        [LoggerMessage(63, LogLevel.Critical, @"The event loop in connection {ConnectionId} failed unexpectedly.", EventName = "Http2UnexpectedConnectionQueueError")]
        public static partial void Http2UnexpectedConnectionQueueError(ILogger logger, string connectionId, Exception ex);

        // IDs prior to 64 are reserved for back compat (the various KestrelTrace loggers used to share a single sequence)

        [LoggerMessage(64, LogLevel.Debug, @"Connection id ""{ConnectionId}"" aborted since at least {Count} ENHANCE_YOUR_CALM responses were recorded per second.", EventName = "Http2TooManyEnhanceYourCalms")]
        public static partial void Http2TooManyEnhanceYourCalms(ILogger logger, string connectionId, int count);

        [LoggerMessage(65, LogLevel.Debug, @"Connection id ""{ConnectionId}"" exceeded the output flow control maximum queue size of {Count}.", EventName = "Http2FlowControlQueueOperationsExceeded")]
        public static partial void Http2FlowControlQueueOperationsExceeded(ILogger logger, string connectionId, int count);

        [LoggerMessage(66, LogLevel.Debug, @"Connection id ""{ConnectionId}"" configured maximum flow control queue size {Actual} is less than the maximum streams per connection {Expected}. Increasing configured value to {Expected}.", EventName = "Http2FlowControlQueueMaximumTooLow")]
        public static partial void Http2FlowControlQueueMaximumTooLow(ILogger logger, string connectionId, int expected, int actual);
    }
}

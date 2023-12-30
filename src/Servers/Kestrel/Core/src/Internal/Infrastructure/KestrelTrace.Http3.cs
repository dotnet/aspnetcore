// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed partial class KestrelTrace : ILogger
{
    public void Http3ConnectionError(string connectionId, Http3ConnectionErrorException ex)
    {
        Http3Log.Http3ConnectionError(_http3Logger, connectionId, ex);
    }

    public void Http3ConnectionClosing(string connectionId)
    {
        Http3Log.Http3ConnectionClosing(_http3Logger, connectionId);
    }

    public void Http3ConnectionClosed(string connectionId, long? highestOpenedStreamId)
    {
        Http3Log.Http3ConnectionClosed(_http3Logger, connectionId, highestOpenedStreamId);
    }

    public void Http3StreamAbort(string traceIdentifier, Http3ErrorCode error, ConnectionAbortedException abortReason)
    {
        if (_http3Logger.IsEnabled(LogLevel.Debug))
        {
            Http3Log.Http3StreamAbort(_http3Logger, traceIdentifier, Http3Formatting.ToFormattedErrorCode(error), abortReason);
        }
    }

    public void Http3FrameReceived(string connectionId, long streamId, Http3RawFrame frame)
    {
        if (_http3Logger.IsEnabled(LogLevel.Trace))
        {
            Http3Log.Http3FrameReceived(_http3Logger, connectionId, Http3Formatting.ToFormattedType(frame.Type), streamId, frame.Length);
        }
    }

    public void Http3FrameSending(string connectionId, long streamId, Http3RawFrame frame)
    {
        if (_http3Logger.IsEnabled(LogLevel.Trace))
        {
            Http3Log.Http3FrameSending(_http3Logger, connectionId, Http3Formatting.ToFormattedType(frame.Type), streamId, frame.Length);
        }
    }

    public void Http3OutboundControlStreamError(string connectionId, Exception ex)
    {
        Http3Log.Http3OutboundControlStreamError(_http3Logger, connectionId, ex);
    }

    public void QPackDecodingError(string connectionId, long streamId, Exception ex)
    {
        Http3Log.QPackDecodingError(_http3Logger, connectionId, streamId, ex);
    }

    public void QPackEncodingError(string connectionId, long streamId, Exception ex)
    {
        Http3Log.QPackEncodingError(_http3Logger, connectionId, streamId, ex);
    }

    public void Http3GoAwayStreamId(string connectionId, long goAwayStreamId)
    {
        Http3Log.Http3GoAwayStreamId(_http3Logger, connectionId, goAwayStreamId);
    }

    private static partial class Http3Log
    {
        [LoggerMessage(42, LogLevel.Debug, @"Connection id ""{ConnectionId}"": HTTP/3 connection error.", EventName = "Http3ConnectionError")]
        public static partial void Http3ConnectionError(ILogger logger, string connectionId, Http3ConnectionErrorException ex);

        [LoggerMessage(43, LogLevel.Debug, @"Connection id ""{ConnectionId}"" is closing.", EventName = "Http3ConnectionClosing")]
        public static partial void Http3ConnectionClosing(ILogger logger, string connectionId);

        [LoggerMessage(44, LogLevel.Debug, @"Connection id ""{ConnectionId}"" is closed. The last processed stream ID was {HighestOpenedStreamId}.", EventName = "Http3ConnectionClosed")]
        public static partial void Http3ConnectionClosed(ILogger logger, string connectionId, long? highestOpenedStreamId);

        [LoggerMessage(45, LogLevel.Debug, @"Trace id ""{TraceIdentifier}"": HTTP/3 stream error ""{error}"". An abort is being sent to the stream.", EventName = "Http3StreamAbort", SkipEnabledCheck = true)]
        public static partial void Http3StreamAbort(ILogger logger, string traceIdentifier, string error, ConnectionAbortedException abortReason);

        [LoggerMessage(46, LogLevel.Trace, @"Connection id ""{ConnectionId}"" received {type} frame for stream ID {id} with length {length}.", EventName = "Http3FrameReceived", SkipEnabledCheck = true)]
        public static partial void Http3FrameReceived(ILogger logger, string connectionId, string type, long id, long length);

        [LoggerMessage(47, LogLevel.Trace, @"Connection id ""{ConnectionId}"" sending {type} frame for stream ID {id} with length {length}.", EventName = "Http3FrameSending", SkipEnabledCheck = true)]
        public static partial void Http3FrameSending(ILogger logger, string connectionId, string type, long id, long length);

        [LoggerMessage(50, LogLevel.Debug, @"Connection id ""{ConnectionId}"": Unexpected error when initializing outbound control stream.", EventName = "Http3OutboundControlStreamError")]
        public static partial void Http3OutboundControlStreamError(ILogger logger, string connectionId, Exception ex);

        [LoggerMessage(51, LogLevel.Debug, @"Connection id ""{ConnectionId}"": QPACK decoding error while decoding headers for stream ID {StreamId}.", EventName = "QPackDecodingError")]
        public static partial void QPackDecodingError(ILogger logger, string connectionId, long streamId, Exception ex);

        [LoggerMessage(52, LogLevel.Information, @"Connection id ""{ConnectionId}"": QPACK encoding error while encoding headers for stream ID {StreamId}.", EventName = "QPackEncodingError")]
        public static partial void QPackEncodingError(ILogger logger, string connectionId, long streamId, Exception ex);

        [LoggerMessage(53, LogLevel.Debug, @"Connection id ""{ConnectionId}"": GOAWAY stream ID {GoAwayStreamId}.", EventName = "Http3GoAwayHighestOpenedStreamId")]
        public static partial void Http3GoAwayStreamId(ILogger logger, string connectionId, long goAwayStreamId);

        // IDs prior to 64 are reserved for back compat (the various KestrelTrace loggers used to share a single sequence)
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Net.Http.HPack;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Testing
{
    internal class CompositeKestrelTrace : IKestrelTrace
    {
        private readonly IKestrelTrace _trace1;
        private readonly IKestrelTrace _trace2;

        public CompositeKestrelTrace(IKestrelTrace kestrelTrace, KestrelTrace kestrelTrace1)
        {
            _trace1 = kestrelTrace;
            _trace2 = kestrelTrace1;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _trace1.Log(logLevel, eventId, state, exception, formatter);
            _trace2.Log(logLevel, eventId, state, exception, formatter);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _trace1.IsEnabled(logLevel) || _trace2.IsEnabled(logLevel);
        }

        public void ConnectionAccepted(string connectionId)
        {
            _trace1.ConnectionAccepted(connectionId);
            _trace2.ConnectionAccepted(connectionId);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return _trace1.BeginScope(state);
        }

        public void ConnectionStart(string connectionId)
        {
            _trace1.ConnectionStart(connectionId);
            _trace2.ConnectionStart(connectionId);
        }

        public void ConnectionStop(string connectionId)
        {
            _trace1.ConnectionStop(connectionId);
            _trace2.ConnectionStop(connectionId);
        }

        public void ConnectionPause(string connectionId)
        {
            _trace1.ConnectionPause(connectionId);
            _trace2.ConnectionPause(connectionId);
        }

        public void ConnectionResume(string connectionId)
        {
            _trace1.ConnectionResume(connectionId);
            _trace2.ConnectionResume(connectionId);
        }

        public void ConnectionRejected(string connectionId)
        {
            _trace1.ConnectionRejected(connectionId);
            _trace2.ConnectionRejected(connectionId);
        }

        public void ConnectionKeepAlive(string connectionId)
        {
            _trace1.ConnectionKeepAlive(connectionId);
            _trace2.ConnectionKeepAlive(connectionId);
        }

        public void ConnectionDisconnect(string connectionId)
        {
            _trace1.ConnectionDisconnect(connectionId);
            _trace2.ConnectionDisconnect(connectionId);
        }

        public void RequestProcessingError(string connectionId, Exception ex)
        {
            _trace1.RequestProcessingError(connectionId, ex);
            _trace2.RequestProcessingError(connectionId, ex);
        }

        public void ConnectionHeadResponseBodyWrite(string connectionId, long count)
        {
            _trace1.ConnectionHeadResponseBodyWrite(connectionId, count);
            _trace2.ConnectionHeadResponseBodyWrite(connectionId, count);
        }

        public void NotAllConnectionsClosedGracefully()
        {
            _trace1.NotAllConnectionsClosedGracefully();
            _trace2.NotAllConnectionsClosedGracefully();
        }

        public void ConnectionBadRequest(string connectionId, Microsoft.AspNetCore.Http.BadHttpRequestException ex)
        {
            _trace1.ConnectionBadRequest(connectionId, ex);
            _trace2.ConnectionBadRequest(connectionId, ex);
        }

        public void ApplicationError(string connectionId, string traceIdentifier, Exception ex)
        {
            _trace1.ApplicationError(connectionId, traceIdentifier, ex);
            _trace2.ApplicationError(connectionId, traceIdentifier, ex);
        }

        public void NotAllConnectionsAborted()
        {
            _trace1.NotAllConnectionsAborted();
            _trace2.NotAllConnectionsAborted();
        }

        public void HeartbeatSlow(TimeSpan heartbeatDuration, TimeSpan interval, DateTimeOffset now)
        {
            _trace1.HeartbeatSlow(heartbeatDuration, interval, now);
            _trace2.HeartbeatSlow(heartbeatDuration, interval, now);
        }

        public void ApplicationNeverCompleted(string connectionId)
        {
            _trace1.ApplicationNeverCompleted(connectionId);
            _trace2.ApplicationNeverCompleted(connectionId);
        }

        public void RequestBodyStart(string connectionId, string traceIdentifier)
        {
            _trace1.RequestBodyStart(connectionId, traceIdentifier);
            _trace2.RequestBodyStart(connectionId, traceIdentifier);
        }

        public void RequestBodyDone(string connectionId, string traceIdentifier)
        {
            _trace1.RequestBodyDone(connectionId, traceIdentifier);
            _trace2.RequestBodyDone(connectionId, traceIdentifier);
        }

        public void RequestBodyNotEntirelyRead(string connectionId, string traceIdentifier)
        {
            _trace1.RequestBodyNotEntirelyRead(connectionId, traceIdentifier);
            _trace2.RequestBodyNotEntirelyRead(connectionId, traceIdentifier);
        }

        public void RequestBodyDrainTimedOut(string connectionId, string traceIdentifier)
        {
            _trace1.RequestBodyDrainTimedOut(connectionId, traceIdentifier);
            _trace2.RequestBodyDrainTimedOut(connectionId, traceIdentifier);
        }

        public void RequestBodyMinimumDataRateNotSatisfied(string connectionId, string traceIdentifier, double rate)
        {
            _trace1.RequestBodyMinimumDataRateNotSatisfied(connectionId, traceIdentifier, rate);
            _trace2.RequestBodyMinimumDataRateNotSatisfied(connectionId, traceIdentifier, rate);
        }

        public void ResponseMinimumDataRateNotSatisfied(string connectionId, string traceIdentifier)
        {
            _trace1.ResponseMinimumDataRateNotSatisfied(connectionId, traceIdentifier);
            _trace2.ResponseMinimumDataRateNotSatisfied(connectionId, traceIdentifier);
        }

        public void ApplicationAbortedConnection(string connectionId, string traceIdentifier)
        {
            _trace1.ApplicationAbortedConnection(connectionId, traceIdentifier);
            _trace2.ApplicationAbortedConnection(connectionId, traceIdentifier);
        }

        public void Http2ConnectionError(string connectionId, Http2ConnectionErrorException ex)
        {
            _trace1.Http2ConnectionError(connectionId, ex);
            _trace2.Http2ConnectionError(connectionId, ex);
        }

        public void Http2StreamError(string connectionId, Http2StreamErrorException ex)
        {
            _trace1.Http2StreamError(connectionId, ex);
            _trace2.Http2StreamError(connectionId, ex);
        }

        public void HPackDecodingError(string connectionId, int streamId, HPackDecodingException ex)
        {
            _trace1.HPackDecodingError(connectionId, streamId, ex);
            _trace2.HPackDecodingError(connectionId, streamId, ex);
        }

        public void HPackEncodingError(string connectionId, int streamId, HPackEncodingException ex)
        {
            _trace1.HPackEncodingError(connectionId, streamId, ex);
            _trace2.HPackEncodingError(connectionId, streamId, ex);
        }

        public void Http2StreamResetAbort(string traceIdentifier, Http2ErrorCode error, ConnectionAbortedException abortReason)
        {
            _trace1.Http2StreamResetAbort(traceIdentifier, error, abortReason);
            _trace2.Http2StreamResetAbort(traceIdentifier, error, abortReason);
        }

        public void Http2ConnectionClosing(string connectionId)
        {
            _trace1.Http2ConnectionClosing(connectionId);
            _trace2.Http2ConnectionClosing(connectionId);
        }

        public void Http2ConnectionClosed(string connectionId, int highestOpenedStreamId)
        {
            _trace1.Http2ConnectionClosed(connectionId, highestOpenedStreamId);
            _trace2.Http2ConnectionClosed(connectionId, highestOpenedStreamId);
        }

        public void Http2FrameReceived(string connectionId, Http2Frame frame)
        {
            _trace1.Http2FrameReceived(connectionId, frame);
            _trace2.Http2FrameReceived(connectionId, frame);
        }

        public void Http2FrameSending(string connectionId, Http2Frame frame)
        {
            _trace1.Http2FrameSending(connectionId, frame);
            _trace2.Http2FrameSending(connectionId, frame);
        }

        public void Http2MaxConcurrentStreamsReached(string connectionId)
        {
            _trace1.Http2MaxConcurrentStreamsReached(connectionId);
            _trace2.Http2MaxConcurrentStreamsReached(connectionId);
        }

        public void InvalidResponseHeaderRemoved()
        {
            _trace1.InvalidResponseHeaderRemoved();
            _trace2.InvalidResponseHeaderRemoved();
        }

        public void Http3ConnectionError(string connectionId, Http3ConnectionException ex)
        {
            _trace1.Http3ConnectionError(connectionId, ex);
            _trace2.Http3ConnectionError(connectionId, ex);
        }

        public void Http3ConnectionClosing(string connectionId)
        {
            _trace1.Http3ConnectionClosing(connectionId);
            _trace2.Http3ConnectionClosing(connectionId);
        }

        public void Http3ConnectionClosed(string connectionId, long highestOpenedStreamId)
        {
            _trace1.Http3ConnectionClosed(connectionId, highestOpenedStreamId);
            _trace2.Http3ConnectionClosed(connectionId, highestOpenedStreamId);
        }

        public void Http3StreamResetAbort(string traceIdentifier, Http3ErrorCode error, ConnectionAbortedException abortReason)
        {
            _trace1.Http3StreamResetAbort(traceIdentifier, error, abortReason);
            _trace2.Http3StreamResetAbort(traceIdentifier, error, abortReason);
        }
    }
}

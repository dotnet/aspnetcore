// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http.HPack;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal interface IKestrelTrace : ILogger
    {
        void ConnectionAccepted(string connectionId);

        void ConnectionStart(string connectionId);

        void ConnectionStop(string connectionId);

        void ConnectionPause(string connectionId);

        void ConnectionResume(string connectionId);

        void ConnectionRejected(string connectionId);

        void ConnectionKeepAlive(string connectionId);

        void ConnectionDisconnect(string connectionId);

        void RequestProcessingError(string connectionId, Exception ex);

        void ConnectionHeadResponseBodyWrite(string connectionId, long count);

        void NotAllConnectionsClosedGracefully();

        void ConnectionBadRequest(string connectionId, BadHttpRequestException ex);

        void ApplicationError(string connectionId, string traceIdentifier, Exception ex);

        void NotAllConnectionsAborted();

        void HeartbeatSlow(TimeSpan heartbeatDuration, TimeSpan interval, DateTimeOffset now);

        void ApplicationNeverCompleted(string connectionId);

        void RequestBodyStart(string connectionId, string traceIdentifier);

        void RequestBodyDone(string connectionId, string traceIdentifier);

        void RequestBodyNotEntirelyRead(string connectionId, string traceIdentifier);

        void RequestBodyDrainTimedOut(string connectionId, string traceIdentifier);

        void RequestBodyMinimumDataRateNotSatisfied(string connectionId, string traceIdentifier, double rate);

        void ResponseMinimumDataRateNotSatisfied(string connectionId, string traceIdentifier);

        void ApplicationAbortedConnection(string connectionId, string traceIdentifier);

        void Http2ConnectionError(string connectionId, Http2ConnectionErrorException ex);

        void Http2ConnectionClosing(string connectionId);

        void Http2ConnectionClosed(string connectionId, int highestOpenedStreamId);

        void Http2StreamError(string connectionId, Http2StreamErrorException ex);

        void Http2StreamResetAbort(string traceIdentifier, Http2ErrorCode error, ConnectionAbortedException abortReason);

        void HPackDecodingError(string connectionId, int streamId, HPackDecodingException ex);

        void HPackEncodingError(string connectionId, int streamId, HPackEncodingException ex);

        void Http2FrameReceived(string connectionId, Http2Frame frame);

        void Http2FrameSending(string connectionId, Http2Frame frame);
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public interface IKestrelTrace : ILogger
    {
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

        void HeartbeatSlow(TimeSpan interval, DateTimeOffset now);

        void ApplicationNeverCompleted(string connectionId);

        void RequestBodyStart(string connectionId, string traceIdentifier);

        void RequestBodyDone(string connectionId, string traceIdentifier);

        void RequestBodyNotEntirelyRead(string connectionId, string traceIdentifier);

        void RequestBodyDrainTimedOut(string connectionId, string traceIdentifier);

        void RequestBodyMininumDataRateNotSatisfied(string connectionId, string traceIdentifier, double rate);

        void ResponseMininumDataRateNotSatisfied(string connectionId, string traceIdentifier);

        void Http2ConnectionError(string connectionId, Http2ConnectionErrorException ex);

        void Http2StreamError(string connectionId, Http2StreamErrorException ex);

        void HPackDecodingError(string connectionId, int streamId, HPackDecodingException ex);
    }
}

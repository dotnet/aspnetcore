// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public interface IKestrelTrace : ILogger
    {
        void ConnectionStart(string connectionId);

        void ConnectionStop(string connectionId);

        void ConnectionPause(string connectionId);

        void ConnectionResume(string connectionId);

        void ConnectionKeepAlive(string connectionId);

        void ConnectionDisconnect(string connectionId);

        void RequestProcessingError(string connectionId, Exception ex);

        void ConnectionDisconnectedWrite(string connectionId, int count, Exception ex);

        void ConnectionHeadResponseBodyWrite(string connectionId, long count);

        void NotAllConnectionsClosedGracefully();

        void ConnectionBadRequest(string connectionId, BadHttpRequestException ex);

        void ApplicationError(string connectionId, string traceIdentifier, Exception ex);

        void NotAllConnectionsAborted();

        void TimerSlow(TimeSpan interval, DateTimeOffset now);

        void ApplicationNeverCompleted(string connectionId);
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    internal interface IQuicTrace : ILogger
    {
        void AcceptedConnection(BaseConnectionContext connection);
        void AcceptedStream(QuicStreamContext streamContext);
        void ConnectedStream(QuicStreamContext streamContext);
        void ConnectionError(BaseConnectionContext connection, Exception ex);
        void ConnectionAborted(BaseConnectionContext connection, long errorCode, Exception ex);
        void ConnectionAbort(BaseConnectionContext connection, long errorCode, string reason);
        void StreamError(QuicStreamContext streamContext, Exception ex);
        void StreamPause(QuicStreamContext streamContext);
        void StreamResume(QuicStreamContext streamContext);
        void StreamShutdownWrite(QuicStreamContext streamContext, string reason);
        void StreamAborted(QuicStreamContext streamContext, long errorCode, Exception ex);
        void StreamAbort(QuicStreamContext streamContext, long errorCode, string reason);
        void StreamAbortRead(QuicStreamContext streamContext, long errorCode, string reason);
        void StreamAbortWrite(QuicStreamContext streamContext, long errorCode, string reason);
    }
}

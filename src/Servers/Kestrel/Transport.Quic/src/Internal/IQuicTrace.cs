// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    internal interface IQuicTrace : ILogger
    {
        void AcceptedConnection(BaseConnectionContext connection);
        void AcceptedStream(QuicStreamContext streamContext);
        void ConnectionError(BaseConnectionContext connection, Exception ex);
        void StreamError(QuicStreamContext streamContext, Exception ex);
        void StreamPause(QuicStreamContext streamContext);
        void StreamResume(QuicStreamContext streamContext);
        void StreamShutdownWrite(QuicStreamContext streamContext, string reason);
        void StreamAbort(QuicStreamContext streamContext, string reason);
    }
}

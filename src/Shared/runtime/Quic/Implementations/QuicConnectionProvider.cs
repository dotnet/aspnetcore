// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations
{
    internal abstract class QuicConnectionProvider : IDisposable
    {
        internal abstract bool Connected { get; }

        internal abstract IPEndPoint LocalEndPoint { get; }

        internal abstract IPEndPoint RemoteEndPoint { get; }

        internal abstract ValueTask ConnectAsync(CancellationToken cancellationToken = default);

        internal abstract QuicStreamProvider OpenUnidirectionalStream();

        internal abstract QuicStreamProvider OpenBidirectionalStream();

        internal abstract long GetRemoteAvailableUnidirectionalStreamCount();

        internal abstract long GetRemoteAvailableBidirectionalStreamCount();

        internal abstract ValueTask<QuicStreamProvider> AcceptStreamAsync(CancellationToken cancellationToken = default);

        internal abstract System.Net.Security.SslApplicationProtocol NegotiatedApplicationProtocol { get; }

        internal abstract ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default);

        public abstract void Dispose();
    }
}

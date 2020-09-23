// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations
{
    internal abstract class QuicConnectionProvider : IDisposable
    {
        internal abstract bool Connected { get; }

        internal abstract IPEndPoint LocalEndPoint { get; }

        internal abstract EndPoint RemoteEndPoint { get; }

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

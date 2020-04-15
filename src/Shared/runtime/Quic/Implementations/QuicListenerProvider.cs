// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations
{
    internal abstract class QuicListenerProvider : IDisposable
    {
        internal abstract IPEndPoint ListenEndPoint { get; }

        internal abstract ValueTask<QuicConnectionProvider> AcceptConnectionAsync(CancellationToken cancellationToken = default);

        internal abstract void Start();

        internal abstract void Close();

        public abstract void Dispose();
    }
}

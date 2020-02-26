// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

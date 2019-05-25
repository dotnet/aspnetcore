// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport
{
    internal class InMemoryTransportFactory : IConnectionListenerFactory, IConnectionListener
    {
        private Channel<ConnectionContext> _acceptQueue = Channel.CreateUnbounded<ConnectionContext>();

        public EndPoint EndPoint { get; set; }

        public void AddConnection(ConnectionContext connection)
        {
            _acceptQueue.Writer.TryWrite(connection);
        }

        public async ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
        {
            if (await _acceptQueue.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_acceptQueue.Reader.TryRead(out var item))
                {
                    return item;
                }
            }

            return null;

        }

        public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint)
        {
            EndPoint = endpoint;

            // The endpoint isn't important
            return new ValueTask<IConnectionListener>(this);
        }

        public ValueTask DisposeAsync()
        {
            return StopAsync(default);
        }

        public ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            _acceptQueue.Writer.TryComplete();

            // TODO: Graceful shutdown
            return default;
        }
    }
}

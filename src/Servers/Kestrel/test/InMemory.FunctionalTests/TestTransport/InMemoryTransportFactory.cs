// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;

internal class InMemoryTransportFactory : IConnectionListenerFactory, IConnectionListener
{
    private readonly Channel<ConnectionContext> _acceptQueue = Channel.CreateUnbounded<ConnectionContext>();

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

    public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        EndPoint = endpoint;

        return new ValueTask<IConnectionListener>(this);
    }

    public ValueTask DisposeAsync()
    {
        return UnbindAsync(default);
    }

    public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        _acceptQueue.Writer.TryComplete();

        return default;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

internal sealed class CircuitClientProxy : ISingleClientProxy
{
    public CircuitClientProxy()
    {
        Connected = false;
    }

    public CircuitClientProxy(ISingleClientProxy clientProxy, string connectionId)
    {
        Transfer(clientProxy, connectionId);
    }

    public bool Connected { get; private set; }

    public string ConnectionId { get; private set; }

    public ISingleClientProxy Client { get; private set; }

    public void Transfer(ISingleClientProxy clientProxy, string connectionId)
    {
        Client = clientProxy ?? throw new ArgumentNullException(nameof(clientProxy));
        ConnectionId = connectionId ?? throw new ArgumentNullException(nameof(connectionId));
        Connected = true;
    }

    public void SetDisconnected()
    {
        Connected = false;
    }

    public Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken = default)
    {
        if (Client == null)
        {
            throw new InvalidOperationException($"{nameof(SendCoreAsync)} cannot be invoked with an offline client.");
        }

        return Client.SendCoreAsync(method, args, cancellationToken);
    }

    public Task<T> InvokeCoreAsync<T>(string method, object[] args, CancellationToken cancellationToken = default)
    {
        if (Client == null)
        {
            throw new InvalidOperationException($"{nameof(InvokeCoreAsync)} cannot be invoked with an offline client.");
        }
        return Client.InvokeCoreAsync<T>(method, args, cancellationToken);
    }
}

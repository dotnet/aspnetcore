// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed class ConnectionReference
{
    private readonly long _id;
    private readonly WeakReference<KestrelConnection> _weakReference;
    private readonly TransportConnectionManager _transportConnectionManager;

    public ConnectionReference(long id, KestrelConnection connection, TransportConnectionManager transportConnectionManager)
    {
        _id = id;

        _weakReference = new WeakReference<KestrelConnection>(connection);
        ConnectionId = connection.TransportConnection.ConnectionId;

        _transportConnectionManager = transportConnectionManager;
    }

    public string ConnectionId { get; }

    public bool TryGetConnection([NotNullWhen(true)] out KestrelConnection? connection)
    {
        return _weakReference.TryGetTarget(out connection);
    }

    public void StopTransportTracking()
    {
        _transportConnectionManager.StopTracking(_id);
    }
}

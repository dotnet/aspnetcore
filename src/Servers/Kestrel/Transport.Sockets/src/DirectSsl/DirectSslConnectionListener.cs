// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;

internal sealed class DirectSslConnectionListener : IConnectionListener
{
    public EndPoint EndPoint => throw new NotImplementedException();

    public ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

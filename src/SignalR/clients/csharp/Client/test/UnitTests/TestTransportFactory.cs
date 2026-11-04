// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

internal class TestTransportFactory : ITransportFactory
{
    private readonly ITransport _transport;

    public TestTransportFactory(ITransport transport)
    {
        _transport = transport;
    }

    public ITransport CreateTransport(HttpTransportType availableServerTransports, bool useStatefulReconnect)
    {
        return _transport;
    }
}

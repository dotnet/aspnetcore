// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sockets.BindTests;

public class SocketTransportFactoryTests
{
    [Fact]
    public async Task ThrowsNotImplementedExceptionWhenBindingToUriEndPoint()
    {
        var socketTransportFactory = new SocketTransportFactory(Options.Create(new SocketTransportOptions()), new LoggerFactory());
        await Assert.ThrowsAsync<NotImplementedException>(async () => await socketTransportFactory.BindAsync(new UriEndPoint(new Uri("http://127.0.0.1:5554"))));
    }

    [Fact]
    public void CanBind_PlainIPEndPoint_ReturnsTrue()
    {
        var socketTransportFactory = new SocketTransportFactory(Options.Create(new SocketTransportOptions()), new LoggerFactory());
        Assert.True(socketTransportFactory.CanBind(new IPEndPoint(IPAddress.Loopback, 0)));
    }

    [Fact]
    public void CanBind_DirectTlsEndpoint_ReturnsFalse()
    {
#pragma warning disable ASPNETCORE_DIRECTTLS_001
        var directTlsEndpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
#pragma warning restore ASPNETCORE_DIRECTTLS_001
        var socketTransportFactory = new SocketTransportFactory(Options.Create(new SocketTransportOptions()), new LoggerFactory());
        Assert.False(socketTransportFactory.CanBind(directTlsEndpoint));
    }
}


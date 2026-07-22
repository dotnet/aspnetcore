// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

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
        // DirectTlsEndpoint derives from IPEndPoint, so the socket transport must explicitly refuse it;
        // otherwise a DirectTls endpoint could be bound as a plaintext socket if this factory is tried first.
#pragma warning disable ASPNETCORE_DIRECTTLS_001 // Experimental API
        var directTlsEndpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);
#pragma warning restore ASPNETCORE_DIRECTTLS_001
        var socketTransportFactory = new SocketTransportFactory(Options.Create(new SocketTransportOptions()), new LoggerFactory());
        Assert.False(socketTransportFactory.CanBind(directTlsEndpoint));
    }
}


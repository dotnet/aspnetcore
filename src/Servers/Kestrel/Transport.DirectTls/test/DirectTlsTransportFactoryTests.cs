// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Tests;

public class DirectTlsTransportFactoryTests
{
    private static DirectTlsTransportFactory CreateFactory()
    {
        return new DirectTlsTransportFactory(
            Options.Create(new DirectTlsTransportOptions()),
            NullLoggerFactory.Instance);
    }

    [Fact]
    public void CanBind_DirectTlsEndpoint_ReturnsTrue()
    {
        var factory = CreateFactory();

        Assert.True(factory.CanBind(new DirectTlsEndpoint(IPAddress.Loopback, 0)));
    }

    [Fact]
    public void CanBind_PlainIPEndPoint_ReturnsFalse()
    {
        var factory = CreateFactory();

        Assert.False(factory.CanBind(new IPEndPoint(IPAddress.Loopback, 0)));
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task BindAsync_NonDirectTlsEndpoint_Throws()
    {
        var factory = CreateFactory();

        await Assert.ThrowsAsync<NotSupportedException>(
            () => factory.BindAsync(new IPEndPoint(IPAddress.Loopback, 0)).AsTask());
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task BindAsync_WithoutServerCertificate_Throws()
    {
        var factory = CreateFactory();
        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.BindAsync(endpoint).AsTask());
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task BindAsync_DelayCertificateMode_Throws()
    {
        var factory = CreateFactory();
        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0)
        {
            Options =
            {
                ServerCertificate = TestResources.GetTestCertificate(),
                ClientCertificateMode = ClientCertificateMode.DelayCertificate,
            },
        };

        await Assert.ThrowsAsync<NotSupportedException>(
            () => factory.BindAsync(endpoint).AsTask());
    }
}

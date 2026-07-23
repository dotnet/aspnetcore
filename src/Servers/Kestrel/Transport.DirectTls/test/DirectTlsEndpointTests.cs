// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Tests;

public class DirectTlsEndpointTests
{
    [Fact]
    public void AddressPortConstructor_CreatesDefaultOptions()
    {
        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 5001);

        Assert.Equal(IPAddress.Loopback, endpoint.Address);
        Assert.Equal(5001, endpoint.Port);
        Assert.NotNull(endpoint.Options);
    }

    [Fact]
    public void AddressPortOptionsConstructor_UsesProvidedOptions()
    {
        var options = new DirectTlsEndpointOptions();

        var endpoint = new DirectTlsEndpoint(IPAddress.Any, 443, options);

        Assert.Equal(IPAddress.Any, endpoint.Address);
        Assert.Equal(443, endpoint.Port);
        Assert.Same(options, endpoint.Options);
    }

    [Fact]
    public void IPEndPointConstructor_CopiesAddressAndPort()
    {
        var options = new DirectTlsEndpointOptions();
        var source = new IPEndPoint(IPAddress.Loopback, 8443);

        var endpoint = new DirectTlsEndpoint(source, options);

        Assert.Equal(source.Address, endpoint.Address);
        Assert.Equal(source.Port, endpoint.Port);
        Assert.Same(options, endpoint.Options);
    }

    [Fact]
    public void AddressPortOptionsConstructor_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DirectTlsEndpoint(IPAddress.Loopback, 5001, null!));
    }

    [Fact]
    public void IPEndPointConstructor_NullEndpoint_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DirectTlsEndpoint(null!, new DirectTlsEndpointOptions()));
    }

    [Fact]
    public void Options_HaveExpectedDefaults()
    {
        var options = new DirectTlsEndpointOptions();

        Assert.Null(options.ServerCertificate);
        Assert.Null(options.ServerCertificateSelector);
        Assert.Equal(SslProtocols.None, options.SslProtocols);
        Assert.Equal(ClientCertificateMode.NoCertificate, options.ClientCertificateMode);
        Assert.Null(options.ClientCertificateValidation);
        Assert.Null(options.TlsClientHelloBytesCallback);
        Assert.Null(options.WorkerCount);
    }

    [Fact]
    public void EndpointOptions_WorkerCount_OverrideRoundTripsAndRejectsNonPositive()
    {
        var options = new DirectTlsEndpointOptions();

        Assert.Null(options.WorkerCount);

        options.WorkerCount = 3;
        Assert.Equal(3, options.WorkerCount);

        options.WorkerCount = null;
        Assert.Null(options.WorkerCount);

        Assert.Throws<ArgumentOutOfRangeException>(() => options.WorkerCount = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.WorkerCount = -1);
    }

    [Fact]
    public void TransportOptions_WorkerCount_DefaultsToProcessorBasedHeuristic()
    {
        var options = new DirectTlsTransportOptions();

        var expected = Environment.ProcessorCount <= 32 ? Math.Min(Environment.ProcessorCount, 16) : Environment.ProcessorCount / 2;
        Assert.Equal(expected, DirectTlsTransportOptions.DefaultWorkerCount);
        Assert.Equal(DirectTlsTransportOptions.DefaultWorkerCount, options.WorkerCount);

        options.WorkerCount = 2;
        Assert.Equal(2, options.WorkerCount);
    }
}

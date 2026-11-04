// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Tests;

[Collection(nameof(NoParallelCollection))]
public class QuicTransportFactoryTests : TestApplicationErrorLoggerLoggedTest
{
    [ConditionalFact]
    [MsQuicSupported]
    public async Task BindAsync_NoFeatures_Error()
    {
        // Arrange
        var quicTransportOptions = new QuicTransportOptions();
        var quicTransportFactory = new QuicTransportFactory(NullLoggerFactory.Instance, Options.Create(quicTransportOptions));

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => quicTransportFactory.BindAsync(new IPEndPoint(0, 0), features: null, cancellationToken: CancellationToken.None).AsTask()).DefaultTimeout();

        // Assert
        Assert.Equal("Couldn't find HTTPS configuration for QUIC transport.", ex.Message);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task BindAsync_NoApplicationProtocols_Error()
    {
        // Arrange
        var quicTransportOptions = new QuicTransportOptions();
        var quicTransportFactory = new QuicTransportFactory(NullLoggerFactory.Instance, Options.Create(quicTransportOptions));
        var features = new FeatureCollection();
        features.Set(new TlsConnectionCallbackOptions());

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => quicTransportFactory.BindAsync(new IPEndPoint(0, 0), features: features, cancellationToken: CancellationToken.None).AsTask()).DefaultTimeout();

        // Assert
        Assert.Equal("No application protocols specified for QUIC transport.", ex.Message);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task BindAsync_SslServerAuthenticationOptions_Success()
    {
        // Arrange
        var quicTransportOptions = new QuicTransportOptions();
        var quicTransportFactory = new QuicTransportFactory(NullLoggerFactory.Instance, Options.Create(quicTransportOptions));
        var features = new FeatureCollection();
        features.Set(new TlsConnectionCallbackOptions
        {
            ApplicationProtocols = new List<SslApplicationProtocol>
            {
                SslApplicationProtocol.Http3
            }
        });

        // Act & Assert
        await quicTransportFactory.BindAsync(new IPEndPoint(0, 0), features: features, cancellationToken: CancellationToken.None).AsTask().DefaultTimeout();
    }
}

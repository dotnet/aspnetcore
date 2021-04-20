// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Tests
{
    public class QuicTransportFactoryTests
    {
        [Fact]
        public async Task BindAsync_NoFeature_Error()
        {
            // Arrange
            var quicTransportOptions = new QuicTransportOptions();
            var quicTransportFactory = new QuicTransportFactory(NullLoggerFactory.Instance, Options.Create(quicTransportOptions));

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => quicTransportFactory.BindAsync(new IPEndPoint(0, 0), features: null, cancellationToken: CancellationToken.None).AsTask()).DefaultTimeout();

            // Assert
            Assert.Equal("Couldn't find HTTPS configuration for QUIC transport.", ex.Message);
        }

        [Fact]
        public async Task BindAsync_NoServerCertificate_Error()
        {
            // Arrange
            var quicTransportOptions = new QuicTransportOptions();
            var quicTransportFactory = new QuicTransportFactory(NullLoggerFactory.Instance, Options.Create(quicTransportOptions));
            var features = new FeatureCollection();
            features.Set(new SslServerAuthenticationOptions());

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => quicTransportFactory.BindAsync(new IPEndPoint(0, 0), features: features, cancellationToken: CancellationToken.None).AsTask()).DefaultTimeout();

            // Assert
            Assert.Equal("SslServerAuthenticationOptions.ServerCertificate must be configured with a value.", ex.Message);
        }
    }
}

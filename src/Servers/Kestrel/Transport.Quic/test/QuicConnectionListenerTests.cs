// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Quic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Tests
{
    public class QuicConnectionListenerTests : TestApplicationErrorLoggerLoggedTest
    {
        [ConditionalFact]
        [MsQuicSupported]
        public async Task AcceptAsync_AfterUnbind_Error()
        {
            // Arrange
            await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

            // Act
            await connectionListener.UnbindAsync().DefaultTimeout();

            // Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => connectionListener.AcceptAndAddFeatureAsync().AsTask()).DefaultTimeout();
        }

        [ConditionalFact]
        [MsQuicSupported]
        public async Task AcceptAsync_ClientCreatesConnection_ServerAccepts()
        {
            // Arrange
            await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

            // Act
            var acceptTask = connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

            var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);

            using var quicConnection = new QuicConnection(QuicImplementationProviders.MsQuic, options);
            await quicConnection.ConnectAsync().DefaultTimeout();

            // Assert
            await using var connection = await acceptTask.DefaultTimeout();
            Assert.False(connection.ConnectionClosed.IsCancellationRequested);

            await connection.DisposeAsync().AsTask().DefaultTimeout();

            // ConnectionClosed isn't triggered because the server initiated close.
            Assert.False(connection.ConnectionClosed.IsCancellationRequested);
        }
    }
}

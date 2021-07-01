// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Quic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Tests
{
    public class QuicConnectionContextTests
    {
        private static readonly byte[] TestData = Encoding.UTF8.GetBytes("Hello world");

        [ConditionalFact]
        [MsQuicSupported]
        public async Task AcceptAsync_ClientStartsAndStopsUnidirectionStream_ServerAccepts()
        {
            // Arrange
            await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory();

            var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
            using var quicConnection = new QuicConnection(QuicImplementationProviders.MsQuic, options);
            await quicConnection.ConnectAsync().DefaultTimeout();

            await using var serverConnection = await connectionListener.AcceptAsync().DefaultTimeout();

            // Act
            var acceptTask = serverConnection.AcceptAsync();

            await using var clientStream = quicConnection.OpenUnidirectionalStream();
            await clientStream.WriteAsync(TestData);

            await using var serverStream = await acceptTask.DefaultTimeout();

            // Assert
            Assert.NotNull(serverStream);
            Assert.False(serverStream.ConnectionClosed.IsCancellationRequested);

            var closedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            serverStream.ConnectionClosed.Register(() => closedTcs.SetResult());

            var read = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
            Assert.Equal(TestData, read.Buffer.ToArray());
            serverStream.Transport.Input.AdvanceTo(read.Buffer.End);

            clientStream.Shutdown();

            read = await serverStream.Transport.Input.ReadAsync().DefaultTimeout();
            Assert.True(read.IsCompleted);

            await closedTcs.Task.DefaultTimeout();
        }

        [ConditionalFact]
        [MsQuicSupported]
        public async Task AcceptAsync_ClientStartsAndStopsBidirectionStream_ServerAccepts()
        {
            // Arrange
            await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory();

            var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
            using var quicConnection = new QuicConnection(QuicImplementationProviders.MsQuic, options);
            await quicConnection.ConnectAsync().DefaultTimeout();

            var serverConnection = await connectionListener.AcceptAsync().DefaultTimeout();

            // Act
            var acceptTask = serverConnection.AcceptAsync();

            await using var clientStream = quicConnection.OpenBidirectionalStream();
            await clientStream.WriteAsync(TestData);

            await using var serverStream = await acceptTask.DefaultTimeout();
            await serverStream.Transport.Output.WriteAsync(TestData);

            // Assert
            Assert.NotNull(serverStream);
            Assert.False(serverStream.ConnectionClosed.IsCancellationRequested);

            var closedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            serverStream.ConnectionClosed.Register(() => closedTcs.SetResult());

            var read = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
            Assert.Equal(TestData, read.Buffer.ToArray());
            serverStream.Transport.Input.AdvanceTo(read.Buffer.End);

            var data = new List<byte>();
            var buffer = new byte[1024];
            var readCount = 0;
            while ((readCount = await clientStream.ReadAsync(buffer).DefaultTimeout()) != -1)
            {
                data.AddRange(buffer.AsMemory(0, readCount).ToArray());
                if (data.Count == TestData.Length)
                {
                    break;
                }
            }
            Assert.Equal(TestData, data);

            clientStream.Shutdown();

            read = await serverStream.Transport.Input.ReadAsync().DefaultTimeout();
            Assert.True(read.IsCompleted);

            await closedTcs.Task.DefaultTimeout();
        }

        [ConditionalFact]
        [MsQuicSupported]
        public async Task AcceptAsync_ServerStartsAndStopsUnidirectionStream_ClientAccepts()
        {
            // Arrange
            await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory();

            var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
            using var quicConnection = new QuicConnection(QuicImplementationProviders.MsQuic, options);
            await quicConnection.ConnectAsync().DefaultTimeout();

            var serverConnection = await connectionListener.AcceptAsync().DefaultTimeout();

            // Act
            var acceptTask = quicConnection.AcceptStreamAsync();

            await using var serverStream = await serverConnection.ConnectAsync();
            await serverStream.Transport.Output.WriteAsync(TestData).DefaultTimeout();

            await using var clientStream = await acceptTask.DefaultTimeout();

            // Assert
            Assert.NotNull(clientStream);

            var data = new List<byte>();
            var buffer = new byte[1024];
            var readCount = 0;
            while ((readCount = await clientStream.ReadAsync(buffer).DefaultTimeout()) != -1)
            {
                data.AddRange(buffer.AsMemory(0, readCount).ToArray());
                if (data.Count == TestData.Length)
                {
                    break;
                }
            }
            Assert.Equal(TestData, data);

            await serverStream.Transport.Output.CompleteAsync().DefaultTimeout();

            readCount = await clientStream.ReadAsync(buffer).DefaultTimeout();
            Assert.Equal(0, readCount);
        }
    }
}

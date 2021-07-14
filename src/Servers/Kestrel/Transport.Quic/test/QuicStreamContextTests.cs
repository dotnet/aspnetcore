// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Quic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Tests
{
    public class QuicStreamContextTests : TestApplicationErrorLoggerLoggedTest
    {
        private static readonly byte[] TestData = Encoding.UTF8.GetBytes("Hello world");

        [ConditionalFact]
        [MsQuicSupported]
        public async Task BidirectionalStream_ServerReadsDataAndCompletes_GracefullyClosed()
        {
            // Arrange
            await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

            var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
            using var clientConnection = new QuicConnection(QuicImplementationProviders.MsQuic, options);
            await clientConnection.ConnectAsync().DefaultTimeout();

            await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

            // Act
            await QuicTestHelpers.CreateAndCompleteBidirectionalStreamGracefully(clientConnection, serverConnection);

            Assert.Contains(LogMessages, m => m.Message.Contains("send loop completed gracefully"));

            var quicConnectionContext = Assert.IsType<QuicConnectionContext>(serverConnection);

            Assert.Equal(1, quicConnectionContext.StreamPool.Count);
        }

        [ConditionalFact]
        [MsQuicSupported]
        public async Task BidirectionalStream_MultipleStreamsOnConnection_ReusedFromPool()
        {
            // Arrange
            await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

            var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
            using var clientConnection = new QuicConnection(QuicImplementationProviders.MsQuic, options);
            await clientConnection.ConnectAsync().DefaultTimeout();

            await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

            // Act
            var stream1 = await QuicTestHelpers.CreateAndCompleteBidirectionalStreamGracefully(clientConnection, serverConnection);
            var stream2 = await QuicTestHelpers.CreateAndCompleteBidirectionalStreamGracefully(clientConnection, serverConnection);

            Assert.Same(stream1, stream2);

            var quicConnectionContext = Assert.IsType<QuicConnectionContext>(serverConnection);
            Assert.Equal(1, quicConnectionContext.StreamPool.Count);
        }

        [ConditionalFact]
        [MsQuicSupported]
        public async Task BidirectionalStream_ClientAbortWrite_ServerReceivesAbort()
        {
            // Arrange
            await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

            var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
            using var quicConnection = new QuicConnection(QuicImplementationProviders.MsQuic, options);
            await quicConnection.ConnectAsync().DefaultTimeout();

            await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

            // Act
            await using var clientStream = quicConnection.OpenBidirectionalStream();
            await clientStream.WriteAsync(TestData).DefaultTimeout();

            await using var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();
            var readResult = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
            serverStream.Transport.Input.AdvanceTo(readResult.Buffer.End);

            var closedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            serverStream.ConnectionClosed.Register(() => closedTcs.SetResult());

            clientStream.AbortWrite((long)Http3ErrorCode.InternalError);

            // Receive abort from client.
            var ex = await Assert.ThrowsAsync<ConnectionResetException>(() => serverStream.Transport.Input.ReadAsync().AsTask()).DefaultTimeout();

            // Server completes its output.
            await serverStream.Transport.Output.CompleteAsync();

            // Assert
            Assert.Equal((long)Http3ErrorCode.InternalError, ((QuicStreamAbortedException)ex.InnerException).ErrorCode);

            var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);

            // Both send and receive loops have exited.
            await quicStreamContext._processingTask.DefaultTimeout();

            await closedTcs.Task.DefaultTimeout();
        }

        [ConditionalFact]
        [MsQuicSupported]
        public async Task ClientToServerUnidirectionalStream_ServerReadsData_GracefullyClosed()
        {
            // Arrange
            await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

            var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
            using var quicConnection = new QuicConnection(QuicImplementationProviders.MsQuic, options);
            await quicConnection.ConnectAsync().DefaultTimeout();

            await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

            // Act
            await using var clientStream = quicConnection.OpenUnidirectionalStream();
            await clientStream.WriteAsync(TestData, endStream: true).DefaultTimeout();

            await using var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();
            var readResult = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
            serverStream.Transport.Input.AdvanceTo(readResult.Buffer.End);

            // Input should be completed.
            readResult = await serverStream.Transport.Input.ReadAsync().DefaultTimeout();

            // Assert
            Assert.True(readResult.IsCompleted);

            var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);
            Assert.False(quicStreamContext.CanWrite);
            Assert.True(quicStreamContext.CanRead);

            // Both send and receive loops have exited.
            await quicStreamContext._processingTask.DefaultTimeout();
        }

        [ConditionalFact]
        [MsQuicSupported]
        public async Task ClientToServerUnidirectionalStream_ClientAbort_ServerReceivesAbort()
        {
            // Arrange
            await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

            var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
            using var quicConnection = new QuicConnection(QuicImplementationProviders.MsQuic, options);
            await quicConnection.ConnectAsync().DefaultTimeout();

            await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

            // Act
            await using var clientStream = quicConnection.OpenUnidirectionalStream();
            await clientStream.WriteAsync(TestData).DefaultTimeout();

            await using var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();
            var readResult = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
            serverStream.Transport.Input.AdvanceTo(readResult.Buffer.End);

            var closedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            serverStream.ConnectionClosed.Register(() => closedTcs.SetResult());

            clientStream.AbortWrite((long)Http3ErrorCode.InternalError);

            // Receive abort from client.
            var ex = await Assert.ThrowsAsync<ConnectionResetException>(() => serverStream.Transport.Input.ReadAsync().AsTask()).DefaultTimeout();

            // Assert
            Assert.Equal((long)Http3ErrorCode.InternalError, ((QuicStreamAbortedException)ex.InnerException).ErrorCode);

            var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);

            // Both send and receive loops have exited.
            await quicStreamContext._processingTask.DefaultTimeout();

            await closedTcs.Task.DefaultTimeout();
        }

        [ConditionalFact]
        [MsQuicSupported]
        public async Task ServerToClientUnidirectionalStream_ServerWritesDataAndCompletes_GracefullyClosed()
        {
            // Arrange
            await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

            var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
            using var quicConnection = new QuicConnection(QuicImplementationProviders.MsQuic, options);
            await quicConnection.ConnectAsync().DefaultTimeout();

            await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

            // Act
            var features = new FeatureCollection();
            features.Set<IStreamDirectionFeature>(new DefaultStreamDirectionFeature(canRead: false, canWrite: true));
            var serverStream = await serverConnection.ConnectAsync(features).DefaultTimeout();
            await serverStream.Transport.Output.WriteAsync(TestData).DefaultTimeout();

            await using var clientStream = await quicConnection.AcceptStreamAsync();

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

            await serverStream.Transport.Output.CompleteAsync();

            readCount = await clientStream.ReadAsync(buffer).DefaultTimeout();

            // Assert
            Assert.Equal(0, readCount);

            var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);
            Assert.True(quicStreamContext.CanWrite);
            Assert.False(quicStreamContext.CanRead);

            // Both send and receive loops have exited.
            await quicStreamContext._processingTask.DefaultTimeout();
        }

        [ConditionalFact]
        [MsQuicSupported]
        public async Task ServerToClientUnidirectionalStream_ServerAborts_ClientGetsAbort()
        {
            // Arrange
            await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

            var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
            using var quicConnection = new QuicConnection(QuicImplementationProviders.MsQuic, options);
            await quicConnection.ConnectAsync().DefaultTimeout();

            await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

            // Act
            var features = new FeatureCollection();
            features.Set<IStreamDirectionFeature>(new DefaultStreamDirectionFeature(canRead: false, canWrite: true));
            var serverStream = await serverConnection.ConnectAsync(features).DefaultTimeout();
            await serverStream.Transport.Output.WriteAsync(TestData).DefaultTimeout();

            await using var clientStream = await quicConnection.AcceptStreamAsync();

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

            ((IProtocolErrorCodeFeature)serverStream).Error = (long)Http3ErrorCode.InternalError;
            serverStream.Abort(new ConnectionAbortedException("Test message"));

            var ex = await Assert.ThrowsAsync<QuicStreamAbortedException>(() => clientStream.ReadAsync(buffer).AsTask()).DefaultTimeout();

            // Assert
            Assert.Equal((long)Http3ErrorCode.InternalError, ex.ErrorCode);

            var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);
            Assert.True(quicStreamContext.CanWrite);
            Assert.False(quicStreamContext.CanRead);

            // Both send and receive loops have exited.
            await quicStreamContext._processingTask.DefaultTimeout();
        }
    }
}

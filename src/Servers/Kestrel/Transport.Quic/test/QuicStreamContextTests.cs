// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Tests
{
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/35070")]
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

            Assert.Contains(TestSink.Writes, m => m.Message.Contains(@"shutting down writes because: ""The QUIC transport's send loop completed gracefully.""."));
        }

        [ConditionalFact]
        [MsQuicSupported]
        public async Task BidirectionalStream_ReadAborted_NotPooled()
        {
            // Arrange
            await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

            var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
            using var clientConnection = new QuicConnection(QuicImplementationProviders.MsQuic, options);
            await clientConnection.ConnectAsync().DefaultTimeout();

            await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

            // Act
            var clientStream = clientConnection.OpenBidirectionalStream();
            await clientStream.WriteAsync(TestData).DefaultTimeout();
            var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();
            var readResult = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
            serverStream.Transport.Input.AdvanceTo(readResult.Buffer.End);

            await clientStream.WriteAsync(TestData).DefaultTimeout();

            // Complete writing.
            await serverStream.Transport.Output.CompleteAsync();

            // Abort read-side of the stream and then complete pipe.
            // This simulates what Kestrel does when a request finishes without
            // reading the request body to the end.
            serverStream.Features.Get<IStreamAbortFeature>().AbortRead((long)Http3ErrorCode.NoError, new ConnectionAbortedException("Test message."));
            await serverStream.Transport.Input.CompleteAsync();

            var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);

            // Both send and receive loops have exited.
            await quicStreamContext._processingTask.DefaultTimeout();
            Assert.True(quicStreamContext.CanWrite);
            Assert.True(quicStreamContext.CanRead);

            await quicStreamContext.DisposeAsync();
            quicStreamContext.Dispose();

            var quicConnectionContext = Assert.IsType<QuicConnectionContext>(serverConnection);

            // Assert
            Assert.Equal(0, quicConnectionContext.StreamPool.Count);
        }

        [ConditionalTheory]
        [MsQuicSupported]
        [InlineData(1024)]
        [InlineData(1024 * 1024)]
        [InlineData(1024 * 1024 * 5)]
        public async Task BidirectionalStream_ServerWritesDataAndDisposes_ClientReadsData(int dataLength)
        {
            // Arrange
            var testData = new byte[dataLength];
            for (int i = 0; i < dataLength; i++)
            {
                testData[i] = (byte)i;
            }

            await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

            var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
            using var clientConnection = new QuicConnection(QuicImplementationProviders.MsQuic, options);
            await clientConnection.ConnectAsync().DefaultTimeout();

            await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

            // Act
            Logger.LogInformation("Client starting stream.");
            var clientStream = clientConnection.OpenBidirectionalStream();
            await clientStream.WriteAsync(TestData, endStream: true).DefaultTimeout();
            var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();

            Logger.LogInformation("Server accepted stream.");
            var readResult = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
            serverStream.Transport.Input.AdvanceTo(readResult.Buffer.End);

            // Input should be completed.
            readResult = await serverStream.Transport.Input.ReadAsync().DefaultTimeout();
            Assert.True(readResult.IsCompleted);

            Logger.LogInformation("Server sending data.");
            await serverStream.Transport.Output.WriteAsync(testData).DefaultTimeout();

            Logger.LogInformation("Server completing pipes.");
            await serverStream.Transport.Input.CompleteAsync().DefaultTimeout();
            await serverStream.Transport.Output.CompleteAsync().DefaultTimeout();

            var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);

            Logger.LogInformation("Server waiting for send and receiving loops to complete.");
            await quicStreamContext._processingTask.DefaultTimeout();
            Assert.True(quicStreamContext.CanWrite);
            Assert.True(quicStreamContext.CanRead);

            Logger.LogInformation("Server disposing stream.");
            await quicStreamContext.DisposeAsync().DefaultTimeout();
            quicStreamContext.Dispose();

            Logger.LogInformation("Client reading until end of stream.");
            var data = await clientStream.ReadUntilEndAsync().DefaultTimeout();
            Assert.Equal(testData.Length, data.Length);
            Assert.Equal(testData, data);

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

            Assert.Equal((long)Http3ErrorCode.InternalError, quicStreamContext.Error);

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
        public async Task ClientToServerUnidirectionalStream_CompleteWrites_PipeProvidesDataAndCompleteTogether()
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

            var readResultTask = serverStream.Transport.Input.ReadAsync();

            await clientStream.WriteAsync(TestData, endStream: true).DefaultTimeout();

            // Assert
            var completeReadResult = await readResultTask.DefaultTimeout();

            Assert.Equal(TestData, completeReadResult.Buffer.ToArray());
            Assert.True(completeReadResult.IsCompleted);
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

            var data = await clientStream.ReadAtLeastLengthAsync(TestData.Length).DefaultTimeout();

            Assert.Equal(TestData, data);

            await serverStream.Transport.Output.CompleteAsync();

            var readCount = await clientStream.ReadAsync(new byte[1024]).DefaultTimeout();

            // Assert
            Assert.Equal(0, readCount);

            var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);
            Assert.True(quicStreamContext.CanWrite);
            Assert.False(quicStreamContext.CanRead);

            // Both send and receive loops have exited.
            await quicStreamContext._processingTask.DefaultTimeout();

            Assert.Contains(TestSink.Writes, m => m.Message.Contains(@"shutting down writes because: ""The QUIC transport's send loop completed gracefully.""."));
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

            var data = await clientStream.ReadAtLeastLengthAsync(TestData.Length).DefaultTimeout();

            Assert.Equal(TestData, data);

            ((IProtocolErrorCodeFeature)serverStream).Error = (long)Http3ErrorCode.InternalError;
            serverStream.Abort(new ConnectionAbortedException("Test message"));

            var ex = await Assert.ThrowsAsync<QuicStreamAbortedException>(() => clientStream.ReadAsync(new byte[1024]).AsTask()).DefaultTimeout();

            // Assert
            Assert.Equal((long)Http3ErrorCode.InternalError, ex.ErrorCode);

            var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);
            Assert.True(quicStreamContext.CanWrite);
            Assert.False(quicStreamContext.CanRead);

            // Both send and receive loops have exited.
            await quicStreamContext._processingTask.DefaultTimeout();

            Assert.Contains(TestSink.Writes, m => m.Message.Contains(@"shutting down writes because: ""Test message""."));
        }

        [ConditionalFact]
        [MsQuicSupported]
        public async Task StreamAbortFeature_AbortWrite_ClientReceivesAbort()
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

            var serverReadTask = serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).AsTask();

            var streamAbortFeature = serverStream.Features.Get<IStreamAbortFeature>();

            streamAbortFeature.AbortRead((long)Http3ErrorCode.InternalError, new ConnectionAbortedException("Test reason"));

            // Assert

            // Server writes data
            await serverStream.Transport.Output.WriteAsync(TestData).DefaultTimeout();
            // Server completes its output.
            await serverStream.Transport.Output.CompleteAsync().DefaultTimeout();

            // Client successfully reads data to end
            var data = await clientStream.ReadUntilEndAsync().DefaultTimeout();
            Assert.Equal(TestData, data);

            // Client errors when writing
            var clientEx = await Assert.ThrowsAsync<QuicStreamAbortedException>(() => clientStream.WriteAsync(data).AsTask()).DefaultTimeout();
            Assert.Equal((long)Http3ErrorCode.InternalError, clientEx.ErrorCode);

            // Server errors when reading
            var serverEx = await Assert.ThrowsAsync<ConnectionAbortedException>(() => serverReadTask).DefaultTimeout();
            Assert.Equal("Test reason", serverEx.Message);
        }
    }
}

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
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Tests;

[Collection(nameof(NoParallelCollection))]
public class QuicStreamContextTests : TestApplicationErrorLoggerLoggedTest
{
    private static readonly byte[] TestData = Encoding.UTF8.GetBytes("Hello world");

    [ConditionalFact]
    [MsQuicSupported]
    public async Task BidirectionalStream_ServerReadsDataAndCompletes_GracefullyClosed()
    {
        // Arrange
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);

        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        await QuicTestHelpers.CreateAndCompleteBidirectionalStreamGracefully(clientConnection, serverConnection, Logger);

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
        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        await using var clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
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

    [ConditionalFact]
    [MsQuicSupported]
    public async Task BidirectionalStream_ClientAbortedAfterDisposeCalled_NotPooled()
    {
        // Arrange
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);

        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        Logger.LogInformation("Client starting stream.");
        await using var clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        await clientStream.WriteAsync(TestData).DefaultTimeout();

        var readTask = clientStream.ReadUntilEndAsync();

        Logger.LogInformation("Server accepted stream.");
        var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();
        var readResult = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        serverStream.Transport.Input.AdvanceTo(readResult.Buffer.End);

        // Server sends a large response that will make it wait to complete sends.
        Logger.LogInformation("Server writing a large response.");
        await serverStream.Transport.Output.WriteAsync(new byte[1024 * 1024 * 32]).DefaultTimeout();

        // Complete reading and writing.
        Logger.LogInformation("Server complete reading and writing.");
        await serverStream.Transport.Input.CompleteAsync();
        await serverStream.Transport.Output.CompleteAsync();

        Logger.LogInformation("Client wait to finish reading.");
        await readTask.DefaultTimeout();

        var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);

        // Server starts disposing
        Logger.LogInformation("Server starts disposing.");
        var disposeTask = quicStreamContext.DisposeAsync();

        // Client aborts while server is draining
        clientStream.Abort(QuicAbortDirection.Read, (long)Http3ErrorCode.RequestCancelled);
        clientStream.Abort(QuicAbortDirection.Write, (long)Http3ErrorCode.RequestCancelled);

        // Server finishes disposing
        Logger.LogInformation("Wait for server finish disposing.");
        await disposeTask.DefaultTimeout();
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
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);

        var testData = new byte[dataLength];
        for (int i = 0; i < dataLength; i++)
        {
            testData[i] = (byte)i;
        }

        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        Logger.LogInformation("Client starting stream.");
        await using var clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        await clientStream.WriteAsync(TestData, completeWrites: true).DefaultTimeout();
        var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();

        Logger.LogInformation("Server accepted stream.");
        var readResult = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        serverStream.Transport.Input.AdvanceTo(readResult.Buffer.End);

        // Input should be completed.
        readResult = await serverStream.Transport.Input.ReadAsync().DefaultTimeout();
        Assert.True(readResult.IsCompleted);

        Logger.LogInformation("Client starting to read.");
        var readingTask = clientStream.ReadUntilEndAsync();

        Logger.LogInformation("Server sending data.");
        await serverStream.Transport.Output.WriteAsync(testData).DefaultTimeout();

        Logger.LogInformation("Server completing pipes.");
        await serverStream.Transport.Input.CompleteAsync().DefaultTimeout();
        await serverStream.Transport.Output.CompleteAsync().DefaultTimeout();

        Logger.LogInformation("Client reading until end of stream.");
        var data = await readingTask.DefaultTimeout();
        Assert.Equal(testData.Length, data.Length);
        Assert.Equal(testData, data);

        var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);

        Logger.LogInformation("Server waiting for send and receiving loops to complete.");
        await quicStreamContext._processingTask.DefaultTimeout();
        Assert.True(quicStreamContext.CanWrite);
        Assert.True(quicStreamContext.CanRead);

        Logger.LogInformation("Server disposing stream.");
        await quicStreamContext.DisposeAsync().DefaultTimeout();
        quicStreamContext.Dispose();

        var quicConnectionContext = Assert.IsType<QuicConnectionContext>(serverConnection);

        Assert.Equal(1, quicConnectionContext.StreamPool.Count);
    }

    [ConditionalFact]
    [MsQuicSupported]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/59978")]
    public async Task BidirectionalStream_MultipleStreamsOnConnection_ReusedFromPool()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        var stream1 = await QuicTestHelpers.CreateAndCompleteBidirectionalStreamGracefully(clientConnection, serverConnection, Logger);
        var stream2 = await QuicTestHelpers.CreateAndCompleteBidirectionalStreamGracefully(clientConnection, serverConnection, Logger);

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
        await using var quicConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        await using var clientStream = await quicConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        await clientStream.WriteAsync(TestData).DefaultTimeout();

        await using var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();
        var readResult = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        serverStream.Transport.Input.AdvanceTo(readResult.Buffer.End);

        var closedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        serverStream.ConnectionClosed.Register(() => closedTcs.SetResult());

        clientStream.Abort(QuicAbortDirection.Write, (long)Http3ErrorCode.InternalError);

        // Receive abort from client.
        var ex = await Assert.ThrowsAsync<ConnectionResetException>(() => serverStream.Transport.Input.ReadAsync().AsTask()).DefaultTimeout();

        // Server completes its output.
        await serverStream.Transport.Output.CompleteAsync();

        // Assert
        Assert.Equal((long)Http3ErrorCode.InternalError, ((QuicException)ex.InnerException).ApplicationErrorCode.Value);

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
        await using var quicConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        await using var clientStream = await quicConnection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional);
        await clientStream.WriteAsync(TestData, completeWrites: true).DefaultTimeout();

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
        await using var quicConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        await using var clientStream = await quicConnection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional);
        await clientStream.WriteAsync(TestData).DefaultTimeout();

        await using var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();
        var readResult = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        serverStream.Transport.Input.AdvanceTo(readResult.Buffer.End);

        var closedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        serverStream.ConnectionClosed.Register(() => closedTcs.SetResult());

        clientStream.Abort(QuicAbortDirection.Write, (long)Http3ErrorCode.InternalError);

        // Receive abort from client.
        var ex = await Assert.ThrowsAsync<ConnectionResetException>(() => serverStream.Transport.Input.ReadAsync().AsTask()).DefaultTimeout();

        // Assert
        Assert.Equal((long)Http3ErrorCode.InternalError, ((QuicException)ex.InnerException).ApplicationErrorCode.Value);

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
        await using var quicConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        await using var clientStream = await quicConnection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional);
        await clientStream.WriteAsync(TestData).DefaultTimeout();

        await using var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();
        var readResult = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        serverStream.Transport.Input.AdvanceTo(readResult.Buffer.End);

        var readResultTask = serverStream.Transport.Input.ReadAsync();

        await clientStream.WriteAsync(TestData, completeWrites: true).DefaultTimeout();

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
        await using var quicConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        var features = new FeatureCollection();
        features.Set<IStreamDirectionFeature>(new DefaultStreamDirectionFeature(canRead: false, canWrite: true));
        var serverStream = await serverConnection.ConnectAsync(features).DefaultTimeout();
        await serverStream.Transport.Output.WriteAsync(TestData).DefaultTimeout();

        await using var clientStream = await quicConnection.AcceptInboundStreamAsync();

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
        await using var quicConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        var features = new FeatureCollection();
        features.Set<IStreamDirectionFeature>(new DefaultStreamDirectionFeature(canRead: false, canWrite: true));
        var serverStream = await serverConnection.ConnectAsync(features).DefaultTimeout();
        await serverStream.Transport.Output.WriteAsync(TestData).DefaultTimeout();

        await using var clientStream = await quicConnection.AcceptInboundStreamAsync();

        var data = await clientStream.ReadAtLeastLengthAsync(TestData.Length).DefaultTimeout();

        Assert.Equal(TestData, data);

        Logger.LogInformation("Server aborting stream");
        ((IProtocolErrorCodeFeature)serverStream).Error = (long)Http3ErrorCode.InternalError;
        serverStream.Abort(new ConnectionAbortedException("Test message"));

        var ex = await Assert.ThrowsAsync<QuicException>(() => clientStream.ReadAsync(new byte[1024]).AsTask()).DefaultTimeout();

        // Assert
        Assert.Equal(QuicError.StreamAborted, ex.QuicError);
        Assert.Equal((long)Http3ErrorCode.InternalError, ex.ApplicationErrorCode.Value);

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
        await using var quicConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        await using var clientStream = await quicConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
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
        var clientEx = await Assert.ThrowsAsync<QuicException>(() => clientStream.WriteAsync(data).AsTask()).DefaultTimeout();
        Assert.Equal(QuicError.StreamAborted, clientEx.QuicError);
        Assert.Equal((long)Http3ErrorCode.InternalError, clientEx.ApplicationErrorCode.Value);

        // Server errors when reading
        var serverEx = await Assert.ThrowsAsync<ConnectionAbortedException>(() => serverReadTask).DefaultTimeout();
        Assert.Equal("Test reason", serverEx.Message);
    }

    [ConditionalTheory]
    [MsQuicSupported]
    [InlineData(-1L)] // Too small
    [InlineData(1L << 62)] // Too big
    public async Task IProtocolErrorFeature_InvalidErrorCode(long errorCode)
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        await using var clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        await clientStream.WriteAsync(TestData).DefaultTimeout();

        var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();

        var protocolErrorCodeFeature = serverStream.Features.Get<IProtocolErrorCodeFeature>();

        // Assert
        Assert.IsType<QuicStreamContext>(protocolErrorCodeFeature);
        Assert.Throws<ArgumentOutOfRangeException>(() => protocolErrorCodeFeature.Error = errorCode);
    }

    [ConditionalTheory]
    [MsQuicSupported]
    [InlineData(-1L)] // Too small
    [InlineData(1L << 62)] // Too big
    public async Task IStreamAbortFeature_InvalidErrorCode(long errorCode)
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        await using var clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        await clientStream.WriteAsync(TestData).DefaultTimeout();

        var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();

        var protocolErrorCodeFeature = serverStream.Features.Get<IStreamAbortFeature>();

        // Assert
        Assert.IsType<QuicStreamContext>(protocolErrorCodeFeature);
        Assert.Throws<ArgumentOutOfRangeException>(() => protocolErrorCodeFeature.AbortRead(errorCode, new ConnectionAbortedException()));
        Assert.Throws<ArgumentOutOfRangeException>(() => protocolErrorCodeFeature.AbortWrite(errorCode, new ConnectionAbortedException()));
    }
}

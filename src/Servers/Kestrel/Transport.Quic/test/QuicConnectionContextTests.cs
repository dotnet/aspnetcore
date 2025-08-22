// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.Http;
using System.Net.Quic;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Tests;

[Collection(nameof(NoParallelCollection))]
public class QuicConnectionContextTests : TestApplicationErrorLoggerLoggedTest
{
    private static readonly byte[] TestData = Encoding.UTF8.GetBytes("Hello world");

    [ConditionalFact]
    [MsQuicSupported]
    public async Task Abort_AbortAfterDispose_Ignored()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(
            LoggerFactory,
            defaultCloseErrorCode: (long)Http3ErrorCode.RequestCancelled);

        // Act
        var acceptTask = connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);

        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await acceptTask.DefaultTimeout();

        await serverConnection.DisposeAsync();

        // Assert
        serverConnection.Abort(); // Doesn't throw ODE.
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task DisposeAsync_DisposeConnectionAfterAcceptingStream_DefaultCloseErrorCodeReported()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(
            LoggerFactory,
            defaultCloseErrorCode: (long)Http3ErrorCode.RequestCancelled);

        // Act
        var acceptTask = connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);

        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await acceptTask.DefaultTimeout();

        await serverConnection.DisposeAsync();

        // Assert
        var ex = await ExceptionAssert.ThrowsAsync<QuicException>(
            () => clientConnection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional).AsTask(),
            exceptionMessage: $"Connection aborted by peer ({(long)Http3ErrorCode.RequestCancelled}).");

        Assert.Equal((long)Http3ErrorCode.RequestCancelled, ex.ApplicationErrorCode);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_CancellationThenAccept_AcceptStreamAfterCancellation()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        // Act
        var acceptTask = connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);

        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await acceptTask.DefaultTimeout();

        // Wait for stream and then cancel
        var cts = new CancellationTokenSource();
        var acceptStreamTask = serverConnection.AcceptAsync(cts.Token);
        cts.Cancel();

        var serverStream = await acceptStreamTask.DefaultTimeout();
        Assert.Null(serverStream);

        // Wait for stream after cancellation
        acceptStreamTask = serverConnection.AcceptAsync();

        await using var clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        await clientStream.WriteAsync(TestData);

        // Assert
        serverStream = await acceptStreamTask.DefaultTimeout();
        Assert.NotNull(serverStream);

        var read = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        Assert.Equal(TestData, read.Buffer.ToArray());
        serverStream.Transport.Input.AdvanceTo(read.Buffer.End);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_ClientClosesConnection_ServerNotified()
    {
        // Arrange
        var connectionClosedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        // Act
        var acceptTask = connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);

        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await acceptTask.DefaultTimeout();
        serverConnection.ConnectionClosed.Register(() => connectionClosedTcs.SetResult());

        var acceptStreamTask = serverConnection.AcceptAsync();

        await clientConnection.CloseAsync(256);

        // Assert
        var ex = await Assert.ThrowsAsync<ConnectionResetException>(() => acceptStreamTask.AsTask()).DefaultTimeout();
        var innerEx = Assert.IsType<QuicException>(ex.InnerException);
        Assert.Equal(QuicError.ConnectionAborted, innerEx.QuicError);
        Assert.Equal(256, innerEx.ApplicationErrorCode.Value);

        await connectionClosedTcs.Task.DefaultTimeout();
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_ClientStartsAndStopsUnidirectionStream_ServerAccepts()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var quicConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        var acceptTask = serverConnection.AcceptAsync();

        await using var clientStream = await quicConnection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional);
        await clientStream.WriteAsync(TestData);

        await using var serverStream = await acceptTask.DefaultTimeout();

        // Assert
        Assert.NotNull(serverStream);
        Assert.False(serverStream.ConnectionClosed.IsCancellationRequested);

        var closedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        serverStream.ConnectionClosed.Register(() => closedTcs.SetResult());

        // Read data from client.
        var read = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        Assert.Equal(TestData, read.Buffer.ToArray());
        serverStream.Transport.Input.AdvanceTo(read.Buffer.End);

        // Shutdown client.
        clientStream.CompleteWrites();

        // Receive shutdown on server.
        read = await serverStream.Transport.Input.ReadAsync().DefaultTimeout();
        Assert.True(read.IsCompleted);

        await closedTcs.Task.DefaultTimeout();
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_ClientStartsAndStopsBidirectionStream_ServerAccepts()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var quicConnection = await QuicConnection.ConnectAsync(options);

        var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        var acceptTask = serverConnection.AcceptAsync();

        await using var clientStream = await quicConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        await clientStream.WriteAsync(TestData);

        await using var serverStream = await acceptTask.DefaultTimeout();
        await serverStream.Transport.Output.WriteAsync(TestData);

        // Assert
        Assert.NotNull(serverStream);
        Assert.False(serverStream.ConnectionClosed.IsCancellationRequested);

        var closedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        serverStream.ConnectionClosed.Register(() => closedTcs.SetResult());

        // Read data from client.
        var read = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        Assert.Equal(TestData, read.Buffer.ToArray());
        serverStream.Transport.Input.AdvanceTo(read.Buffer.End);

        // Read data from server.
        var data = await clientStream.ReadAtLeastLengthAsync(TestData.Length).DefaultTimeout();

        Assert.Equal(TestData, data);

        // Shutdown from client.
        clientStream.CompleteWrites();

        // Get shutdown from client.
        read = await serverStream.Transport.Input.ReadAsync().DefaultTimeout();
        Assert.True(read.IsCompleted);

        await serverStream.Transport.Output.CompleteAsync();

        await closedTcs.Task.DefaultTimeout();
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_ServerStartsAndStopsUnidirectionStream_ClientAccepts()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var quicConnection = await QuicConnection.ConnectAsync(options);

        var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        var acceptTask = quicConnection.AcceptInboundStreamAsync();

        await using var serverStream = await serverConnection.ConnectAsync();
        await serverStream.Transport.Output.WriteAsync(TestData).DefaultTimeout();

        await using var clientStream = await acceptTask.DefaultTimeout();

        // Assert
        Assert.NotNull(clientStream);

        // Read data from server.
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

        // Complete server.
        await serverStream.Transport.Output.CompleteAsync().DefaultTimeout();

        // Receive complete in client.
        readCount = await clientStream.ReadAsync(buffer).DefaultTimeout();
        Assert.Equal(0, readCount);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task AcceptAsync_ClientClosesConnection_ExceptionThrown()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var quicConnection = await QuicConnection.ConnectAsync(options);

        var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        var acceptTask = serverConnection.AcceptAsync().AsTask();

        await quicConnection.CloseAsync((long)Http3ErrorCode.NoError).DefaultTimeout();

        // Assert
        var ex = await Assert.ThrowsAsync<ConnectionResetException>(() => acceptTask).DefaultTimeout();
        var innerEx = Assert.IsType<QuicException>(ex.InnerException);
        Assert.Equal(QuicError.ConnectionAborted, innerEx.QuicError);
        Assert.Equal((long)Http3ErrorCode.NoError, innerEx.ApplicationErrorCode.Value);

        Assert.Equal((long)Http3ErrorCode.NoError, serverConnection.Features.Get<IProtocolErrorCodeFeature>().Error);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task StreamPool_StreamAbortedOnServer_NotPooled()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        var testHeartbeatFeature = new TestHeartbeatFeature();
        serverConnection.Features.Set<IConnectionHeartbeatFeature>(testHeartbeatFeature);

        // Act & Assert
        var quicConnectionContext = Assert.IsType<QuicConnectionContext>(serverConnection);
        Assert.Equal(0, quicConnectionContext.StreamPool.Count);

        await using var clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        await clientStream.WriteAsync(TestData, completeWrites: true).DefaultTimeout();
        var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();
        var readResult = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        serverStream.Transport.Input.AdvanceTo(readResult.Buffer.End);

        // Input should be completed.
        readResult = await serverStream.Transport.Input.ReadAsync();
        Assert.True(readResult.IsCompleted);

        // Complete reading and then abort.
        await serverStream.Transport.Input.CompleteAsync();
        serverStream.Abort(new ConnectionAbortedException("Test message"));

        var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);

        // Both send and receive loops have exited.
        await quicStreamContext._processingTask.DefaultTimeout();

        await quicStreamContext.DisposeAsync();

        Assert.Equal(0, quicConnectionContext.StreamPool.Count);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task StreamPool_StreamAbortedOnServerAfterComplete_NotPooled()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        var testHeartbeatFeature = new TestHeartbeatFeature();
        serverConnection.Features.Set<IConnectionHeartbeatFeature>(testHeartbeatFeature);

        // Act & Assert
        var quicConnectionContext = Assert.IsType<QuicConnectionContext>(serverConnection);
        Assert.Equal(0, quicConnectionContext.StreamPool.Count);

        await using var clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        await clientStream.WriteAsync(TestData, completeWrites: true).DefaultTimeout();
        var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();
        var readResult = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        serverStream.Transport.Input.AdvanceTo(readResult.Buffer.End);

        // Input should be completed.
        readResult = await serverStream.Transport.Input.ReadAsync();
        Assert.True(readResult.IsCompleted);

        // Complete reading and writing.
        await serverStream.Transport.Input.CompleteAsync();
        await serverStream.Transport.Output.CompleteAsync();

        var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);

        // Both send and receive loops have exited.
        await quicStreamContext._processingTask.DefaultTimeout();

        serverStream.Abort(new ConnectionAbortedException("Test message"));

        await quicStreamContext.DisposeAsync();

        Assert.Equal(0, quicConnectionContext.StreamPool.Count);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task StreamPool_StreamAbortedOnClient_NotPooled()
    {
        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        var testHeartbeatFeature = new TestHeartbeatFeature();
        serverConnection.Features.Set<IConnectionHeartbeatFeature>(testHeartbeatFeature);

        // Act & Assert
        var quicConnectionContext = Assert.IsType<QuicConnectionContext>(serverConnection);
        Assert.Equal(0, quicConnectionContext.StreamPool.Count);

        await using var clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        await clientStream.WriteAsync(TestData).DefaultTimeout();

        var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();
        var readResult = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        serverStream.Transport.Input.AdvanceTo(readResult.Buffer.End);

        clientStream.Abort(QuicAbortDirection.Write, (long)Http3ErrorCode.InternalError);

        // Receive abort form client.
        var ex = await Assert.ThrowsAsync<ConnectionResetException>(() => serverStream.Transport.Input.ReadAsync().AsTask()).DefaultTimeout();
        Assert.Equal("Stream aborted by peer (258).", ex.Message);
        Assert.Equal((long)Http3ErrorCode.InternalError, ((QuicException)ex.InnerException).ApplicationErrorCode.Value);

        // Complete reading and then abort.
        await serverStream.Transport.Input.CompleteAsync();
        await serverStream.Transport.Output.CompleteAsync();

        var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);

        // Both send and receive loops have exited.
        await quicStreamContext._processingTask.DefaultTimeout();

        await quicStreamContext.DisposeAsync();

        Assert.Equal(0, quicConnectionContext.StreamPool.Count);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task StreamPool_StreamAbortedOnClientAndServer_NotPooled()
    {
        // Arrange
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);

        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        var testHeartbeatFeature = new TestHeartbeatFeature();
        serverConnection.Features.Set<IConnectionHeartbeatFeature>(testHeartbeatFeature);

        // Act & Assert
        var quicConnectionContext = Assert.IsType<QuicConnectionContext>(serverConnection);
        Assert.Equal(0, quicConnectionContext.StreamPool.Count);

        await using var clientStream = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        await clientStream.WriteAsync(TestData).DefaultTimeout();

        var serverStream = await serverConnection.AcceptAsync().DefaultTimeout();
        var readResult = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        serverStream.Transport.Input.AdvanceTo(readResult.Buffer.End);

        clientStream.Abort(QuicAbortDirection.Write, (long)Http3ErrorCode.InternalError);

        // Receive abort form client.
        var serverEx = await Assert.ThrowsAsync<ConnectionResetException>(() => serverStream.Transport.Input.ReadAsync().AsTask()).DefaultTimeout();
        Assert.Equal("Stream aborted by peer (258).", serverEx.Message);
        Assert.Equal((long)Http3ErrorCode.InternalError, ((QuicException)serverEx.InnerException).ApplicationErrorCode.Value);

        serverStream.Features.Get<IProtocolErrorCodeFeature>().Error = (long)Http3ErrorCode.RequestRejected;
        serverStream.Abort(new ConnectionAbortedException("Test message."));

        // Complete server.
        await serverStream.Transport.Input.CompleteAsync();
        await serverStream.Transport.Output.CompleteAsync();

        var buffer = new byte[1024];
        var clientEx = await Assert.ThrowsAsync<QuicException>(() => clientStream.ReadAsync(buffer).AsTask()).DefaultTimeout();
        Assert.Equal(QuicError.StreamAborted, clientEx.QuicError);
        Assert.Equal((long)Http3ErrorCode.RequestRejected, clientEx.ApplicationErrorCode.Value);

        var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);

        // Both send and receive loops have exited.
        await quicStreamContext._processingTask.DefaultTimeout();

        await quicStreamContext.DisposeAsync();

        Assert.Equal(0, quicConnectionContext.StreamPool.Count);
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task StreamPool_Heartbeat_ExpiredStreamRemoved()
    {
        // Arrange
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);

        var timeProvider = new FakeTimeProvider();
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory, timeProvider);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        var testHeartbeatFeature = new TestHeartbeatFeature();
        serverConnection.Features.Set<IConnectionHeartbeatFeature>(testHeartbeatFeature);

        // Act & Assert
        var quicConnectionContext = Assert.IsType<QuicConnectionContext>(serverConnection);
        Assert.Equal(0, quicConnectionContext.StreamPool.Count);

        var stream1 = await QuicTestHelpers.CreateAndCompleteBidirectionalStreamGracefully(clientConnection, serverConnection, Logger);

        Assert.Equal(1, quicConnectionContext.StreamPool.Count);
        QuicStreamContext pooledStream = quicConnectionContext.StreamPool._array[0];
        Assert.Same(stream1, pooledStream);
        Assert.Equal(timeProvider.GetTimestamp() + QuicConnectionContext.StreamPoolExpirySeconds * timeProvider.TimestampFrequency, pooledStream.PoolExpirationTimestamp);

        timeProvider.Advance(TimeSpan.FromSeconds(0.1));
        testHeartbeatFeature.RaiseHeartbeat();
        // Not removed.
        Assert.Equal(1, quicConnectionContext.StreamPool.Count);

        var stream2 = await QuicTestHelpers.CreateAndCompleteBidirectionalStreamGracefully(clientConnection, serverConnection, Logger);

        Assert.Equal(1, quicConnectionContext.StreamPool.Count);
        pooledStream = quicConnectionContext.StreamPool._array[0];
        Assert.Same(stream1, pooledStream);
        Assert.Equal(timeProvider.GetTimestamp() + QuicConnectionContext.StreamPoolExpirySeconds * timeProvider.TimestampFrequency, pooledStream.PoolExpirationTimestamp);

        Assert.Same(stream1, stream2);

        timeProvider.Advance(TimeSpan.FromSeconds(QuicConnectionContext.StreamPoolExpirySeconds));
        testHeartbeatFeature.RaiseHeartbeat();
        // Not removed.
        Assert.Equal(1, quicConnectionContext.StreamPool.Count);

        timeProvider.Advance(TimeSpan.FromTicks(1));
        testHeartbeatFeature.RaiseHeartbeat();
        // Removed.
        Assert.Equal(0, quicConnectionContext.StreamPool.Count);
    }

    [ConditionalFact]
    [MsQuicSupported]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/56517")]
    public async Task StreamPool_ManyConcurrentStreams_StreamPoolFull()
    {
        // Arrange
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);

        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        var testHeartbeatFeature = new TestHeartbeatFeature();
        serverConnection.Features.Set<IConnectionHeartbeatFeature>(testHeartbeatFeature);

        // Act
        var quicConnectionContext = Assert.IsType<QuicConnectionContext>(serverConnection);
        Assert.Equal(0, quicConnectionContext.StreamPool.Count);

        var pauseCompleteTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allConnectionsOnServerTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var streamTasks = new List<Task>();
        var requestState = new RequestState(clientConnection, serverConnection, allConnectionsOnServerTcs, pauseCompleteTcs.Task);

        const int StreamsSent = 101;
        for (var i = 0; i < StreamsSent; i++)
        {
            streamTasks.Add(SendStream(Logger, streamIndex: i, requestState));
        }

        Logger.LogInformation("Waiting for all connections to be received by the server.");
        await allConnectionsOnServerTcs.Task.DefaultTimeout();
        pauseCompleteTcs.SetResult();

        Logger.LogInformation("Waiting for all stream tasks.");
        await Task.WhenAll(streamTasks).DefaultTimeout();
        Logger.LogInformation("Stream tasks finished.");

        // Assert
        // Up to 100 streams are pooled.
        Assert.Equal(100, quicConnectionContext.StreamPool.Count);

        static async Task SendStream(ILogger logger, int streamIndex, RequestState requestState)
        {
            try
            {
                logger.LogInformation($"{StreamId(streamIndex)}: Client opening outbound stream.");
                await using var clientStream = await requestState.QuicConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                logger.LogInformation($"{StreamId(streamIndex)}: Client writing to stream.");
                await clientStream.WriteAsync(TestData, completeWrites: true).DefaultTimeout();

                logger.LogInformation($"{StreamId(streamIndex)}: Server accepting incoming stream.");
                var serverStream = await requestState.ServerConnection.AcceptAsync().DefaultTimeout();
                logger.LogInformation($"{StreamId(streamIndex)}: Server reading data.");
                var readResult = await serverStream.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
                serverStream.Transport.Input.AdvanceTo(readResult.Buffer.End);

                // Input should be completed.
                logger.LogInformation($"{StreamId(streamIndex)}: Server verifying all data received.");
                readResult = await serverStream.Transport.Input.ReadAsync();
                Assert.True(readResult.IsCompleted);

                lock (requestState)
                {
                    requestState.ActiveConcurrentConnections++;

                    logger.LogInformation($"{StreamId(streamIndex)}: Increasing active concurrent connections to {requestState.ActiveConcurrentConnections}.");
                    if (requestState.ActiveConcurrentConnections == StreamsSent)
                    {
                        logger.LogInformation($"{StreamId(streamIndex)}: All connections on server.");
                        requestState.AllConnectionsOnServerTcs.SetResult();
                    }
                }

                await requestState.PauseCompleteTask;

                // Complete reading and writing.
                logger.LogInformation($"{StreamId(streamIndex)}: Server completing reading and writing.");
                await serverStream.Transport.Input.CompleteAsync();
                await serverStream.Transport.Output.CompleteAsync();

                logger.LogInformation($"{StreamId(streamIndex)}: Client verifying all data received.");
                var count = await clientStream.ReadAsync(new byte[1024]);
                Assert.Equal(0, count);

                logger.LogInformation($"{StreamId(streamIndex)}: Diposing {nameof(QuicStreamContext)}.");
                var quicStreamContext = Assert.IsType<QuicStreamContext>(serverStream);

                // Both send and receive loops have exited.
                await quicStreamContext._processingTask.DefaultTimeout();
                await quicStreamContext.DisposeAsync();
                quicStreamContext.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{StreamId(streamIndex)}: Error.");
                throw;
            }
        }

        static string StreamId(int index) => $"Stream-{index}";
    }

    [ConditionalFact]
    [MsQuicSupported]
    public async Task PersistentState_StreamsReused_StatePersisted()
    {
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);

        // Arrange
        await using var connectionListener = await QuicTestHelpers.CreateConnectionListenerFactory(LoggerFactory);

        var options = QuicTestHelpers.CreateClientConnectionOptions(connectionListener.EndPoint);
        await using var clientConnection = await QuicConnection.ConnectAsync(options);

        await using var serverConnection = await connectionListener.AcceptAndAddFeatureAsync().DefaultTimeout();

        // Act
        Logger.LogInformation("Client starting stream 1");
        await using var clientStream1 = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        await clientStream1.WriteAsync(TestData, completeWrites: true).DefaultTimeout();

        Logger.LogInformation("Server accept stream 1");
        var serverStream1 = await serverConnection.AcceptAsync().DefaultTimeout();

        Logger.LogInformation("Server reading stream 1");
        var readResult1 = await serverStream1.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        serverStream1.Transport.Input.AdvanceTo(readResult1.Buffer.End);

        serverStream1.Features.Get<IPersistentStateFeature>().State["test"] = true;

        // Input should be completed.
        readResult1 = await serverStream1.Transport.Input.ReadAsync();
        Assert.True(readResult1.IsCompleted);

        // Complete reading and writing.
        Logger.LogInformation("Server complete stream 1");
        await serverStream1.Transport.Input.CompleteAsync();
        await serverStream1.Transport.Output.CompleteAsync();

        Logger.LogInformation("Server disposing stream 1");
        var quicStreamContext1 = Assert.IsType<QuicStreamContext>(serverStream1);
        await quicStreamContext1._processingTask.DefaultTimeout();
        await quicStreamContext1.DisposeAsync();
        quicStreamContext1.Dispose();

        Logger.LogInformation("Client starting stream 2");
        await using var clientStream2 = await clientConnection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        await clientStream2.WriteAsync(TestData, completeWrites: true).DefaultTimeout();

        Logger.LogInformation("Server accept stream 2");
        var serverStream2 = await serverConnection.AcceptAsync().DefaultTimeout();

        Logger.LogInformation("Server reading stream 2");
        var readResult2 = await serverStream2.Transport.Input.ReadAtLeastAsync(TestData.Length).DefaultTimeout();
        serverStream2.Transport.Input.AdvanceTo(readResult2.Buffer.End);

        object state = serverStream2.Features.Get<IPersistentStateFeature>().State["test"];

        // Input should be completed.
        readResult2 = await serverStream2.Transport.Input.ReadAsync();
        Assert.True(readResult2.IsCompleted);

        // Complete reading and writing.
        Logger.LogInformation("Server complete stream 2");
        await serverStream2.Transport.Input.CompleteAsync();
        await serverStream2.Transport.Output.CompleteAsync();

        Logger.LogInformation("Server disposing stream 2");
        var quicStreamContext2 = Assert.IsType<QuicStreamContext>(serverStream2);
        await quicStreamContext2._processingTask.DefaultTimeout();
        await quicStreamContext2.DisposeAsync();
        quicStreamContext2.Dispose();

        Assert.Same(quicStreamContext1, quicStreamContext2);

        var quicConnectionContext = Assert.IsType<QuicConnectionContext>(serverConnection);
        Assert.Equal(1, quicConnectionContext.StreamPool.Count);

        Assert.Equal(true, state);
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

        var protocolErrorCodeFeature = serverConnection.Features.Get<IProtocolErrorCodeFeature>();

        // Assert
        Assert.IsType<QuicConnectionContext>(protocolErrorCodeFeature);
        Assert.Throws<ArgumentOutOfRangeException>(() => protocolErrorCodeFeature.Error = errorCode);
    }

    private record RequestState(
        QuicConnection QuicConnection,
        MultiplexedConnectionContext ServerConnection,
        TaskCompletionSource AllConnectionsOnServerTcs,
        Task PauseCompleteTask)
    {
        public int ActiveConcurrentConnections { get; set; }
    };

    private class TestHeartbeatFeature : IConnectionHeartbeatFeature
    {
        private readonly List<(Action<object> Action, object State)> _actions = new List<(Action<object>, object)>();

        public void OnHeartbeat(Action<object> action, object state)
        {
            _actions.Add((action, state));
        }

        public void RaiseHeartbeat()
        {
            foreach (var a in _actions)
            {
                a.Action(a.State);
            }
        }
    }
}

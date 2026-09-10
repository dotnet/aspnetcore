// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

public class RemoteJSDataStreamTest
{
    private static readonly TestRemoteJSRuntime _jsRuntime = new(Options.Create(new CircuitOptions()), Options.Create(new HubOptions<ComponentHub>()), Mock.Of<ILogger<RemoteJSRuntime>>());

    [Fact]
    public async Task CreateRemoteJSDataStreamAsync_CreatesStream()
    {
        // Arrange
        var jsStreamReference = Mock.Of<IJSStreamReference>();

        // Act
        var remoteJSDataStream = await RemoteJSDataStream.CreateRemoteJSDataStreamAsync(_jsRuntime, jsStreamReference, totalLength: 100, signalRMaximumIncomingBytes: 10_000, jsInteropDefaultCallTimeout: TimeSpan.FromMinutes(1), cancellationToken: CancellationToken.None).DefaultTimeout();

        // Assert
        Assert.NotNull(remoteJSDataStream);
    }

    [Fact]
    public async Task ReceiveData_DoesNotFindStream()
    {
        // Arrange
        var chunk = new byte[] { 3, 5, 6, 7 };
        var unrecognizedGuid = 10;

        // Act
        var success = await RemoteJSDataStream.ReceiveData(_jsRuntime, streamId: unrecognizedGuid, chunkId: 0, chunk, error: null).DefaultTimeout();

        // Assert
        Assert.False(success);
    }

    [Fact]
    public async Task ReceiveData_SuccessReadsBackStream()
    {
        // Arrange
        var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions<ComponentHub>()), Mock.Of<ILogger<RemoteJSRuntime>>());
        var remoteJSDataStream = await CreateRemoteJSDataStreamAsync(jsRuntime);
        var streamId = GetStreamId(remoteJSDataStream, jsRuntime);
        var chunk = new byte[100];
        var random = new Random();
        random.NextBytes(chunk);

        var sendDataTask = Task.Run(async () =>
        {
            // Act 1
            var success = await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 0, chunk, error: null).DefaultTimeout();
            return success;
        });

        // Act & Assert 2
        using var memoryStream = new MemoryStream();
        await remoteJSDataStream.CopyToAsync(memoryStream).DefaultTimeout();
        Assert.Equal(chunk, memoryStream.ToArray());

        // Act & Assert 3
        var sendDataCompleted = await sendDataTask.DefaultTimeout();
        Assert.True(sendDataCompleted);
    }

    [Fact]
    public async Task ReceiveData_SuccessReadsBackPipeReader()
    {
        // Arrange
        var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions<ComponentHub>()), Mock.Of<ILogger<RemoteJSRuntime>>());
        var remoteJSDataStream = await CreateRemoteJSDataStreamAsync(jsRuntime);
        var streamId = GetStreamId(remoteJSDataStream, jsRuntime);
        var chunk = new byte[100];
        var random = new Random();
        random.NextBytes(chunk);

        var sendDataTask = Task.Run(async () =>
        {
            // Act 1
            var success = await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 0, chunk, error: null).DefaultTimeout();
            return success;
        });

        // Act & Assert 2
        using var memoryStream = new MemoryStream();
        await remoteJSDataStream.PipeReader.CopyToAsync(memoryStream).DefaultTimeout();
        Assert.Equal(chunk, memoryStream.ToArray());

        // Act & Assert 3
        var sendDataCompleted = await sendDataTask.DefaultTimeout();
        Assert.True(sendDataCompleted);
    }

    [Fact]
    public async Task ReceiveData_WithError()
    {
        // Arrange
        var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions<ComponentHub>()), Mock.Of<ILogger<RemoteJSRuntime>>());
        var remoteJSDataStream = await CreateRemoteJSDataStreamAsync(jsRuntime);
        var streamId = GetStreamId(remoteJSDataStream, jsRuntime);

        // Act & Assert 1
        var success = await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 0, chunk: null, error: "some error").DefaultTimeout();
        Assert.False(success);

        // Act & Assert 2
        using var mem = new MemoryStream();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await remoteJSDataStream.CopyToAsync(mem).DefaultTimeout());
        Assert.Equal("An error occurred while reading the remote stream: some error", ex.Message);
    }

    [Fact]
    public async Task ReceiveData_WithZeroLengthChunk()
    {
        // Arrange
        var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions<ComponentHub>()), Mock.Of<ILogger<RemoteJSRuntime>>());
        var remoteJSDataStream = await CreateRemoteJSDataStreamAsync(jsRuntime);
        var streamId = GetStreamId(remoteJSDataStream, jsRuntime);
        var chunk = Array.Empty<byte>();

        // Act & Assert 1
        var ex = await Assert.ThrowsAsync<EndOfStreamException>(async () => await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 0, chunk, error: null).DefaultTimeout());
        Assert.Equal("The incoming data chunk cannot be empty.", ex.Message);

        // Act & Assert 2
        using var mem = new MemoryStream();
        ex = await Assert.ThrowsAsync<EndOfStreamException>(async () => await remoteJSDataStream.CopyToAsync(mem).DefaultTimeout());
        Assert.Equal("The incoming data chunk cannot be empty.", ex.Message);
    }

    [Fact]
    public async Task ReceiveData_WithLargerChunksThanPermitted()
    {
        // Arrange
        var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions<ComponentHub>()), Mock.Of<ILogger<RemoteJSRuntime>>());
        var remoteJSDataStream = await CreateRemoteJSDataStreamAsync(jsRuntime);
        var streamId = GetStreamId(remoteJSDataStream, jsRuntime);
        var chunk = new byte[50_000]; // more than the 32k maximum chunk size

        // Act & Assert 1
        var ex = await Assert.ThrowsAsync<EndOfStreamException>(async () => await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 0, chunk, error: null).DefaultTimeout());
        Assert.Equal("The incoming data chunk exceeded the permitted length.", ex.Message);

        // Act & Assert 2
        using var mem = new MemoryStream();
        ex = await Assert.ThrowsAsync<EndOfStreamException>(async () => await remoteJSDataStream.CopyToAsync(mem).DefaultTimeout());
        Assert.Equal("The incoming data chunk exceeded the permitted length.", ex.Message);
    }

    [Fact]
    public async Task ReceiveData_ProvidedWithMoreBytesThanRemaining()
    {
        // Arrange
        var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions<ComponentHub>()), Mock.Of<ILogger<RemoteJSRuntime>>());
        var jsStreamReference = Mock.Of<IJSStreamReference>();
        var remoteJSDataStream = await RemoteJSDataStream.CreateRemoteJSDataStreamAsync(jsRuntime, jsStreamReference, totalLength: 100, signalRMaximumIncomingBytes: 10_000, jsInteropDefaultCallTimeout: TimeSpan.FromMinutes(1), cancellationToken: CancellationToken.None);
        var streamId = GetStreamId(remoteJSDataStream, jsRuntime);
        var chunk = new byte[110]; // 100 byte totalLength for stream

        // Act & Assert 1
        var ex = await Assert.ThrowsAsync<EndOfStreamException>(async () => await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 0, chunk, error: null).DefaultTimeout());
        Assert.Equal("The incoming data stream declared a length 100, but 110 bytes were sent.", ex.Message);

        // Act & Assert 2
        using var mem = new MemoryStream();
        ex = await Assert.ThrowsAsync<EndOfStreamException>(async () => await remoteJSDataStream.CopyToAsync(mem).DefaultTimeout());
        Assert.Equal("The incoming data stream declared a length 100, but 110 bytes were sent.", ex.Message);
    }

    [Fact]
    public async Task ReceiveData_ProvidedWithOutOfOrderChunk_SimulatesSignalRDisconnect()
    {
        // Arrange
        var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions<ComponentHub>()), Mock.Of<ILogger<RemoteJSRuntime>>());
        var jsStreamReference = Mock.Of<IJSStreamReference>();
        var remoteJSDataStream = await RemoteJSDataStream.CreateRemoteJSDataStreamAsync(jsRuntime, jsStreamReference, totalLength: 100, signalRMaximumIncomingBytes: 10_000, jsInteropDefaultCallTimeout: TimeSpan.FromMinutes(1), cancellationToken: CancellationToken.None);
        var streamId = GetStreamId(remoteJSDataStream, jsRuntime);
        var chunk = new byte[5];

        // Act & Assert 1
        for (var i = 0; i < 5; i++)
        {
            await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: i, chunk, error: null);
        }
        var ex = await Assert.ThrowsAsync<EndOfStreamException>(async () => await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 7, chunk, error: null).DefaultTimeout());
        Assert.Equal("Out of sequence chunk received, expected 5, but received 7.", ex.Message);

        // Act & Assert 2
        using var mem = new MemoryStream();
        ex = await Assert.ThrowsAsync<EndOfStreamException>(async () => await remoteJSDataStream.CopyToAsync(mem).DefaultTimeout());
        Assert.Equal("Out of sequence chunk received, expected 5, but received 7.", ex.Message);
    }

    [Fact]
    public async Task ReceiveData_NoDataProvidedBeforeTimeout_StreamDisposed()
    {
        // Arrange
        var unhandledExceptionRaisedTask = new TaskCompletionSource<bool>();
        var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions<ComponentHub>()), Mock.Of<ILogger<RemoteJSRuntime>>());
        jsRuntime.UnhandledException += (_, ex) =>
        {
            Assert.Equal("Did not receive any data in the allotted time.", ex.Message);
            unhandledExceptionRaisedTask.SetResult(ex is TimeoutException);
        };

        var jsStreamReference = Mock.Of<IJSStreamReference>();
        var remoteJSDataStream = await RemoteJSDataStream.CreateRemoteJSDataStreamAsync(
            jsRuntime,
            jsStreamReference,
            totalLength: 15,
            signalRMaximumIncomingBytes: 10_000,
            jsInteropDefaultCallTimeout: TimeSpan.FromSeconds(2),
            cancellationToken: CancellationToken.None);
        var streamId = GetStreamId(remoteJSDataStream, jsRuntime);
        var chunk = new byte[] { 3, 5, 7 };

        // Act & Assert 1
        // Trigger timeout and ensure unhandled exception raised to crush circuit
        remoteJSDataStream.InvalidateLastDataReceivedTimeForTimeout();
        var unhandledExceptionResult = await unhandledExceptionRaisedTask.Task.DefaultTimeout();
        Assert.True(unhandledExceptionResult);

        // Act & Assert 2
        // Confirm exception also raised on pipe reader
        using var mem = new MemoryStream();
        var ex = await Assert.ThrowsAsync<TimeoutException>(async () => await remoteJSDataStream.CopyToAsync(mem).DefaultTimeout());
        Assert.Equal("Did not receive any data in the allotted time.", ex.Message);

        // Act & Assert 3
        // Ensures stream is disposed after the timeout and any additional chunks aren't accepted
        var success = await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 0, chunk, error: null).DefaultTimeout();
        Assert.False(success);
    }

    [Fact]
    public async Task ReceiveData_ReceivesDataThenTimesout_StreamDisposed()
    {
        // Arrange
        var unhandledExceptionRaisedTask = new TaskCompletionSource<bool>();
        var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions<ComponentHub>()), Mock.Of<ILogger<RemoteJSRuntime>>());
        jsRuntime.UnhandledException += (_, ex) =>
        {
            Assert.Equal("Did not receive any data in the allotted time.", ex.Message);
            unhandledExceptionRaisedTask.SetResult(ex is TimeoutException);
        };

        var jsStreamReference = Mock.Of<IJSStreamReference>();
        var remoteJSDataStream = await RemoteJSDataStream.CreateRemoteJSDataStreamAsync(
            jsRuntime,
            jsStreamReference,
            totalLength: 15,
            signalRMaximumIncomingBytes: 10_000,
            jsInteropDefaultCallTimeout: TimeSpan.FromSeconds(3),
            cancellationToken: CancellationToken.None);
        var streamId = GetStreamId(remoteJSDataStream, jsRuntime);
        var chunk = new byte[] { 3, 5, 7 };

        // Act & Assert 1
        var success = await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 0, chunk, error: null).DefaultTimeout();
        Assert.True(success);

        // Act & Assert 2
        success = await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 1, chunk, error: null).DefaultTimeout();
        Assert.True(success);

        // Act & Assert 3
        // Trigger timeout and ensure unhandled exception raised to crush circuit
        remoteJSDataStream.InvalidateLastDataReceivedTimeForTimeout();
        var unhandledExceptionResult = await unhandledExceptionRaisedTask.Task.DefaultTimeout();
        Assert.True(unhandledExceptionResult);

        // Act & Assert 4
        // Confirm exception also raised on pipe reader
        using var mem = new MemoryStream();
        var ex = await Assert.ThrowsAsync<TimeoutException>(async () => await remoteJSDataStream.CopyToAsync(mem).DefaultTimeout());
        Assert.Equal("Did not receive any data in the allotted time.", ex.Message);

        // Act & Assert 5
        // Ensures stream is disposed after the timeout and any additional chunks aren't accepted
        success = await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 2, chunk, error: null).DefaultTimeout();
        Assert.False(success);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValueLinkedCts_Works_WhenOneTokenCannotBeCanceled(bool isToken1Cancelable)
    {
        var cts = new CancellationTokenSource();
        var token1 = isToken1Cancelable ? cts.Token : CancellationToken.None;
        var token2 = isToken1Cancelable ? CancellationToken.None : cts.Token;

        using var linkedCts = RemoteJSDataStream.ValueLinkedCancellationTokenSource.Create(token1, token2);

        Assert.False(linkedCts.HasLinkedCancellationTokenSource);
        Assert.False(linkedCts.Token.IsCancellationRequested);

        cts.Cancel();

        Assert.True(linkedCts.Token.IsCancellationRequested);
    }

    [Fact]
    public void ValueLinkedCts_Works_WhenBothTokensCannotBeCanceled()
    {
        using var linkedCts = RemoteJSDataStream.ValueLinkedCancellationTokenSource.Create(
            CancellationToken.None,
            CancellationToken.None);

        Assert.False(linkedCts.HasLinkedCancellationTokenSource);
        Assert.False(linkedCts.Token.IsCancellationRequested);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ValueLinkedCts_Works_WhenBothTokensCanBeCanceled(bool shouldCancelToken1, bool shouldCancelToken2)
    {
        var cts1 = new CancellationTokenSource();
        var cts2 = new CancellationTokenSource();
        using var linkedCts = RemoteJSDataStream.ValueLinkedCancellationTokenSource.Create(cts1.Token, cts2.Token);

        Assert.True(linkedCts.HasLinkedCancellationTokenSource);
        Assert.False(linkedCts.Token.IsCancellationRequested);

        if (shouldCancelToken1)
        {
            cts1.Cancel();
        }
        if (shouldCancelToken2)
        {
            cts2.Cancel();
        }

        Assert.True(linkedCts.Token.IsCancellationRequested);
    }

    private static async Task<RemoteJSDataStream> CreateRemoteJSDataStreamAsync(TestRemoteJSRuntime jsRuntime = null)
    {
        var jsStreamReference = Mock.Of<IJSStreamReference>();
        var remoteJSDataStream = await RemoteJSDataStream.CreateRemoteJSDataStreamAsync(jsRuntime ?? _jsRuntime, jsStreamReference, totalLength: 100, signalRMaximumIncomingBytes: 10_000, jsInteropDefaultCallTimeout: TimeSpan.FromMinutes(1), cancellationToken: CancellationToken.None);
        return remoteJSDataStream;
    }

    private static long GetStreamId(RemoteJSDataStream stream, RemoteJSRuntime runtime) =>
        runtime.RemoteJSDataStreamInstances.FirstOrDefault(kvp => kvp.Value == stream).Key;

    class TestRemoteJSRuntime : RemoteJSRuntime, IJSRuntime
    {
        public TestRemoteJSRuntime(IOptions<CircuitOptions> circuitOptions, IOptions<HubOptions<ComponentHub>> hubOptions, ILogger<RemoteJSRuntime> logger) : base(circuitOptions, hubOptions, logger)
        {
        }

        public new ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            Assert.Equal("Blazor._internal.sendJSDataStream", identifier);
            return ValueTask.FromResult<TValue>(default);
        }
    }
}

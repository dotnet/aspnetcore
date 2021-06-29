// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    public class RemoteJSDataStreamTest
    {
        private static readonly TestRemoteJSRuntime _jsRuntime = new(Options.Create(new CircuitOptions()), Options.Create(new HubOptions()), Mock.Of<ILogger<RemoteJSRuntime>>());

        [Fact]
        public async void CreateRemoteJSDataStreamAsync_CreatesStream()
        {
            // Arrange
            var jsStreamReference = Mock.Of<IJSStreamReference>();

            // Act
            var remoteJSDataStream = await RemoteJSDataStream.CreateRemoteJSDataStreamAsync(_jsRuntime, jsStreamReference, totalLength: 100, maximumIncomingBytes: 10_000, jsInteropDefaultCallTimeout: TimeSpan.FromMinutes(1), pauseIncomingBytesThreshold: 50, resumeIncomingBytesThreshold: 25, cancellationToken: CancellationToken.None);

            // Assert
            Assert.NotNull(remoteJSDataStream);
        }

        [Fact]
        public async void ReceiveData_DoesNotFindStream()
        {
            // Arrange
            var chunk = new byte[] { 3, 5, 6, 7 };
            var unrecognizedGuid = 10;

            // Act
            var success = await RemoteJSDataStream.ReceiveData(_jsRuntime, streamId: unrecognizedGuid, chunkId: 0, chunk, error: null);

            // Assert
            Assert.False(success);
        }

        [Fact]
        public async void ReceiveData_SuccessReadsBackStream()
        {
            // Arrange
            var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions()), Mock.Of<ILogger<RemoteJSRuntime>>());
            var remoteJSDataStream = await CreateRemoteJSDataStreamAsync(jsRuntime);
            var streamId = GetStreamId(remoteJSDataStream, jsRuntime);
            var chunk = new byte[100];
            var random = new Random();
            random.NextBytes(chunk);

            var sendDataTask = Task.Run(async () =>
            {
                // Act 1
                var success = await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 0, chunk, error: null);
                return success;
            });

            // Act & Assert 2
            using var memoryStream = new MemoryStream();
            await remoteJSDataStream.CopyToAsync(memoryStream);
            Assert.Equal(chunk, memoryStream.ToArray());

            // Act & Assert 3
            var sendDataCompleted = await sendDataTask;
            Assert.True(sendDataCompleted);
        }

        [Fact]
        public async void ReceiveData_WithError()
        {
            // Arrange
            var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions()), Mock.Of<ILogger<RemoteJSRuntime>>());
            var remoteJSDataStream = await CreateRemoteJSDataStreamAsync(jsRuntime);
            var streamId = GetStreamId(remoteJSDataStream, jsRuntime);

            // Act & Assert 1
            var success = await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 0, chunk: null, error: "some error");
            Assert.False(success);

            // Act & Assert 2
            using var mem = new MemoryStream();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await remoteJSDataStream.CopyToAsync(mem));
            Assert.Equal("An error occurred while reading the remote stream: some error", ex.Message);
        }

        [Fact]
        public async void ReceiveData_WithZeroLengthChunk()
        {
            // Arrange
            var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions()), Mock.Of<ILogger<RemoteJSRuntime>>());
            var remoteJSDataStream = await CreateRemoteJSDataStreamAsync(jsRuntime);
            var streamId = GetStreamId(remoteJSDataStream, jsRuntime);
            var chunk = Array.Empty<byte>();

            // Act & Assert 1
            var ex = await Assert.ThrowsAsync<EndOfStreamException>(async () => await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 0, chunk, error: null));
            Assert.Equal("The incoming data chunk cannot be empty.", ex.Message);

            // Act & Assert 2
            using var mem = new MemoryStream();
            ex = await Assert.ThrowsAsync<EndOfStreamException>(async () => await remoteJSDataStream.CopyToAsync(mem));
            Assert.Equal("The incoming data chunk cannot be empty.", ex.Message);
        }

        [Fact]
        public async void ReceiveData_ProvidedWithMoreBytesThanRemaining()
        {
            // Arrange
            var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions()), Mock.Of<ILogger<RemoteJSRuntime>>());
            var jsStreamReference = Mock.Of<IJSStreamReference>();
            var remoteJSDataStream = await RemoteJSDataStream.CreateRemoteJSDataStreamAsync(jsRuntime, jsStreamReference, totalLength: 100, maximumIncomingBytes: 10_000, jsInteropDefaultCallTimeout: TimeSpan.FromMinutes(1), pauseIncomingBytesThreshold: 50, resumeIncomingBytesThreshold: 25, cancellationToken: CancellationToken.None);
            var streamId = GetStreamId(remoteJSDataStream, jsRuntime);
            var chunk = new byte[110]; // 100 byte totalLength for stream

            // Act & Assert 1
            var ex = await Assert.ThrowsAsync<EndOfStreamException>(async () => await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 0, chunk, error: null));
            Assert.Equal("The incoming data stream declared a length 100, but 110 bytes were sent.", ex.Message);

            // Act & Assert 2
            using var mem = new MemoryStream();
            ex = await Assert.ThrowsAsync<EndOfStreamException>(async () => await remoteJSDataStream.CopyToAsync(mem));
            Assert.Equal("The incoming data stream declared a length 100, but 110 bytes were sent.", ex.Message);
        }

        [Fact]
        public async void ReceiveData_ProvidedWithOutOfOrderChunk_SimulatesSignalRDisconnect()
        {
            // Arrange
            var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions()), Mock.Of<ILogger<RemoteJSRuntime>>());
            var jsStreamReference = Mock.Of<IJSStreamReference>();
            var remoteJSDataStream = await RemoteJSDataStream.CreateRemoteJSDataStreamAsync(jsRuntime, jsStreamReference, totalLength: 100, maximumIncomingBytes: 10_000, jsInteropDefaultCallTimeout: TimeSpan.FromMinutes(1), pauseIncomingBytesThreshold: 50, resumeIncomingBytesThreshold: 25, cancellationToken: CancellationToken.None);
            var streamId = GetStreamId(remoteJSDataStream, jsRuntime);
            var chunk = new byte[5];

            // Act & Assert 1
            for (var i = 0; i < 5; i++)
            {
                await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: i, chunk, error: null);
            }
            var ex = await Assert.ThrowsAsync<EndOfStreamException>(async () => await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 7, chunk, error: null));
            Assert.Equal("Out of sequence chunk received, expected 5, but received 7.", ex.Message);

            // Act & Assert 2
            using var mem = new MemoryStream();
            ex = await Assert.ThrowsAsync<EndOfStreamException>(async () => await remoteJSDataStream.CopyToAsync(mem));
            Assert.Equal("Out of sequence chunk received, expected 5, but received 7.", ex.Message);
        }

        [Fact]
        public async void ReceiveData_NoDataProvidedBeforeTimeout_StreamDisposed()
        {
            // Arrange
            var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions()), Mock.Of<ILogger<RemoteJSRuntime>>());
            var timeoutExceptionRaisedSemaphore = new SemaphoreSlim(initialCount: 0, maxCount: 1);
            jsRuntime.UnhandledException += (_, ex) =>
            {
                Assert.Equal("Did not receive any data in the alloted time.", ex.Message);
                Assert.IsType<TimeoutException>(ex);
                timeoutExceptionRaisedSemaphore.Release();
            };

            var jsStreamReference = Mock.Of<IJSStreamReference>();
            var remoteJSDataStream = await RemoteJSDataStream.CreateRemoteJSDataStreamAsync(
                jsRuntime,
                jsStreamReference,
                totalLength: 15,
                maximumIncomingBytes: 10_000,
                jsInteropDefaultCallTimeout: TimeSpan.FromSeconds(10), // Note we're using a 10 second timeout for this test
                pauseIncomingBytesThreshold: 50,
                resumeIncomingBytesThreshold: 25,
                cancellationToken: CancellationToken.None);
            var streamId = GetStreamId(remoteJSDataStream, jsRuntime);
            var chunk = new byte[] { 3, 5, 7 };

            // Act & Assert 1
            // Trigger timeout and ensure unhandled exception raised to crush circuit
            remoteJSDataStream.InvalidateLastDataReceivedTimeForTimeout();
            await timeoutExceptionRaisedSemaphore.WaitAsync();

            // Act & Assert 2
            // Confirm exception also raised on pipe reader
            using var mem = new MemoryStream();
            var ex = await Assert.ThrowsAsync<TimeoutException>(async () => await remoteJSDataStream.CopyToAsync(mem));
            Assert.Equal("Did not receive any data in the alloted time.", ex.Message);

            // Act & Assert 3
            // Ensures stream is disposed after the timeout and any additional chunks aren't accepted
            var success = await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 0, chunk, error: null);
            Assert.False(success);
        }

        [Fact]
        public async void ReceiveData_ReceivesDataThenTimesout_StreamDisposed()
        {
            // Arrange
            var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions()), Mock.Of<ILogger<RemoteJSRuntime>>());
            var timeoutExceptionRaisedSemaphore = new SemaphoreSlim(initialCount: 0, maxCount: 1);
            jsRuntime.UnhandledException += (_, ex) =>
            {
                Assert.Equal("Did not receive any data in the alloted time.", ex.Message);
                Assert.IsType<TimeoutException>(ex);
                timeoutExceptionRaisedSemaphore.Release();
            };

            var jsStreamReference = Mock.Of<IJSStreamReference>();
            var remoteJSDataStream = await RemoteJSDataStream.CreateRemoteJSDataStreamAsync(
                jsRuntime,
                jsStreamReference,
                totalLength: 15,
                maximumIncomingBytes: 10_000,
                jsInteropDefaultCallTimeout: TimeSpan.FromSeconds(30), // Note we're using a 30 second timeout for this test
                pauseIncomingBytesThreshold: 50,
                resumeIncomingBytesThreshold: 25,
                cancellationToken: CancellationToken.None);
            var streamId = GetStreamId(remoteJSDataStream, jsRuntime);
            var chunk = new byte[] { 3, 5, 7 };

            // Act & Assert 1
            var success = await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 0, chunk, error: null);
            Assert.True(success);

            // Act & Assert 2
            success = await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 1, chunk, error: null);
            Assert.True(success);

            // Act & Assert 3
            // Trigger timeout and ensure unhandled exception raised to crush circuit
            remoteJSDataStream.InvalidateLastDataReceivedTimeForTimeout();
            await timeoutExceptionRaisedSemaphore.WaitAsync();

            // Act & Assert 4
            // Confirm exception also raised on pipe reader
            using var mem = new MemoryStream();
            var ex = await Assert.ThrowsAsync<TimeoutException>(async () => await remoteJSDataStream.CopyToAsync(mem));
            Assert.Equal("Did not receive any data in the alloted time.", ex.Message);

            // Act & Assert 5
            // Ensures stream is disposed after the timeout and any additional chunks aren't accepted
            success = await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunkId: 2, chunk, error: null);
            Assert.False(success);
        }

        private static async Task<RemoteJSDataStream> CreateRemoteJSDataStreamAsync(TestRemoteJSRuntime jsRuntime = null)
        {
            var jsStreamReference = Mock.Of<IJSStreamReference>();
            var remoteJSDataStream = await RemoteJSDataStream.CreateRemoteJSDataStreamAsync(jsRuntime ?? _jsRuntime, jsStreamReference, totalLength: 100, maximumIncomingBytes: 10_000, jsInteropDefaultCallTimeout: TimeSpan.FromMinutes(1), pauseIncomingBytesThreshold: 50, resumeIncomingBytesThreshold: 25, cancellationToken: CancellationToken.None);
            return remoteJSDataStream;
        }

        private static long GetStreamId(RemoteJSDataStream stream, RemoteJSRuntime runtime) =>
            runtime.RemoteJSDataStreamInstances.FirstOrDefault(kvp => kvp.Value == stream).Key;

        class TestRemoteJSRuntime : RemoteJSRuntime, IJSRuntime
        {
            public TestRemoteJSRuntime(IOptions<CircuitOptions> circuitOptions, IOptions<HubOptions> hubOptions, ILogger<RemoteJSRuntime> logger) : base(circuitOptions, hubOptions, logger)
            {
            }

            public new ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
            {
                Assert.Equal("Blazor._internal.sendJSDataStream", identifier);
                return ValueTask.FromResult<TValue>(default);
            }
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
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
            var jsDataReference = Mock.Of<IJSDataReference>();

            // Act
            var remoteJSDataStream = await RemoteJSDataStream.CreateRemoteJSDataStreamAsync(_jsRuntime, jsDataReference, totalLength: 100, maxBufferSize: 50, maximumIncomingBytes: 10_000);

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
            var success = await RemoteJSDataStream.ReceiveData(_jsRuntime, streamId: unrecognizedGuid, chunk, error: null);

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
            var chunk = new byte[] { 3, 5, 7 };

            // Act
            var success = await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunk, error: null);

            // Assert
            Assert.True(success);
        }

        [Fact]
        public async void ReceiveData_WithError()
        {
            // Arrange
            var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions()), Mock.Of<ILogger<RemoteJSRuntime>>());
            var remoteJSDataStream = await CreateRemoteJSDataStreamAsync(jsRuntime);
            var streamId = GetStreamId(remoteJSDataStream, jsRuntime);

            // Act
            var success = await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunk: null, error: "some error");

            // Assert
            Assert.False(success);
        }

        [Fact]
        public async void ReceiveData_WithZeroLengthChunk()
        {
            // Arrange
            var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions()), Mock.Of<ILogger<RemoteJSRuntime>>());
            var remoteJSDataStream = await CreateRemoteJSDataStreamAsync(jsRuntime);
            var streamId = GetStreamId(remoteJSDataStream, jsRuntime);
            var chunk = Array.Empty<byte>();

            // Act
            var success = await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunk, error: null);

            // Assert
            Assert.False(success);
        }

        [Fact]
        public async void ReceiveData_ProvidedWithMoreBytesThanRemaining()
        {
            // Arrange
            var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), Options.Create(new HubOptions()), Mock.Of<ILogger<RemoteJSRuntime>>());
            var jsDataReference = Mock.Of<IJSDataReference>();
            var remoteJSDataStream = await RemoteJSDataStream.CreateRemoteJSDataStreamAsync(jsRuntime, jsDataReference, totalLength: 100, maxBufferSize: 50, maximumIncomingBytes: 10_000);
            var streamId = GetStreamId(remoteJSDataStream, jsRuntime);
            var chunk = new byte[110]; // 100 byte totalLength for stream 

            // Act
            var success = await RemoteJSDataStream.ReceiveData(jsRuntime, streamId, chunk, error: null);

            // Assert
            Assert.False(success);
        }

        private static async Task<RemoteJSDataStream> CreateRemoteJSDataStreamAsync(TestRemoteJSRuntime jsRuntime = null)
        {
            var jsDataReference = Mock.Of<IJSDataReference>();
            var remoteJSDataStream = await RemoteJSDataStream.CreateRemoteJSDataStreamAsync(jsRuntime ?? _jsRuntime, jsDataReference, totalLength: 100, maxBufferSize: 50, maximumIncomingBytes: 10_000);
            return remoteJSDataStream;
        }

        private static long GetStreamId(RemoteJSDataStream stream, RemoteJSRuntime runtime) =>
            runtime.RemoteJSDataStreamInstances.FirstOrDefault(kvp => kvp.Value == stream).Key;

        class TestRemoteJSRuntime : RemoteJSRuntime, IJSRuntime
        {
            public TestRemoteJSRuntime(IOptions<CircuitOptions> circuitOptions, IOptions<HubOptions> hubOptions, ILogger<RemoteJSRuntime> logger) : base(circuitOptions, hubOptions, logger)
            {
            }

            new public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
            {
                Assert.Equal("Blazor._internal.sendJSDataStream", identifier);
                return ValueTask.FromResult<TValue>(default);
            }
        }
    }
}

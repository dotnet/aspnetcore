// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

public class RemoteJSRuntimeTest
{
    [Fact]
    public void ReceiveByteArray_WithLargerChunksThanPermitted()
    {
        // Arrange
        var jsRuntime = CreateTestRemoteJSRuntime();
        var data = new byte[50_000]; // more than the 32k default MaximumIncomingBytes

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => jsRuntime.TestReceiveByteArray(id: 0, data));
        Assert.Equal("Exceeded the maximum byte array transfer limit for a call. (Parameter 'data')", ex.Message);
    }

    [Fact]
    public void ReceiveByteArray_WithLargeChunks_UsingComponentHubOptions()
    {
        // Arrange
        var jsRuntime = CreateTestRemoteJSRuntime(componentHubMaximumIncomingBytes: 52*1024);
        var data = new byte[50_000];

        // Act & Assert
        jsRuntime.TestReceiveByteArray(id: 0, data);
    }

    [Fact]
    public void ReceiveByteArray_WithLargerChunksThanPermitted_ComponentHubOptionsTakePrecedence()
    {
        // Arrange
        var jsRuntime = CreateTestRemoteJSRuntime(globalHubMaximumIncomingBytes: 52*1024);
        var data = new byte[50_000]; // more than the 32k default MaximumIncomingBytes

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => jsRuntime.TestReceiveByteArray(id: 0, data));
        Assert.Equal("Exceeded the maximum byte array transfer limit for a call. (Parameter 'data')", ex.Message);
    }

    [Fact]
    public void ReceiveByteArray_WithLargeChunks_UsingGlobalHubOptions()
    {
        // Arrange
        var jsRuntime = CreateTestRemoteJSRuntime(componentHubMaximumIncomingBytes: null, globalHubMaximumIncomingBytes: 52*1024);
        var data = new byte[50_000];

        // Act & Assert
        jsRuntime.TestReceiveByteArray(id: 0, data);
    }

    [Fact]
    public void ReceiveByteArray_WithLargeChunks_NullOptions()
    {
        // Arrange
        var jsRuntime = CreateTestRemoteJSRuntime(componentHubMaximumIncomingBytes: null, globalHubMaximumIncomingBytes: null);
        var data = new byte[50_000];

        // Act & Assert
        jsRuntime.TestReceiveByteArray(id: 0, data);
    }

    [Fact]
    public void ReceiveByteArray_RejectsLargeNumberOfSmallArrays()
    {
        // Arrange
        var jsRuntime = CreateTestRemoteJSRuntime(componentHubMaximumIncomingBytes: 20);

        // Act & Assert
        jsRuntime.TestReceiveByteArray(id: 0, new byte[0]);
        for (var i = 1; i < 5; i++)
        {
            // Each array takes is counted as being atleast 4 bytes. 5*4 = 20 => after this loop we've received max bytes
            jsRuntime.TestReceiveByteArray(i, new byte[0]);
        }

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => jsRuntime.TestReceiveByteArray(5, new byte[] { 0 }));
        Assert.Equal("Exceeded the maximum byte array transfer limit for a call. (Parameter 'data')", ex.Message);
    }

    [Fact]
    public void ReceiveByteArray_RejectsExcessiveData()
    {
        // Arrange
        var jsRuntime = CreateTestRemoteJSRuntime();

        // Act & Assert
        jsRuntime.TestReceiveByteArray(id: 0, new byte[30000]);
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => jsRuntime.TestReceiveByteArray(1, new byte[5000]));
        Assert.Equal("Exceeded the maximum byte array transfer limit for a call. (Parameter 'data')", ex.Message);
    }

    [Fact]
    public void ReceiveByteArray_ResetsBytesReceivedWhenIdIsZero()
    {
        // Arrange
        var jsRuntime = CreateTestRemoteJSRuntime();

        // Act & Assert
        jsRuntime.TestReceiveByteArray(id: 0, new byte[30000]);
        jsRuntime.TestReceiveByteArray(id: 0, new byte[5000]);
    }

    private TestRemoteJSRuntime CreateTestRemoteJSRuntime(long? componentHubMaximumIncomingBytes = 32*1024, long? globalHubMaximumIncomingBytes = 32*1024)
    {
        var componentHubOptions = Options.Create(new HubOptions<ComponentHub>());
        componentHubOptions.Value.MaximumReceiveMessageSize = componentHubMaximumIncomingBytes;
        var globalHubOptions = Options.Create(new HubOptions());
        globalHubOptions.Value.MaximumReceiveMessageSize = globalHubMaximumIncomingBytes;
        var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), componentHubOptions, globalHubOptions, Mock.Of<ILogger<RemoteJSRuntime>>());
        return jsRuntime;
    }

    class TestRemoteJSRuntime : RemoteJSRuntime, IJSRuntime
    {
        public TestRemoteJSRuntime(IOptions<CircuitOptions> circuitOptions, IOptions<HubOptions<ComponentHub>> hubOptions, IOptions<HubOptions> globalHubOptions, ILogger<RemoteJSRuntime> logger) : base(circuitOptions, hubOptions, globalHubOptions, logger)
        {
        }

        public void TestReceiveByteArray(int id, byte[] data)
        {
            ReceiveByteArray(id, data);
        }

        public new ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            Assert.Equal("Blazor._internal.sendJSDataStream", identifier);
            return ValueTask.FromResult<TValue>(default);
        }
    }
}

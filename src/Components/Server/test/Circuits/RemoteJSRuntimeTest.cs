// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

public class RemoteJSRuntimeTest
{
    [Fact]
    public void ReceiveByteArray_WithChunkSmallerThanDefaultMaximum()
    {
        // Arrange
        var jsRuntime = CreateTestRemoteJSRuntime();
        var data = new byte[29_000];

        // Act & Assert
        jsRuntime.TestReceiveByteArray(id: 0, data);
    }

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
    public void ReceiveByteArray_WithLargeChunks_UsingConfiguredComponentHubOptions()
    {
        // Arrange
        var jsRuntime = CreateTestRemoteJSRuntime(componentHubMaximumIncomingBytes: 52 * 1024);
        var data = new byte[50_000];

        // Act & Assert
        jsRuntime.TestReceiveByteArray(id: 0, data);
    }

    [Fact]
    public void ReceiveByteArray_WithLargeChunks_NullComponentHubOptions()
    {
        // Arrange
        var jsRuntime = CreateTestRemoteJSRuntime(componentHubMaximumIncomingBytes: null);
        var data = new byte[100_000];

        // Act & Assert
        jsRuntime.TestReceiveByteArray(id: 0, data);
    }

    [Fact]
    public void ReceiveByteArray_RejectsLargeNumberOfSmallArrays()
    {
        // Arrange
        var jsRuntime = CreateTestRemoteJSRuntime(componentHubMaximumIncomingBytes: 20);

        // Act & Assert
        jsRuntime.TestReceiveByteArray(id: 0, Array.Empty<byte>());
        for (var i = 1; i < 5; i++)
        {
            // Each array takes is counted as being atleast 4 bytes. 5*4 = 20 => after this loop we've received max bytes
            jsRuntime.TestReceiveByteArray(i, Array.Empty<byte>());
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

    private static TestRemoteJSRuntime CreateTestRemoteJSRuntime(long? componentHubMaximumIncomingBytes = 32 * 1024)
    {
        var componentHubOptions = Options.Create(new HubOptions<ComponentHub>());
        componentHubOptions.Value.MaximumReceiveMessageSize = componentHubMaximumIncomingBytes;
        var jsRuntime = new TestRemoteJSRuntime(Options.Create(new CircuitOptions()), componentHubOptions, Mock.Of<ILogger<RemoteJSRuntime>>());
        return jsRuntime;
    }

    class TestRemoteJSRuntime : RemoteJSRuntime, IJSRuntime
    {
        public TestRemoteJSRuntime(IOptions<CircuitOptions> circuitOptions, IOptions<HubOptions<ComponentHub>> hubOptions, ILogger<RemoteJSRuntime> logger) : base(circuitOptions, hubOptions, logger)
        {
        }

        public void TestReceiveByteArray(int id, byte[] data)
        {
            ReceiveByteArray(id, data);
        }
    }
}

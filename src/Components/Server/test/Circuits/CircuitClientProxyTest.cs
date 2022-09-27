// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

public class CircuitClientProxyTest
{
    [Fact]
    public async Task SendCoreAsync_WithoutTransfer()
    {
        // Arrange
        bool? isCancelled = null;
        var clientProxy = new Mock<IClientProxy>();
        clientProxy.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback((string _, object[] __, CancellationToken token) =>
            {
                isCancelled = token.IsCancellationRequested;
            })
            .Returns(Task.CompletedTask);
        var circuitClient = new CircuitClientProxy(clientProxy.Object, "connection0");

        // Act
        var sendTask = circuitClient.SendCoreAsync("test", Array.Empty<object>());
        await sendTask;

        // Assert
        Assert.False(isCancelled);
    }

    [Fact]
    public void Transfer_SetsConnected()
    {
        // Arrange
        var clientProxy = Mock.Of<IClientProxy>(
            c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()) == Task.CompletedTask);
        var circuitClient = new CircuitClientProxy(clientProxy, "connection0");
        circuitClient.SetDisconnected();

        // Act
        circuitClient.Transfer(Mock.Of<IClientProxy>(), "connection1");

        // Assert
        Assert.True(circuitClient.Connected);
    }
}

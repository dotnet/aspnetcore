// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
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
}

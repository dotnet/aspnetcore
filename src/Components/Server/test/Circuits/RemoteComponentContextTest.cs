// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace Microsoft.AspNetCore.Components.Browser.Rendering
{
    public class RemoteComponentContextTest
    {
        [Fact]
        public void IfNotInitialized_IsConnectedReturnsFalse()
        {
            Assert.False(new RemoteComponentContext().IsConnected);
        }

        [Fact]
        public void IfInitialized_IsConnectedValueDeterminedByCircuitProxy()
        {
            // Arrange
            var clientProxy = new FakeClientProxy();
            var circuitProxy = new CircuitClientProxy(clientProxy, "test connection");
            var remoteComponentContext = new RemoteComponentContext();

            // Act/Assert: Can observe connected state
            remoteComponentContext.Initialize(circuitProxy);
            Assert.True(remoteComponentContext.IsConnected);

            // Act/Assert: Can observe disconnected state
            circuitProxy.SetDisconnected();
            Assert.False(remoteComponentContext.IsConnected);
        }

        private class FakeClientProxy : IClientProxy
        {
            public Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken = default)
            { 
                throw new NotImplementedException();
            }
        }
    }
}

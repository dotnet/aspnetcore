// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Components.Browser;
using Microsoft.AspNetCore.Components.Browser.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    public class CircuitHostTest
    {
        [Fact]
        public void Dispose_DisposesResources()
        {
            // Arrange
            var serviceScope = new Mock<IServiceScope>();
            var clientProxy = Mock.Of<IClientProxy>();
            var renderRegistry = new RendererRegistry();
            var jsRuntime = Mock.Of<IJSRuntime>();
            var syncContext = new CircuitSynchronizationContext();

            var remoteRenderer = new TestRemoteRenderer(
                Mock.Of<IServiceProvider>(),
                renderRegistry,
                jsRuntime,
                clientProxy,
                syncContext);

            var circuitHost = new CircuitHost(serviceScope.Object, clientProxy, renderRegistry, remoteRenderer, configure: _ => { }, jsRuntime: jsRuntime, synchronizationContext: syncContext);

            // Act
            circuitHost.Dispose();

            // Assert
            serviceScope.Verify(s => s.Dispose(), Times.Once());
            Assert.True(remoteRenderer.Disposed);
        }

        private class TestRemoteRenderer : RemoteRenderer
        {
            public TestRemoteRenderer(IServiceProvider serviceProvider, RendererRegistry rendererRegistry, IJSRuntime jsRuntime, IClientProxy client, SynchronizationContext syncContext)
                : base(serviceProvider, rendererRegistry, jsRuntime, client, syncContext)
            {
            }

            public bool Disposed { get; set; }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                Disposed = true;
            }
        }
    }
}

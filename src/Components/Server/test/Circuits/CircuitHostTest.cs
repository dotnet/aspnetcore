// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
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
        public async Task DisposeAsync_DisposesResources()
        {
            // Arrange
            var serviceScope = new Mock<IServiceScope>();
            var remoteRenderer = GetRemoteRenderer();
            var circuitHost = GetCircuitHost(
                serviceScope.Object,
                remoteRenderer);

            // Act
            await circuitHost.DisposeAsync();

            // Assert
            serviceScope.Verify(s => s.Dispose(), Times.Once());
            Assert.True(remoteRenderer.Disposed);
        }

        [Fact]
        public async Task InitializeAsync_InvokesHandlers()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            var handler1 = new Mock<CircuitHandler>(MockBehavior.Strict);
            var handler2 = new Mock<CircuitHandler>(MockBehavior.Strict);
            var sequence = new MockSequence();

            handler1
                .InSequence(sequence)
                .Setup(h => h.OnCircuitOpenedAsync(It.IsAny<Circuit>(), cancellationToken))
                .Returns(Task.CompletedTask)
                .Verifiable();

            handler2
                .InSequence(sequence)
                .Setup(h => h.OnCircuitOpenedAsync(It.IsAny<Circuit>(), cancellationToken))
                .Returns(Task.CompletedTask)
                .Verifiable();

            handler1
                .InSequence(sequence)
                .Setup(h => h.OnConnectionUpAsync(It.IsAny<Circuit>(), cancellationToken))
                .Returns(Task.CompletedTask)
                .Verifiable();

            handler2
                .InSequence(sequence)
                .Setup(h => h.OnConnectionUpAsync(It.IsAny<Circuit>(), cancellationToken))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var circuitHost = GetCircuitHost(handlers: new[] { handler1.Object, handler2.Object });

            // Act
            await circuitHost.InitializeAsync(cancellationToken);

            // Assert
            handler1.VerifyAll();
            handler2.VerifyAll();
        }

        [Fact]
        public async Task DisposeAsync_InvokesCircuitHandler()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            var handler1 = new Mock<CircuitHandler>(MockBehavior.Strict);
            var handler2 = new Mock<CircuitHandler>(MockBehavior.Strict);
            var sequence = new MockSequence();

            handler1
                .InSequence(sequence)
                .Setup(h => h.OnConnectionDownAsync(It.IsAny<Circuit>(), cancellationToken))
                .Returns(Task.CompletedTask)
                .Verifiable();

            handler2
                .InSequence(sequence)
                .Setup(h => h.OnConnectionDownAsync(It.IsAny<Circuit>(), cancellationToken))
                .Returns(Task.CompletedTask)
                .Verifiable();

            handler1
                .InSequence(sequence)
                .Setup(h => h.OnCircuitClosedAsync(It.IsAny<Circuit>(), cancellationToken))
                .Returns(Task.CompletedTask)
                .Verifiable();

            handler2
                .InSequence(sequence)
                .Setup(h => h.OnCircuitClosedAsync(It.IsAny<Circuit>(), cancellationToken))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var circuitHost = GetCircuitHost(handlers: new[] { handler1.Object, handler2.Object });

            // Act
            await circuitHost.DisposeAsync();

            // Assert
            handler1.VerifyAll();
            handler2.VerifyAll();
        }

        private static CircuitHost GetCircuitHost(
            IServiceScope serviceScope = null,
            RemoteRenderer remoteRenderer = null,
            CircuitHandler[] handlers = null)
        {
            serviceScope = serviceScope ?? Mock.Of<IServiceScope>();
            var clientProxy = Mock.Of<IClientProxy>();
            var renderRegistry = new RendererRegistry();
            var jsRuntime = Mock.Of<IJSRuntime>();
            var syncContext = new CircuitSynchronizationContext();

            remoteRenderer = remoteRenderer ?? GetRemoteRenderer();
            handlers = handlers ?? Array.Empty<CircuitHandler>();

            return new CircuitHost(
                serviceScope,
                clientProxy,
                renderRegistry,
                remoteRenderer,
                configure: _ => { },
                jsRuntime: jsRuntime,
                synchronizationContext:
                syncContext,
                handlers);
        }

        private static TestRemoteRenderer GetRemoteRenderer()
        {
            return new TestRemoteRenderer(
                Mock.Of<IServiceProvider>(),
                new RendererRegistry(),
                Mock.Of<IJSRuntime>(),
                Mock.Of<IClientProxy>(),
                new CircuitSynchronizationContext());
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

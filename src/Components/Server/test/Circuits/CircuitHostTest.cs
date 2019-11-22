// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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
            var circuitHost = TestCircuitHost.Create(
                serviceScope: serviceScope.Object,
                remoteRenderer: remoteRenderer);

            // Act
            await circuitHost.DisposeAsync();

            // Assert
            serviceScope.Verify(s => s.Dispose(), Times.Once());
            Assert.True(remoteRenderer.Disposed);
            Assert.Null(circuitHost.Handle.CircuitHost);
        }

        [Fact]
        public async Task DisposeAsync_DisposesScopeAsynchronouslyIfPossible()
        {
            // Arrange
            var serviceScope = new Mock<IServiceScope>();
            serviceScope
                .As<IAsyncDisposable>()
                .Setup(f => f.DisposeAsync())
                .Returns(new ValueTask(Task.CompletedTask))
                .Verifiable();

            var remoteRenderer = GetRemoteRenderer();
            var circuitHost = TestCircuitHost.Create(
                serviceScope: serviceScope.Object,
                remoteRenderer: remoteRenderer);

            // Act
            await circuitHost.DisposeAsync();

            // Assert
            serviceScope.Verify(s => s.Dispose(), Times.Never());
            serviceScope.As<IAsyncDisposable>().Verify(s => s.DisposeAsync(), Times.Once());
            Assert.True(remoteRenderer.Disposed);
            Assert.Null(circuitHost.Handle.CircuitHost);
        }

        [Fact]
        public async Task DisposeAsync_DisposesResourcesAndSilencesException()
        {
            // Arrange
            var serviceScope = new Mock<IServiceScope>();
            var handler = new Mock<CircuitHandler>();
            handler
                .Setup(h => h.OnCircuitClosedAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()))
                .Throws<InvalidTimeZoneException>();
            var remoteRenderer = GetRemoteRenderer();
            var circuitHost = TestCircuitHost.Create(
                serviceScope: serviceScope.Object,
                remoteRenderer: remoteRenderer,
                handlers: new[] { handler.Object });

            var throwOnDisposeComponent = new ThrowOnDisposeComponent();
            circuitHost.Renderer.AssignRootComponentId(throwOnDisposeComponent);

            // Act
            await circuitHost.DisposeAsync(); // Does not throw

            // Assert
            Assert.True(throwOnDisposeComponent.DidCallDispose);
            serviceScope.Verify(scope => scope.Dispose(), Times.Once());
            Assert.True(remoteRenderer.Disposed);
        }

        [Fact]
        public async Task DisposeAsync_DisposesRendererWithinSynchronizationContext()
        {
            // Arrange
            var serviceScope = new Mock<IServiceScope>();
            var remoteRenderer = GetRemoteRenderer();
            var circuitHost = TestCircuitHost.Create(
                serviceScope: serviceScope.Object,
                remoteRenderer: remoteRenderer);

            var component = new DispatcherComponent(circuitHost.Renderer.Dispatcher);
            circuitHost.Renderer.AssignRootComponentId(component);
            var original = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);

            // Act & Assert
            try
            {
                Assert.Null(SynchronizationContext.Current);
                await circuitHost.DisposeAsync();
                Assert.True(component.Called);
                Assert.Null(SynchronizationContext.Current);
            }
            finally
            {
                // Not sure if the line above messes up the xunit sync context, so just being cautious here.
                SynchronizationContext.SetSynchronizationContext(original);
            }
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

            var circuitHost = TestCircuitHost.Create(handlers: new[] { handler1.Object, handler2.Object });

            // Act
            await circuitHost.InitializeAsync(cancellationToken);

            // Assert
            handler1.VerifyAll();
            handler2.VerifyAll();
        }

        [Fact]
        public async Task InitializeAsync_ReportsOwnAsyncExceptions()
        {
            // Arrange
            var handler = new Mock<CircuitHandler>(MockBehavior.Strict);
            var tcs = new TaskCompletionSource<object>();
            var reportedErrors = new List<UnhandledExceptionEventArgs>();

            handler
                .Setup(h => h.OnCircuitOpenedAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()))
                .Returns(tcs.Task)
                .Verifiable();

            var circuitHost = TestCircuitHost.Create(handlers: new[] { handler.Object });
            circuitHost.UnhandledException += (sender, errorInfo) =>
            {
                Assert.Same(circuitHost, sender);
                reportedErrors.Add(errorInfo);
            };

            // Act
            var initializeAsyncTask = circuitHost.InitializeAsync(new CancellationToken());

            // Assert: No synchronous exceptions
            handler.VerifyAll();
            Assert.Empty(reportedErrors);

            // Act: Trigger async exception
            var ex = new InvalidTimeZoneException();
            tcs.SetException(ex);

            // Assert: The top-level task still succeeds, because the intended usage
            // pattern is fire-and-forget.
            await initializeAsyncTask;

            // Assert: The async exception was reported via the side-channel
            var aex = Assert.IsType<AggregateException>(reportedErrors.Single().ExceptionObject);
            Assert.Same(ex, aex.InnerExceptions.Single());
            Assert.False(reportedErrors.Single().IsTerminating);
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

            var circuitHost = TestCircuitHost.Create(handlers: new[] { handler1.Object, handler2.Object });

            // Act
            await circuitHost.DisposeAsync();

            // Assert
            handler1.VerifyAll();
            handler2.VerifyAll();
        }

        private static TestRemoteRenderer GetRemoteRenderer()
        {
            return new TestRemoteRenderer(
                Mock.Of<IServiceProvider>(),
                Mock.Of<IClientProxy>());
        }

        private class TestRemoteRenderer : RemoteRenderer
        {
            public TestRemoteRenderer(IServiceProvider serviceProvider, IClientProxy client)
                : base(serviceProvider, NullLoggerFactory.Instance, new CircuitOptions(), new CircuitClientProxy(client, "connection"), NullLogger.Instance)
            {
            }

            public bool Disposed { get; set; }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                Disposed = true;
            }
        }

        private class DispatcherComponent : ComponentBase, IDisposable
        {
            public DispatcherComponent(Dispatcher dispatcher)
            {
                Dispatcher = dispatcher;
            }

            public Dispatcher Dispatcher { get; }
            public bool Called { get; private set; }

            public void Dispose()
            {
                Called = true;
                Assert.Same(Dispatcher, SynchronizationContext.Current);
            }
        }

        private class ThrowOnDisposeComponent : IComponent, IDisposable
        {
            public bool DidCallDispose { get; private set; }
            public void Attach(RenderHandle renderHandle) { }

            public Task SetParametersAsync(ParameterView parameters)
                => Task.CompletedTask;

            public void Dispose()
            {
                DidCallDispose = true;
                throw new InvalidFilterCriteriaException();
            }
        }
    }
}

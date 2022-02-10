// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

public class CircuitHostTest
{
    [Fact]
    public async Task DisposeAsync_DisposesResources()
    {
        // Arrange
        var serviceScope = new Mock<IServiceScope>();
        var remoteRenderer = GetRemoteRenderer();
        var circuitHost = TestCircuitHost.Create(
            serviceScope: new AsyncServiceScope(serviceScope.Object),
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
            serviceScope: new AsyncServiceScope(serviceScope.Object),
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
            serviceScope: new AsyncServiceScope(serviceScope.Object),
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
            serviceScope: new AsyncServiceScope(serviceScope.Object),
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
    public async Task DisposeAsync_MarksJSRuntimeAsDisconnectedBeforeDisposingRenderer()
    {
        // Arrange
        var serviceScope = new Mock<IServiceScope>();
        var remoteRenderer = GetRemoteRenderer();
        var circuitHost = TestCircuitHost.Create(
            serviceScope: new AsyncServiceScope(serviceScope.Object),
            remoteRenderer: remoteRenderer);

        var component = new PerformJSInteropOnDisposeComponent(circuitHost.JSRuntime);
        circuitHost.Renderer.AssignRootComponentId(component);

        var circuitUnhandledExceptions = new List<UnhandledExceptionEventArgs>();
        circuitHost.UnhandledException += (sender, eventArgs) =>
        {
            circuitUnhandledExceptions.Add(eventArgs);
        };

        // Act
        await circuitHost.DisposeAsync();

        // Assert: Component disposal logic sees the exception
        var componentException = Assert.IsType<JSDisconnectedException>(component.ExceptionDuringDisposeAsync);

        // Assert: Circuit host notifies about the exception
        Assert.Collection(circuitUnhandledExceptions, eventArgs =>
        {
            Assert.Same(componentException, eventArgs.ExceptionObject);
        });
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
        await circuitHost.InitializeAsync(new ProtectedPrerenderComponentApplicationStore(Mock.Of<IDataProtectionProvider>()), cancellationToken);

        // Assert
        handler1.VerifyAll();
        handler2.VerifyAll();
    }

    [Fact]
    public async Task InitializeAsync_RendersRootComponentsInParallel()
    {
        // To test that root components are run in parallel, we ensure that each root component
        // finishes rendering (i.e. returns from SetParametersAsync()) only after all other
        // root components have started rendering. If the root components were rendered
        // sequentially, the 1st component would get stuck rendering forever because the
        // 2nd component had not yet started rendering. We call RenderInParallelComponent.Setup()
        // to configure how many components will be rendered in advance so that each component
        // can be assigned a TaskCompletionSource and await the same array of tasks. A timeout
        // is configured for circuitHost.InitializeAsync() so that the test can fail rather than
        // hang forever.

        // Arrange
        var componentCount = 3;
        var initializeTimeout = TimeSpan.FromMilliseconds(5000);
        var cancellationToken = new CancellationToken();
        var serviceScope = new Mock<IServiceScope>();
        var descriptors = new List<ComponentDescriptor>();
        RenderInParallelComponent.Setup(componentCount);
        for (var i = 0; i < componentCount; i++)
        {
            descriptors.Add(new()
            {
                ComponentType = typeof(RenderInParallelComponent),
                Parameters = ParameterView.Empty,
                Sequence = 0
            });
        }
        var circuitHost = TestCircuitHost.Create(
            serviceScope: new AsyncServiceScope(serviceScope.Object),
            descriptors: descriptors);

        // Act
        object initializeException = null;
        circuitHost.UnhandledException += (sender, eventArgs) => initializeException = eventArgs.ExceptionObject;
        var initializeTask = circuitHost.InitializeAsync(new ProtectedPrerenderComponentApplicationStore(Mock.Of<IDataProtectionProvider>()), cancellationToken);
        await initializeTask.WaitAsync(initializeTimeout);

        // Assert: This was not reached only because an exception was thrown in InitializeAsync()
        Assert.True(initializeException is null, $"An exception was thrown in {nameof(TestCircuitHost.InitializeAsync)}(): {initializeException}");
    }

    [Fact]
    public async Task InitializeAsync_ReportsOwnAsyncExceptions()
    {
        // Arrange
        var handler = new Mock<CircuitHandler>(MockBehavior.Strict);
        var tcs = new TaskCompletionSource();
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
        var initializeAsyncTask = circuitHost.InitializeAsync(new ProtectedPrerenderComponentApplicationStore(Mock.Of<IDataProtectionProvider>()), new CancellationToken());

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
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(new Mock<IJSRuntime>().Object);
        return new TestRemoteRenderer(
            serviceCollection.BuildServiceProvider(),
            Mock.Of<IClientProxy>());
    }

    private class TestRemoteRenderer : RemoteRenderer
    {
        public TestRemoteRenderer(IServiceProvider serviceProvider, IClientProxy client)
            : base(
                  serviceProvider,
                  NullLoggerFactory.Instance,
                  new CircuitOptions(),
                  new CircuitClientProxy(client, "connection"),
                  NullLogger.Instance,
                  CreateJSRuntime(new CircuitOptions()),
                  new CircuitJSComponentInterop(new CircuitOptions()))
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private static RemoteJSRuntime CreateJSRuntime(CircuitOptions options)
            => new RemoteJSRuntime(Options.Create(options), Options.Create(new HubOptions<ComponentHub>()), null);
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

    private class RenderInParallelComponent : IComponent, IDisposable
    {
        private static TaskCompletionSource[] _renderTcsArray;
        private static int _instanceCount = 0;

        private readonly int _id;

        public static void Setup(int numComponents)
        {
            if (_instanceCount > 0)
            {
                throw new InvalidOperationException(
                    $"Cannot call '{nameof(Setup)}' when there are still " +
                    $"{nameof(RenderInParallelComponent)} instances active.");
            }

            _renderTcsArray = new TaskCompletionSource[numComponents];

            for (int i = 0; i < _renderTcsArray.Length; i++)
            {
                _renderTcsArray[i] = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        public RenderInParallelComponent()
        {
            if (_instanceCount >= _renderTcsArray.Length)
            {
                throw new InvalidOperationException("Created more test component instances than expected.");
            }

            _id = _instanceCount++;
        }

        public void Attach(RenderHandle renderHandle)
        {
        }

        public async Task SetParametersAsync(ParameterView parameters)
        {
            _renderTcsArray[_id].SetResult();
            await Task.WhenAll(_renderTcsArray.Select(tcs => tcs.Task));
        }

        public void Dispose()
        {
            _instanceCount--;
        }
    }

    private class PerformJSInteropOnDisposeComponent : IComponent, IAsyncDisposable
    {
        private readonly IJSRuntime _js;

        public PerformJSInteropOnDisposeComponent(IJSRuntime jsRuntime)
        {
            _js = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        }

        public Exception ExceptionDuringDisposeAsync { get; private set; }

        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters)
            => Task.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _js.InvokeVoidAsync("SomeJsCleanupCode");
            }
            catch (Exception ex)
            {
                ExceptionDuringDisposeAsync = ex;
                throw;
            }
        }
    }
}

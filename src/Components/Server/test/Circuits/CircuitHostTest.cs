// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Rendering;
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
    private readonly IDataProtectionProvider _ephemeralDataProtectionProvider = new EphemeralDataProtectionProvider();
    private readonly ServerComponentInvocationSequence _invocationSequence = new();

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

        SetupMockInboundActivityHandlers(sequence, handler1, handler2);

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
        await circuitHost.InitializeAsync(new ProtectedPrerenderComponentApplicationStore(Mock.Of<IDataProtectionProvider>()), default, cancellationToken);

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
        var initializeTask = circuitHost.InitializeAsync(new ProtectedPrerenderComponentApplicationStore(Mock.Of<IDataProtectionProvider>()), default, cancellationToken);
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

        SetupMockInboundActivityHandler(handler);

        handler
            .Setup(h => h.OnCircuitOpenedAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task)
            .Verifiable();

        var circuitHost = TestCircuitHost.Create(handlers: new[] { handler.Object }, descriptors: [new ComponentDescriptor()]);
        circuitHost.UnhandledException += (sender, errorInfo) =>
        {
            Assert.Same(circuitHost, sender);
            reportedErrors.Add(errorInfo);
        };

        // Act
        var initializeAsyncTask = circuitHost.InitializeAsync(new ProtectedPrerenderComponentApplicationStore(Mock.Of<IDataProtectionProvider>()), default, new CancellationToken());

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

        SetupMockInboundActivityHandlers(sequence, handler1, handler2);

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

    [Fact]
    public async Task HandleInboundActivityAsync_InvokesCircuitActivityHandlers()
    {
        // Arrange
        var handler1 = new Mock<CircuitHandler>(MockBehavior.Strict);
        var handler2 = new Mock<CircuitHandler>(MockBehavior.Strict);
        var handler3 = new Mock<CircuitHandler>(MockBehavior.Strict);
        var sequence = new MockSequence();

        var asyncLocal1 = new AsyncLocal<bool>();
        var asyncLocal3 = new AsyncLocal<bool>();

        handler3
            .InSequence(sequence)
            .Setup(h => h.CreateInboundActivityHandler(It.IsAny<Func<CircuitInboundActivityContext, Task>>()))
            .Returns((Func<CircuitInboundActivityContext, Task> next) => async (CircuitInboundActivityContext context) =>
            {
                asyncLocal3.Value = true;
                await next(context);
            })
            .Verifiable();

        handler2
            .InSequence(sequence)
            .Setup(h => h.CreateInboundActivityHandler(It.IsAny<Func<CircuitInboundActivityContext, Task>>()))
            .Returns((Func<CircuitInboundActivityContext, Task> next) => next)
            .Verifiable();

        handler1
            .InSequence(sequence)
            .Setup(h => h.CreateInboundActivityHandler(It.IsAny<Func<CircuitInboundActivityContext, Task>>()))
            .Returns((Func<CircuitInboundActivityContext, Task> next) => async (CircuitInboundActivityContext context) =>
            {
                asyncLocal1.Value = true;
                await next(context);
            })
            .Verifiable();

        var circuitHost = TestCircuitHost.Create(handlers: new[] { handler1.Object, handler2.Object, handler3.Object });
        var asyncLocal1ValueInHandler = false;
        var asyncLocal3ValueInHandler = false;

        // Act
        await circuitHost.HandleInboundActivityAsync(() =>
        {
            asyncLocal1ValueInHandler = asyncLocal1.Value;
            asyncLocal3ValueInHandler = asyncLocal3.Value;
            return Task.CompletedTask;
        });

        // Assert
        handler1.VerifyAll();
        handler2.VerifyAll();
        handler3.VerifyAll();

        Assert.False(asyncLocal1.Value);
        Assert.False(asyncLocal3.Value);

        Assert.True(asyncLocal1ValueInHandler);
        Assert.True(asyncLocal3ValueInHandler);
    }

    [Fact]
    public async Task HandleInboundActivityAsync_InvokesHandlerFunc_WhenNoCircuitActivityHandlersAreRegistered()
    {
        // Arrange
        var circuitHost = TestCircuitHost.Create();
        var wasHandlerFuncInvoked = false;

        // Act
        await circuitHost.HandleInboundActivityAsync(() =>
        {
            wasHandlerFuncInvoked = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.True(wasHandlerFuncInvoked);
    }

    [Fact]
    public async Task SendPersistedStateToClient_WithSuccessfulInvocation_ReturnsTrue()
    {
        // Arrange
        var mockClientProxy = new Mock<ISingleClientProxy>();
        mockClientProxy
            .Setup(c => c.InvokeCoreAsync<bool>(
                "JS.SavePersistedState",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var client = new CircuitClientProxy(mockClientProxy.Object, "connection-id");
        var circuitHost = TestCircuitHost.Create(clientProxy: client);

        var rootComponents = "mock-root-components";
        var applicationState = "mock-application-state";
        var cancellationToken = new CancellationToken();

        // Act
        var result = await circuitHost.SendPersistedStateToClient(rootComponents, applicationState, cancellationToken);

        // Assert
        Assert.True(result);
        mockClientProxy.Verify(
            c => c.InvokeCoreAsync<bool>(
                "JS.SavePersistedState",
                It.Is<object[]>(args => args[0].Equals(circuitHost.CircuitId.Secret) &&
                                        args[1].Equals(rootComponents) &&
                                        args[2].Equals(applicationState)),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task SendPersistedStateToClient_WithFailedInvocation_ReturnsFalse()
    {
        // Arrange
        var mockClientProxy = new Mock<ISingleClientProxy>();
        mockClientProxy
            .Setup(c => c.InvokeCoreAsync<bool>(
                "JS.SavePersistedState",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var client = new CircuitClientProxy(mockClientProxy.Object, "connection-id");
        var circuitHost = TestCircuitHost.Create(clientProxy: client);

        var rootComponents = "mock-root-components";
        var applicationState = "mock-application-state";
        var cancellationToken = new CancellationToken();

        // Act
        var result = await circuitHost.SendPersistedStateToClient(rootComponents, applicationState, cancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendPersistedStateToClient_WithException_LogsAndReturnsFalse()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        var mockClientProxy = new Mock<ISingleClientProxy>();
        mockClientProxy
            .Setup(c => c.InvokeCoreAsync<bool>(
                "JS.SavePersistedState",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var client = new CircuitClientProxy(mockClientProxy.Object, "connection-id");
        var circuitHost = TestCircuitHost.Create(clientProxy: client);

        var rootComponents = "mock-root-components";
        var applicationState = "mock-application-state";
        var cancellationToken = new CancellationToken();

        // Act
        var result = await circuitHost.SendPersistedStateToClient(rootComponents, applicationState, cancellationToken);

        // Assert
        Assert.False(result);
        mockClientProxy.Verify(
            c => c.InvokeCoreAsync<bool>(
                "JS.SavePersistedState",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendPersistedStateToClient_WithDisconnectedClient_ReturnsFalse()
    {
        // Arrange
        var client = new CircuitClientProxy(); // Creates a disconnected client
        var circuitHost = TestCircuitHost.Create(clientProxy: client);

        var rootComponents = "mock-root-components";
        var applicationState = "mock-application-state";
        var cancellationToken = new CancellationToken();

        // Act & Assert
        Assert.False(await circuitHost.SendPersistedStateToClient(rootComponents, applicationState, cancellationToken));
    }

    private static async Task<CircuitHost> CreateConnectedCircuitHostAsync(
        Mock<ISingleClientProxy> mockProxy = null,
        bool initialize = true)
    {
        var ownsProxy = mockProxy is null;
        mockProxy ??= new Mock<ISingleClientProxy>();

        if (ownsProxy)
        {
            mockProxy
                .Setup(c => c.SendCoreAsync("JS.RequestPause", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        var client = new CircuitClientProxy(mockProxy.Object, "connection-id");
        var circuitHost = TestCircuitHost.Create(clientProxy: client);

        if (initialize)
        {
            await circuitHost.InitializeAsync(
                new ProtectedPrerenderComponentApplicationStore(new EphemeralDataProtectionProvider()),
                default,
                CancellationToken.None);

            // TestCircuitHost has no descriptors so InitializeAsync skips OnConnectionUpAsync.
            await circuitHost.Renderer.Dispatcher.InvokeAsync(
                () => circuitHost.OnConnectionUpAsync(CancellationToken.None));
        }

        return circuitHost;
    }

    [Fact]
    public async Task AliveConnectedIdle_ReturnsTrueAndSendsMessage()
    {
        var proxy = new Mock<ISingleClientProxy>();
        proxy.Setup(c => c.SendCoreAsync("JS.RequestPause", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        var circuitHost = await CreateConnectedCircuitHostAsync(proxy);

        var result = await circuitHost.RequestPauseAsync(CancellationToken.None);

        Assert.True(result);
        proxy.Verify(c => c.SendCoreAsync("JS.RequestPause",
            It.Is<object[]>(a => a.Length == 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConnectedWhileDispatcherBusy_ReturnsTrueAndSendsMessage()
    {
        var proxy = new Mock<ISingleClientProxy>();
        proxy.Setup(c => c.SendCoreAsync("JS.RequestPause", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        var circuitHost = await CreateConnectedCircuitHostAsync(proxy);

        var tcs = new TaskCompletionSource();
        var dispatcherTask = circuitHost.Renderer.Dispatcher.InvokeAsync(() => tcs.Task);

        var result = await circuitHost.RequestPauseAsync(CancellationToken.None);

        Assert.True(result);
        proxy.Verify(c => c.SendCoreAsync("JS.RequestPause",
            It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);

        tcs.SetResult();
        await dispatcherTask;
    }

    [Fact]
    public async Task PauseWhileAsyncHandlerSuspended_NoUnobservedExceptions()
    {
        var proxy = new Mock<ISingleClientProxy>();
        proxy.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        var circuitHost = await CreateConnectedCircuitHostAsync(proxy);

        var unhandledExceptions = new List<Exception>();
        circuitHost.UnhandledException += (_, e) =>
            unhandledExceptions.Add((Exception)e.ExceptionObject);

        // Simulate an async event handler that suspends at an await point.
        var asyncWorkTcs = new TaskCompletionSource();
        var handlerStarted = new TaskCompletionSource();
        var handlerTask = circuitHost.Renderer.Dispatcher.InvokeAsync(async () =>
        {
            handlerStarted.SetResult();
            // Simulates: await Http.GetAsync(...) — dispatcher is released here.
            await asyncWorkTcs.Task;
            // This continuation runs after the circuit is disposed.
            // Any attempt to render or use JSRuntime will fail.
        });

        // Wait for the handler to reach the await point.
        await handlerStarted.Task;

        // Pause succeeds — the dispatcher is free (handler is suspended).
        var result = await circuitHost.RequestPauseAsync(CancellationToken.None);
        Assert.True(result);

        // Dispose the circuit (simulating what PauseCircuitAsync does after persistence).
        await circuitHost.DisposeAsync();

        // Release the async work — continuation runs on a disposed circuit.
        asyncWorkTcs.SetResult();

        // Wait for the handler to complete.
        // The continuation should not throw unobserved exceptions.
        await handlerTask;

        Assert.Empty(unhandledExceptions);
    }

    [Fact]
    public async Task PauseFromHandler_PauseMessageSentBeforeRenderBatch()
    {
        var messageOrder = new List<string>();
        var proxy = new Mock<ISingleClientProxy>();
        proxy.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
             .Callback((string method, object[] _, CancellationToken _) => messageOrder.Add(method))
             .Returns(Task.CompletedTask);

        var client = new CircuitClientProxy(proxy.Object, "connection-id");
        var remoteRenderer = GetRemoteRenderer();
        var circuitHost = TestCircuitHost.Create(clientProxy: client, remoteRenderer: remoteRenderer);
        await circuitHost.InitializeAsync(
            new ProtectedPrerenderComponentApplicationStore(new EphemeralDataProtectionProvider()),
            default, CancellationToken.None);
        await circuitHost.Renderer.Dispatcher.InvokeAsync(
            () => circuitHost.OnConnectionUpAsync(CancellationToken.None));

        messageOrder.Clear();

        // Simulate an event handler that mutates state and triggers pause.
        await circuitHost.Renderer.Dispatcher.InvokeAsync(async () =>
        {
            // Mutate state — this will cause a render after the handler returns.
            var component = new TestComponent(builder =>
            {
                builder.AddContent(0, "rendered");
            });
            circuitHost.Renderer.AssignRootComponentId(component);

            // Trigger pause — SendCoreAsync("JS.RequestPause") is called NOW.
            await circuitHost.RequestPauseAsync(CancellationToken.None);
        });

        // JS.RequestPause is sent inside the handler.
        // JS.RenderBatch (if sent) comes after the handler completes.
        var pauseIndex = messageOrder.IndexOf("JS.RequestPause");
        Assert.True(pauseIndex >= 0, "JS.RequestPause should have been sent");

        var renderIndex = messageOrder.IndexOf("JS.RenderBatch");
        if (renderIndex >= 0)
        {
            Assert.True(pauseIndex < renderIndex,
                "JS.RequestPause should be sent before JS.RenderBatch");
        }
    }

    [Fact]
    public async Task PauseFromOutside_WhileSyncRenderBlocked_PauseWaitsForRender()
    {
        var renderReleased = false;
        var pauseSentAfterRelease = false;
        var proxy = new Mock<ISingleClientProxy>();
        proxy.Setup(c => c.SendCoreAsync("JS.RequestPause", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
             .Callback(() => pauseSentAfterRelease = renderReleased)
             .Returns(Task.CompletedTask);

        var client = new CircuitClientProxy(proxy.Object, "connection-id");
        var circuitHost = TestCircuitHost.Create(clientProxy: client);
        await circuitHost.InitializeAsync(
            new ProtectedPrerenderComponentApplicationStore(new EphemeralDataProtectionProvider()),
            default, CancellationToken.None);
        await circuitHost.Renderer.Dispatcher.InvokeAsync(
            () => circuitHost.OnConnectionUpAsync(CancellationToken.None));

        // ManualResetEventSlim is used because the callback is synchronous (simulating sync rendering).
        var renderStarted = new ManualResetEventSlim();
        var releaseRender = new ManualResetEventSlim();

        var dispatcherTask = Task.Run(() => circuitHost.Renderer.Dispatcher.InvokeAsync(() =>
        {
            renderStarted.Set();
            releaseRender.Wait();
        }));

        renderStarted.Wait();

        var pauseTask = Task.Run(() => circuitHost.RequestPauseAsync(CancellationToken.None).AsTask());

        renderReleased = true;
        releaseRender.Set();
        await dispatcherTask;

        var result = await pauseTask;
        Assert.True(result);
        Assert.True(pauseSentAfterRelease, "Pause should be sent after sync render finishes");
    }

    [Fact]
    public async Task DispatchedPause_WhileSyncWorkBlocked_PauseWaitsForSyncWork()
    {
        var syncWorkReleased = false;
        var pauseSentAfterRelease = false;
        var proxy = new Mock<ISingleClientProxy>();
        proxy.Setup(c => c.SendCoreAsync("JS.RequestPause", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
             .Callback(() => pauseSentAfterRelease = syncWorkReleased)
             .Returns(Task.CompletedTask);
        var circuitHost = await CreateConnectedCircuitHostAsync(proxy);

        var syncWorkStarted = new ManualResetEventSlim();
        var releaseSyncWork = new ManualResetEventSlim();

        var dispatcherTask = Task.Run(() => circuitHost.Renderer.Dispatcher.InvokeAsync(() =>
        {
            syncWorkStarted.Set();
            releaseSyncWork.Wait();
        }));

        syncWorkStarted.Wait();

        var pauseTask = Task.Run(() => circuitHost.Renderer.Dispatcher.InvokeAsync(
            async () => await circuitHost.RequestPauseAsync(CancellationToken.None)));

        syncWorkReleased = true;
        releaseSyncWork.Set();
        await dispatcherTask;

        var result = await pauseTask;
        Assert.True(result);
        Assert.True(pauseSentAfterRelease, "Pause should be sent after sync work finishes");
    }

    [Fact]
    public async Task Disposed_ReturnsFalseNoMessage()
    {
        var proxy = new Mock<ISingleClientProxy>();
        var circuitHost = await CreateConnectedCircuitHostAsync(proxy);
        await circuitHost.DisposeAsync();

        Assert.False(await circuitHost.RequestPauseAsync(CancellationToken.None));
        proxy.Verify(c => c.SendCoreAsync(It.IsAny<string>(),
            It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Disconnected_ReturnsFalse()
    {
        var proxy = new Mock<ISingleClientProxy>();
        var client = new CircuitClientProxy(proxy.Object, "connection-id");
        var circuitHost = TestCircuitHost.Create(clientProxy: client);
        await circuitHost.InitializeAsync(
            new ProtectedPrerenderComponentApplicationStore(new EphemeralDataProtectionProvider()),
            default, CancellationToken.None);
        await circuitHost.Renderer.Dispatcher.InvokeAsync(
            () => circuitHost.OnConnectionUpAsync(CancellationToken.None));

        client.SetDisconnected();

        Assert.False(await circuitHost.RequestPauseAsync(CancellationToken.None));
        proxy.Verify(c => c.SendCoreAsync(It.IsAny<string>(),
            It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NotInitialized_ReturnsFalse()
    {
        var circuitHost = await CreateConnectedCircuitHostAsync(initialize: false);

        Assert.False(await circuitHost.RequestPauseAsync(CancellationToken.None));
    }

    [Fact]
    public async Task AlreadyPausedAndDisposed_ReturnsFalse()
    {
        var circuitHost = await CreateConnectedCircuitHostAsync();
        Assert.True(await circuitHost.RequestPauseAsync(CancellationToken.None));

        await circuitHost.DisposeAsync();

        Assert.False(await circuitHost.RequestPauseAsync(CancellationToken.None));
    }

    [Fact]
    public async Task PauseInProgress_IdempotentReturnsTrue()
    {
        var proxy = new Mock<ISingleClientProxy>();
        proxy.Setup(c => c.SendCoreAsync("JS.RequestPause", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        var circuitHost = await CreateConnectedCircuitHostAsync(proxy);

        Assert.True(await circuitHost.RequestPauseAsync(CancellationToken.None));
        Assert.True(await circuitHost.RequestPauseAsync(CancellationToken.None));

        proxy.Verify(c => c.SendCoreAsync("JS.RequestPause",
            It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CancelledBeforeSend_ReturnsFalse()
    {
        var circuitHost = await CreateConnectedCircuitHostAsync();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.False(await circuitHost.RequestPauseAsync(cts.Token));
    }

    [Fact]
    public async Task CancelledAfterSend_ReturnsTrue()
    {
        using var cts = new CancellationTokenSource();
        var proxy = new Mock<ISingleClientProxy>();
        proxy.Setup(c => c.SendCoreAsync("JS.RequestPause", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        var circuitHost = await CreateConnectedCircuitHostAsync(proxy);

        var result = await circuitHost.RequestPauseAsync(cts.Token);
        cts.Cancel();

        Assert.True(result);
    }

    [Fact]
    public async Task SendSucceeds_ReturnsTrue_EvenIfClientNeverReceives()
    {
        var circuitHost = await CreateConnectedCircuitHostAsync();

        Assert.True(await circuitHost.RequestPauseAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SendThrows_ReturnsFalse()
    {
        var proxy = new Mock<ISingleClientProxy>();
        proxy.Setup(c => c.SendCoreAsync("JS.RequestPause", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new IOException("Connection reset"));
        var circuitHost = await CreateConnectedCircuitHostAsync(proxy);

        Assert.False(await circuitHost.RequestPauseAsync(CancellationToken.None));
    }

    [Fact]
    public async Task AfterReconnect_SendsOnNewConnection()
    {
        var oldProxy = new Mock<ISingleClientProxy>();
        var newProxy = new Mock<ISingleClientProxy>();
        newProxy.Setup(c => c.SendCoreAsync("JS.RequestPause", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var client = new CircuitClientProxy(oldProxy.Object, "old-connection");
        var circuitHost = TestCircuitHost.Create(clientProxy: client);
        await circuitHost.InitializeAsync(
            new ProtectedPrerenderComponentApplicationStore(new EphemeralDataProtectionProvider()),
            default, CancellationToken.None);
        await circuitHost.Renderer.Dispatcher.InvokeAsync(
            () => circuitHost.OnConnectionUpAsync(CancellationToken.None));

        client.Transfer(newProxy.Object, "new-connection");

        Assert.True(await circuitHost.RequestPauseAsync(CancellationToken.None));
        newProxy.Verify(c => c.SendCoreAsync("JS.RequestPause",
            It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        oldProxy.Verify(c => c.SendCoreAsync(It.IsAny<string>(),
            It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DisconnectedCache_ReturnsFalse()
    {
        var client = new CircuitClientProxy(Mock.Of<ISingleClientProxy>(), "conn");
        var circuitHost = TestCircuitHost.Create(clientProxy: client);
        await circuitHost.InitializeAsync(
            new ProtectedPrerenderComponentApplicationStore(new EphemeralDataProtectionProvider()),
            default, CancellationToken.None);
        await circuitHost.Renderer.Dispatcher.InvokeAsync(
            () => circuitHost.OnConnectionUpAsync(CancellationToken.None));

        client.SetDisconnected();

        Assert.False(await circuitHost.RequestPauseAsync(CancellationToken.None));
    }

    [Fact]
    public async Task MultipleConcurrentCalls_AllReturnTrue()
    {
        var circuitHost = await CreateConnectedCircuitHostAsync();

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => circuitHost.RequestPauseAsync(CancellationToken.None).AsTask())
            .ToArray();
        var results = await Task.WhenAll(tasks);

        Assert.All(results, Assert.True);
    }

    [Fact]
    public async Task PauseDisposeRepause_OldRefReturnsFalse()
    {
        var circuitHost = await CreateConnectedCircuitHostAsync();
        Assert.True(await circuitHost.RequestPauseAsync(CancellationToken.None));

        await circuitHost.DisposeAsync();

        Assert.False(await circuitHost.RequestPauseAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StaleReference_ReturnsFalse()
    {
        var circuitHost = await CreateConnectedCircuitHostAsync();
        var circuit = circuitHost.Circuit;
        await circuitHost.DisposeAsync();

        Assert.False(await circuit.RequestCircuitPauseAsync());
    }

    [Fact]
    public async Task DrainMultipleCircuits_AllAccepted()
    {
        var hosts = new List<CircuitHost>();
        for (var i = 0; i < 5; i++)
        {
            hosts.Add(await CreateConnectedCircuitHostAsync());
        }

        var tasks = hosts.Select(h => h.RequestPauseAsync(CancellationToken.None).AsTask()).ToArray();
        var results = await Task.WhenAll(tasks);

        Assert.All(results, Assert.True);
    }

    [Fact]
    public async Task HostShutdownCancels_ReturnsFalse()
    {
        var tcs = new TaskCompletionSource();
        var proxy = new Mock<ISingleClientProxy>();
        proxy.Setup(c => c.SendCoreAsync("JS.RequestPause", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
             .Returns((string _, object[] _, CancellationToken ct) =>
             {
                 ct.ThrowIfCancellationRequested();
                 return tcs.Task;
             });
        var circuitHost = await CreateConnectedCircuitHostAsync(proxy);

        using var cts = new CancellationTokenSource();
        var pauseTask = circuitHost.RequestPauseAsync(cts.Token);

        cts.Cancel();
        tcs.SetCanceled();

        Assert.False(await pauseTask);
    }

    [Fact]
    public async Task RecoverableRenderingError_StillConnected_ReturnsTrue()
    {
        var circuitHost = await CreateConnectedCircuitHostAsync();

        Assert.True(await circuitHost.RequestPauseAsync(CancellationToken.None));
    }

    [Fact]
    public async Task FatalRenderingError_Disposed_ReturnsFalse()
    {
        var circuitHost = await CreateConnectedCircuitHostAsync();
        await circuitHost.DisposeAsync();

        Assert.False(await circuitHost.RequestPauseAsync(CancellationToken.None));
    }

    // Initialized but OnConnectionUpAsync not yet fired (Blazor Web path without descriptors).
    [Fact]
    public async Task DuringOnCircuitOpenedAsync_ReturnsFalse()
    {
        var proxy = new Mock<ISingleClientProxy>();
        proxy.Setup(c => c.SendCoreAsync("JS.RequestPause", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var circuitHost = await CreateConnectedCircuitHostAsync(proxy, initialize: false);
        await circuitHost.InitializeAsync(
            new ProtectedPrerenderComponentApplicationStore(new EphemeralDataProtectionProvider()),
            default,
            CancellationToken.None);

        var result = await circuitHost.RequestPauseAsync(CancellationToken.None);
        Assert.False(result);

        proxy.Verify(c => c.SendCoreAsync(It.IsAny<string>(),
            It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClientUnresponsive_ServerReturnsTrue()
    {
        var circuitHost = await CreateConnectedCircuitHostAsync();

        Assert.True(await circuitHost.RequestPauseAsync(CancellationToken.None));
    }

    [Fact]
    public async Task PublicApi_DelegatesToCircuitHost_Connected()
    {
        var proxy = new Mock<ISingleClientProxy>();
        proxy.Setup(c => c.SendCoreAsync("JS.RequestPause", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        var circuitHost = await CreateConnectedCircuitHostAsync(proxy);

        Assert.True(await circuitHost.Circuit.RequestCircuitPauseAsync());
        proxy.Verify(c => c.SendCoreAsync("JS.RequestPause",
            It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublicApi_DelegatesToCircuitHost_Disconnected()
    {
        var client = new CircuitClientProxy();
        var circuitHost = TestCircuitHost.Create(clientProxy: client);

        Assert.False(await circuitHost.Circuit.RequestCircuitPauseAsync());
    }

    [Fact]
    public async Task CalledFromDispatcher_NoDeadlock_ReturnsTrue()
    {
        var circuitHost = await CreateConnectedCircuitHostAsync();

        var result = await circuitHost.Renderer.Dispatcher.InvokeAsync(
            async () => await circuitHost.Circuit.RequestCircuitPauseAsync());

        Assert.True(result);
    }

    [Fact]
    public async Task CalledFromBackgroundThread_ReturnsTrue()
    {
        var circuitHost = await CreateConnectedCircuitHostAsync();

        var result = await Task.Run(() => circuitHost.Circuit.RequestCircuitPauseAsync().AsTask());

        Assert.True(result);
    }

    [Fact]
    public async Task UpdateRootComponents_CanAddNewRootComponent()
    {
        // Arrange
        var circuitHost = TestCircuitHost.Create(
            remoteRenderer: GetRemoteRenderer(),
            serviceScope: new ServiceCollection().BuildServiceProvider().CreateAsyncScope());
        var expectedMessage = "Hello, world!";
        Dictionary<string, object> parameters = new()
        {
            [nameof(DynamicallyAddedComponent.Message)] = expectedMessage,
        };

        // Act
        await AddComponentAsync<DynamicallyAddedComponent>(circuitHost, 1, parameters);

        // Assert
        var componentState = ((TestRemoteRenderer)circuitHost.Renderer).GetTestComponentState(0);
        var component = Assert.IsType<DynamicallyAddedComponent>(componentState.Component);
        Assert.Equal(expectedMessage, component.Message);
    }

    [Fact]
    public async Task UpdateRootComponents_CanUpdateExistingRootComponent()
    {
        // Arrange
        var circuitHost = TestCircuitHost.Create(
            remoteRenderer: GetRemoteRenderer(),
            serviceScope: new ServiceCollection().BuildServiceProvider().CreateAsyncScope());
        var componentKey = "mykey";

        await AddComponentAsync<DynamicallyAddedComponent>(circuitHost, 1, null, componentKey);

        var expectedMessage = "Updated message";
        Dictionary<string, object> parameters = new()
        {
            [nameof(DynamicallyAddedComponent.Message)] = expectedMessage,
        };

        // Act
        await UpdateComponentAsync<DynamicallyAddedComponent>(circuitHost, 1, parameters, componentKey);

        // Assert
        var componentState = ((TestRemoteRenderer)circuitHost.Renderer).GetTestComponentState(0);
        var component = Assert.IsType<DynamicallyAddedComponent>(componentState.Component);
        Assert.Equal(expectedMessage, component.Message);
    }

    [Fact]
    public async Task UpdateRootComponents_CanReplaceExistingRootComponent_WhenNoComponentKeyWasSpecified()
    {
        // Arrange
        var circuitHost = TestCircuitHost.Create(
            remoteRenderer: GetRemoteRenderer(),
            serviceScope: new ServiceCollection().BuildServiceProvider().CreateAsyncScope());

        await AddComponentAsync<DynamicallyAddedComponent>(circuitHost, 1);

        var expectedMessage = "Updated message";
        Dictionary<string, object> parameters = new()
        {
            [nameof(DynamicallyAddedComponent.Message)] = expectedMessage,
        };

        // Act
        await UpdateComponentAsync<DynamicallyAddedComponent>(circuitHost, 1, parameters);

        // Assert
        Assert.Throws<ArgumentException>(() =>
            ((TestRemoteRenderer)circuitHost.Renderer).GetTestComponentState(0));
        var componentState = ((TestRemoteRenderer)circuitHost.Renderer).GetTestComponentState(1);
        var component = Assert.IsType<DynamicallyAddedComponent>(componentState.Component);
        Assert.Equal(expectedMessage, component.Message);
    }

    [Fact]
    public async Task UpdateRootComponents_DoesNotUpdateExistingRootComponent_WhenDescriptorComponentTypeDoesNotMatchRootComponentType()
    {
        // Arrange
        var circuitHost = TestCircuitHost.Create(
            remoteRenderer: GetRemoteRenderer(),
            serviceScope: new ServiceCollection().BuildServiceProvider().CreateAsyncScope());

        // Arrange
        var expectedMessage = "Existing message";
        await AddComponentAsync<DynamicallyAddedComponent>(circuitHost, 1, new Dictionary<string, object>()
        {
            [nameof(DynamicallyAddedComponent.Message)] = expectedMessage,
        });

        await AddComponentAsync<TestComponent>(circuitHost, 2, []);

        Dictionary<string, object> parameters = new()
        {
            [nameof(DynamicallyAddedComponent.Message)] = "Updated message",
        };

        // Act
        var evt = await Assert.RaisesAsync<UnhandledExceptionEventArgs>(
            handler => circuitHost.UnhandledException += new UnhandledExceptionEventHandler(handler),
            handler => circuitHost.UnhandledException -= new UnhandledExceptionEventHandler(handler),
            () => UpdateComponentAsync<TestComponent /* Note the incorrect component type */>(circuitHost, 1, parameters));

        // Assert
        var componentState = ((TestRemoteRenderer)circuitHost.Renderer).GetTestComponentState(0);
        var component = Assert.IsType<DynamicallyAddedComponent>(componentState.Component);
        Assert.Equal(expectedMessage, component.Message);

        Assert.NotNull(evt);
        var exception = Assert.IsType<InvalidOperationException>(evt.Arguments.ExceptionObject);
        Assert.Equal("Cannot update components with mismatching types.", exception.Message);
    }

    [Fact]
    public async Task UpdateRootComponents_DoesNotUpdateExistingRootComponent_WhenDescriptorKeyDoesNotMatchOriginalKey()
    {
        // Arrange
        var circuitHost = TestCircuitHost.Create(
            remoteRenderer: GetRemoteRenderer(),
            serviceScope: new ServiceCollection().BuildServiceProvider().CreateAsyncScope());

        // Arrange
        var originalKey = "original_key";
        var expectedMessage = "Existing message";
        await AddComponentAsync<DynamicallyAddedComponent>(circuitHost, 1, new Dictionary<string, object>()
        {
            [nameof(DynamicallyAddedComponent.Message)] = expectedMessage,
        }, originalKey);

        Dictionary<string, object> parameters = new()
        {
            [nameof(DynamicallyAddedComponent.Message)] = "Updated message",
        };

        // Act
        var evt = await Assert.RaisesAsync<UnhandledExceptionEventArgs>(
            handler => circuitHost.UnhandledException += new UnhandledExceptionEventHandler(handler),
            handler => circuitHost.UnhandledException -= new UnhandledExceptionEventHandler(handler),
            () => UpdateComponentAsync<DynamicallyAddedComponent>(circuitHost, 1, parameters, componentKey: "new_key"));

        // Assert
        var componentState = ((TestRemoteRenderer)circuitHost.Renderer).GetTestComponentState(0);
        var component = Assert.IsType<DynamicallyAddedComponent>(componentState.Component);
        Assert.Equal(expectedMessage, component.Message);

        Assert.NotNull(evt);
        var exception = Assert.IsType<InvalidOperationException>(evt.Arguments.ExceptionObject);
        Assert.Equal("Cannot update components with mismatching keys.", exception.Message);
    }

    [Fact]
    public async Task UpdateRootComponents_CanRemoveExistingRootComponent()
    {
        // Arrange
        var circuitHost = TestCircuitHost.Create(
            remoteRenderer: GetRemoteRenderer(),
            serviceScope: new ServiceCollection().BuildServiceProvider().CreateAsyncScope());
        var expectedMessage = "Updated message";

        Dictionary<string, object> parameters = new()
        {
            [nameof(DynamicallyAddedComponent.Message)] = expectedMessage,
        };
        await AddComponentAsync<DynamicallyAddedComponent>(circuitHost, 1, parameters);

        // Act
        await RemoveComponentAsync(circuitHost, 1);

        // Assert
        Assert.Throws<ArgumentException>(() =>
            ((TestRemoteRenderer)circuitHost.Renderer).GetTestComponentState(0));
    }

    [Fact]
    public async Task UpdateRootComponents_ValidatesOperationSequencingDuringValueUpdateRestore()
    {
        // Arrange
        var testRenderer = GetRemoteRenderer();
        var circuitHost = TestCircuitHost.Create(
            remoteRenderer: testRenderer);

        // Set up initial components for subsequent operations
        await AddComponentAsync<DynamicallyAddedComponent>(circuitHost, 0, new Dictionary<string, object>
        {
            [nameof(DynamicallyAddedComponent.Message)] = "Component 0"
        });
        await AddComponentAsync<DynamicallyAddedComponent>(circuitHost, 1, new Dictionary<string, object>
        {
            [nameof(DynamicallyAddedComponent.Message)] = "Component 1"
        });

        Assert.Equal(2, testRenderer.GetOrCreateWebRootComponentManager().GetRootComponents().Count());
        var store = new TestComponentApplicationStore(
            new Dictionary<string, byte[]> { ["test"] = [1, 2, 3] });

        var operations = new RootComponentOperation[]
        {
            new()
            {
                Type = RootComponentOperationType.Add,
                SsrComponentId = 2,
                Marker = CreateMarker(typeof(DynamicallyAddedComponent), "2", new Dictionary<string, object>
                {
                    [nameof(DynamicallyAddedComponent.Message)] = "New Component 2"
                }),
                Descriptor = new(
                    componentType: typeof(DynamicallyAddedComponent),
                    parameters: CreateWebRootComponentParameters(new Dictionary<string, object>
                    {
                        [nameof(DynamicallyAddedComponent.Message)] = "New Component 2"
                    })),
            },

            new()
            {
                Type = RootComponentOperationType.Remove,
                SsrComponentId = 0,
            },

            new()
            {
                Type = RootComponentOperationType.Update,
                SsrComponentId = 1,
                Marker = CreateMarker(typeof(DynamicallyAddedComponent), "1", new Dictionary<string, object>
                {
                    [nameof(DynamicallyAddedComponent.Message)] = "Replaced Component 1"
                }),
                Descriptor = new(
                    componentType: typeof(DynamicallyAddedComponent),
                    parameters: CreateWebRootComponentParameters(new Dictionary<string, object>
                    {
                        [nameof(DynamicallyAddedComponent.Message)] = "Replaced Component 1"
                    })),
            },
        };

        var batch = new RootComponentOperationBatch
        {
            BatchId = 1,
            Operations = operations
        };

        var updateTask = circuitHost.UpdateRootComponents(batch, store, false, CancellationToken.None);
        Assert.Equal(2, testRenderer.GetOrCreateWebRootComponentManager().GetRootComponents().Count());
        var dynamicallyAddedComponent1 = Assert.IsType<DynamicallyAddedComponent>(testRenderer.GetTestComponentState(3).Component);
        dynamicallyAddedComponent1.Updated = new ManualResetEvent(false);
        Assert.Equal("Default message", dynamicallyAddedComponent1.Message);
        var dynamicallyAddedComponent2 = Assert.IsType<DynamicallyAddedComponent>(testRenderer.GetTestComponentState(2).Component);
        dynamicallyAddedComponent2.Updated = new ManualResetEvent(false);
        Assert.Equal("Default message", dynamicallyAddedComponent2.Message);
        store.Continue();
        await updateTask;

        dynamicallyAddedComponent1.Updated.WaitOne();
        dynamicallyAddedComponent2.Updated.WaitOne();

        Assert.Equal("Replaced Component 1", Assert.IsType<DynamicallyAddedComponent>(testRenderer.GetTestComponentState(3).Component).Message);
        Assert.Equal("New Component 2", Assert.IsType<DynamicallyAddedComponent>(testRenderer.GetTestComponentState(2).Component).Message);

        Assert.Equal(2, testRenderer.GetOrCreateWebRootComponentManager().GetRootComponents().Count());
    }

    private async Task AddComponentAsync<TComponent>(CircuitHost circuitHost, int ssrComponentId, Dictionary<string, object> parameters = null, string componentKey = "")
        where TComponent : IComponent
    {
        var addOperation = new RootComponentOperation
        {
            Type = RootComponentOperationType.Add,
            SsrComponentId = ssrComponentId,
            Marker = CreateMarker(typeof(TComponent), ssrComponentId.ToString(CultureInfo.InvariantCulture), parameters, componentKey),
            Descriptor = new(
                componentType: typeof(TComponent),
                parameters: CreateWebRootComponentParameters(parameters)),
        };

        // Add component
        await circuitHost.UpdateRootComponents(new() { Operations = [addOperation] }, null, false, CancellationToken.None);
    }

    private async Task UpdateComponentAsync<TComponent>(CircuitHost circuitHost, int ssrComponentId, Dictionary<string, object> parameters = null, string componentKey = "")
    {
        var updateOperation = new RootComponentOperation
        {
            Type = RootComponentOperationType.Update,
            SsrComponentId = ssrComponentId,
            Marker = CreateMarker(typeof(TComponent), ssrComponentId.ToString(CultureInfo.InvariantCulture), parameters, componentKey),
            Descriptor = new(
                componentType: typeof(TComponent),
                parameters: CreateWebRootComponentParameters(parameters)),
        };

        // Update component
        await circuitHost.UpdateRootComponents(new() { Operations = [updateOperation] }, null, false, CancellationToken.None);
    }

    private async Task RemoveComponentAsync(CircuitHost circuitHost, int ssrComponentId)
    {
        var removeOperation = new RootComponentOperation
        {
            Type = RootComponentOperationType.Remove,
            SsrComponentId = ssrComponentId,
        };

        // Remove component
        await circuitHost.UpdateRootComponents(new() { Operations = [removeOperation] }, null, false, CancellationToken.None);
    }

    private ProtectedPrerenderComponentApplicationStore CreateStore()
    {
        return new ProtectedPrerenderComponentApplicationStore(_ephemeralDataProtectionProvider);
    }

    private ServerComponentDeserializer CreateDeserializer()
    {
        return new ServerComponentDeserializer(_ephemeralDataProtectionProvider, NullLogger<ServerComponentDeserializer>.Instance, new RootTypeCache(), new ComponentParameterDeserializer(NullLogger<ComponentParameterDeserializer>.Instance, new ComponentParametersTypeCache()));
    }

    private static TestRemoteRenderer GetRemoteRenderer()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(new Mock<IJSRuntime>().Object);
        return new TestRemoteRenderer(
            serviceCollection.BuildServiceProvider(),
            Mock.Of<ISingleClientProxy>());
    }

    private static void SetupMockInboundActivityHandlers(MockSequence sequence, params Mock<CircuitHandler>[] circuitHandlers)
    {
        for (var i = circuitHandlers.Length - 1; i >= 0; i--)
        {
            circuitHandlers[i]
                .InSequence(sequence)
                .Setup(h => h.CreateInboundActivityHandler(It.IsAny<Func<CircuitInboundActivityContext, Task>>()))
                .Returns((Func<CircuitInboundActivityContext, Task> next) => next)
                .Verifiable();
        }
    }

    private static void SetupMockInboundActivityHandler(Mock<CircuitHandler> circuitHandler)
    {
        circuitHandler
            .Setup(h => h.CreateInboundActivityHandler(It.IsAny<Func<CircuitInboundActivityContext, Task>>()))
            .Returns((Func<CircuitInboundActivityContext, Task> next) => next)
            .Verifiable();
    }

    private ComponentMarker CreateMarker(Type type, string locationHash, Dictionary<string, object> parameters = null, string componentKey = "")
    {
        var serializer = new ServerComponentSerializer(_ephemeralDataProtectionProvider);
        var key = new ComponentMarkerKey(locationHash, componentKey);
        var marker = ComponentMarker.Create(ComponentMarker.ServerMarkerType, false, key);
        serializer.SerializeInvocation(
            ref marker,
            _invocationSequence,
            type,
            parameters is null ? ParameterView.Empty : ParameterView.FromDictionary(parameters));
        return marker;
    }

    private static WebRootComponentParameters CreateWebRootComponentParameters(IDictionary<string, object> parameters = null)
    {
        if (parameters is null)
        {
            return WebRootComponentParameters.Empty;
        }

        var parameterView = ParameterView.FromDictionary(parameters);
        var (parameterDefinitions, parameterValues) = ComponentParameter.FromParameterView(parameterView);
        for (var i = 0; i < parameterValues.Count; i++)
        {
            // WebRootComponentParameters expects serialized parameter values to be JsonElements.
            var jsonElement = JsonSerializer.SerializeToElement(parameterValues[i]);
            parameterValues[i] = jsonElement;
        }
        return new WebRootComponentParameters(
            parameterView,
            parameterDefinitions.AsReadOnly(),
            parameterValues.AsReadOnly());
    }

    private class TestRemoteRenderer : RemoteRenderer
    {
        public TestRemoteRenderer(IServiceProvider serviceProvider, ISingleClientProxy client)
            : base(
                  serviceProvider,
                  NullLoggerFactory.Instance,
                  new CircuitOptions(),
                  new CircuitClientProxy(client, "connection"),
                  new TestServerComponentDeserializer(),
                  NullLogger.Instance,
                  CreateJSRuntime(new CircuitOptions()),
                  new CircuitJSComponentInterop(new CircuitOptions()))
        {
        }

        public ComponentState GetTestComponentState(int id)
            => base.GetComponentState(id);

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

    private class TestServerComponentDeserializer : IServerComponentDeserializer
    {
        public bool TryDeserializeComponentDescriptorCollection(string serializedComponentRecords, out List<ComponentDescriptor> descriptors)
        {
            descriptors = default;
            return true;
        }

        public bool TryDeserializeRootComponentOperations(string serializedComponentOperations, out RootComponentOperationBatch operationBatch, bool deserializeDescriptors = true)
        {
            operationBatch = default;
            return true;
        }

        public bool TryDeserializeWebRootComponentDescriptor(ComponentMarker record, [NotNullWhen(true)] out WebRootComponentDescriptor result)
        {
            result = default;
            return true;
        }
    }

    private class DynamicallyAddedComponent : IComponent, IDisposable
    {
        private readonly TaskCompletionSource _disposeTcs = new();
        private RenderHandle _renderHandle;

        [Parameter]
        public string Message { get; set; } = "Default message";

        public ManualResetEvent Updated { get; set; }

        private void Render(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Message);
        }

        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.TryGetValue<string>(nameof(Message), out var message))
            {
                Message = message;
            }

            TriggerRender();
            Updated?.Set();
            return Task.CompletedTask;
        }

        public void TriggerRender()
        {
            var task = _renderHandle.Dispatcher.InvokeAsync(() => _renderHandle.Render(Render));
            Assert.True(task.IsCompletedSuccessfully);
        }

        public Task WaitForDisposeAsync()
            => _disposeTcs.Task;

        public void Dispose()
        {
            _disposeTcs.SetResult();
        }
    }

    private class TestComponent() : IComponent, IHandleAfterRender
    {
        private RenderHandle _renderHandle;
        private readonly RenderFragment _renderFragment = (builder) =>
        {
            builder.OpenElement(0, "my element");
            builder.AddContent(1, "some text");
            builder.CloseElement();
        };

        public TestComponent(RenderFragment renderFragment) : this() => _renderFragment = renderFragment;

        public Action OnAfterRenderComplete { get; set; }

        public void Attach(RenderHandle renderHandle) => _renderHandle = renderHandle;

        public Task OnAfterRenderAsync()
        {
            OnAfterRenderComplete?.Invoke();
            return Task.CompletedTask;
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            TriggerRender();
            return Task.CompletedTask;
        }

        public void TriggerRender()
        {
            var task = _renderHandle.Dispatcher.InvokeAsync(() => _renderHandle.Render(_renderFragment));
            Assert.True(task.IsCompletedSuccessfully);
        }
    }

    private class TestComponentApplicationStore(Dictionary<string, byte[]> dictionary) : IPersistentComponentStateStore, IClearableStore
    {
        private readonly TaskCompletionSource _tcs = new();

        public void Clear() => dictionary.Clear();

        public async Task<IDictionary<string, byte[]>> GetPersistedStateAsync()
        {
            await _tcs.Task;
            return dictionary;
        }

        public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state) => throw new NotImplementedException();
        internal void Continue() => _tcs.SetResult();
    }
}

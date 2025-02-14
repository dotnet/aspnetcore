// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        SetupMockInboundActivityHandler(handler);

        handler
            .Setup(h => h.OnCircuitOpenedAsync(It.IsAny<Circuit>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task)
            .Verifiable();

        var circuitHost = TestCircuitHost.Create(handlers: new[] { handler.Object }, descriptors: [new ComponentDescriptor() ]);
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
        await circuitHost.UpdateRootComponents(new() { Operations = [addOperation] }, null, CancellationToken.None);
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
        await circuitHost.UpdateRootComponents(new() { Operations = [updateOperation] }, null, CancellationToken.None);
    }

    private async Task RemoveComponentAsync(CircuitHost circuitHost, int ssrComponentId)
    {
        var removeOperation = new RootComponentOperation
        {
            Type = RootComponentOperationType.Remove,
            SsrComponentId = ssrComponentId,
        };

        // Remove component
        await circuitHost.UpdateRootComponents(new() { Operations = [removeOperation] }, null, CancellationToken.None);
    }

    private ProtectedPrerenderComponentApplicationStore CreateStore()
    {
        return new ProtectedPrerenderComponentApplicationStore(_ephemeralDataProtectionProvider);
    }

    private ServerComponentDeserializer CreateDeserializer()
    {
        return new ServerComponentDeserializer(_ephemeralDataProtectionProvider, NullLogger<ServerComponentDeserializer>.Instance, new RootComponentTypeCache(), new ComponentParameterDeserializer(NullLogger<ComponentParameterDeserializer>.Instance, new ComponentParametersTypeCache()));
    }

    private static TestRemoteRenderer GetRemoteRenderer()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(new Mock<IJSRuntime>().Object);
        return new TestRemoteRenderer(
            serviceCollection.BuildServiceProvider(),
            Mock.Of<IClientProxy>());
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
        public TestRemoteRenderer(IServiceProvider serviceProvider, IClientProxy client)
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

        public bool TryDeserializeRootComponentOperations(string serializedComponentOperations, out RootComponentOperationBatch operationBatch)
        {
            operationBatch = default;
            return true;
        }
    }

    private class DynamicallyAddedComponent : IComponent, IDisposable
    {
        private readonly TaskCompletionSource _disposeTcs = new();
        private RenderHandle _renderHandle;

        [Parameter]
        public string Message { get; set; } = "Default message";

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
}

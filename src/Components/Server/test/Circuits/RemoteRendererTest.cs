// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.Web.Rendering;

public class RemoteRendererTest
{
    // Nothing should exceed the timeout in a successful run of the the tests, this is just here to catch
    // failures.
    private static readonly TimeSpan Timeout = Debugger.IsAttached ? System.Threading.Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10);

    private const int MaxInteractiveServerRootComponentCount = 3;

    private readonly IDataProtectionProvider _ephemeralDataProtectionProvider = new EphemeralDataProtectionProvider();

    [Fact]
    public void WritesAreBufferedWhenTheClientIsOffline()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var renderer = GetRemoteRenderer(serviceProvider);
        var component = new TestComponent(builder =>
        {
            builder.OpenElement(0, "my element");
            builder.AddContent(1, "some text");
            builder.CloseElement();
        });

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();
        component.TriggerRender();

        // Assert
        Assert.Equal(2, renderer._unacknowledgedRenderBatches.Count);
    }

    [Fact]
    public void NotAcknowledgingRenders_ProducesBatches_UpToTheLimit()
    {
        var serviceProvider = CreateServiceProvider();
        var renderer = GetRemoteRenderer(serviceProvider);
        var component = new TestComponent(builder =>
        {
            builder.OpenElement(0, "my element");
            builder.AddContent(1, "some text");
            builder.CloseElement();
        });

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        for (int i = 0; i < 20; i++)
        {
            component.TriggerRender();

        }

        // Assert
        Assert.Equal(10, renderer._unacknowledgedRenderBatches.Count);
    }

    [Fact]
    public async Task NoNewBatchesAreCreated_WhenThereAreNoPendingRenderRequestsFromComponents()
    {
        var serviceProvider = CreateServiceProvider();
        var renderer = GetRemoteRenderer(serviceProvider);
        var component = new TestComponent(builder =>
        {
            builder.OpenElement(0, "my element");
            builder.AddContent(1, "some text");
            builder.CloseElement();
        });

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        for (var i = 0; i < 10; i++)
        {
            component.TriggerRender();
        }

        await renderer.OnRenderCompletedAsync(2, null);

        // Assert
        Assert.Equal(9, renderer._unacknowledgedRenderBatches.Count);
    }

    [Fact]
    public async Task ProducesNewBatch_WhenABatchGetsAcknowledged()
    {
        var serviceProvider = CreateServiceProvider();
        var renderer = GetRemoteRenderer(serviceProvider);
        var i = 0;
        var component = new TestComponent(builder =>
        {
            builder.AddContent(0, $"Value {i}");
        });

        // Act
        var componentId = renderer.AssignRootComponentId(component);
        for (i = 0; i < 20; i++)
        {
            component.TriggerRender();
        }
        Assert.Equal(10, renderer._unacknowledgedRenderBatches.Count);

        await renderer.OnRenderCompletedAsync(2, null);

        // Assert
        Assert.Equal(10, renderer._unacknowledgedRenderBatches.Count);
    }

    [Fact]
    public async Task ProcessBufferedRenderBatches_WritesRenders()
    {
        // Arrange
        var @event = new ManualResetEventSlim();
        var serviceProvider = CreateServiceProvider();
        var renderIds = new List<long>();

        var firstBatchTCS = new TaskCompletionSource();
        var secondBatchTCS = new TaskCompletionSource();
        var thirdBatchTCS = new TaskCompletionSource();

        var initialClient = new Mock<IClientProxy>();
        initialClient.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback((string name, object[] value, CancellationToken token) => renderIds.Add((long)value[0]))
            .Returns(firstBatchTCS.Task);
        var circuitClient = new CircuitClientProxy(initialClient.Object, "connection0");
        var renderer = GetRemoteRenderer(serviceProvider, circuitClient);
        var component = new TestComponent(builder =>
        {
            builder.OpenElement(0, "my element");
            builder.AddContent(1, "some text");
            builder.CloseElement();
        });

        var client = new Mock<IClientProxy>();
        client.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback((string name, object[] value, CancellationToken token) => renderIds.Add((long)value[0]))
            .Returns<string, object[], CancellationToken>((n, v, t) => (long)v[0] == 3 ? secondBatchTCS.Task : thirdBatchTCS.Task);

        var componentId = renderer.AssignRootComponentId(component);
        component.TriggerRender();
        _ = renderer.OnRenderCompletedAsync(2, null);

        @event.Reset();
        firstBatchTCS.SetResult();

        // Waiting is required here because the continuations of SetResult will not execute synchronously.
        @event.Wait(Timeout);

        circuitClient.SetDisconnected();
        component.TriggerRender();
        component.TriggerRender();

        // Act
        circuitClient.Transfer(client.Object, "new-connection");
        var task = renderer.ProcessBufferedRenderBatches();

        foreach (var id in renderIds.ToArray())
        {
            _ = renderer.OnRenderCompletedAsync(id, null);
        }

        secondBatchTCS.SetResult();
        thirdBatchTCS.SetResult();

        // Assert
        Assert.Equal(new long[] { 2, 3, 4 }, renderIds);
        Assert.True(task.Wait(3000), "One or more render batches weren't acknowledged");

        await task;
    }

    [Fact]
    public async Task OnRenderCompletedAsync_DoesNotThrowWhenReceivedDuplicateAcks()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var firstBatchTCS = new TaskCompletionSource();
        var secondBatchTCS = new TaskCompletionSource();
        var offlineClient = new CircuitClientProxy(new Mock<IClientProxy>(MockBehavior.Strict).Object, "offline-client");
        offlineClient.SetDisconnected();
        var renderer = GetRemoteRenderer(serviceProvider, offlineClient);
        RenderFragment initialContent = (builder) =>
        {
            builder.OpenElement(0, "my element");
            builder.AddContent(1, "some text");
            builder.CloseElement();
        };
        var trigger = new Trigger();
        var renderIds = new List<long>();
        var onlineClient = new Mock<IClientProxy>();
        onlineClient.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback((string name, object[] value, CancellationToken token) => renderIds.Add((long)value[1]))
            .Returns<string, object[], CancellationToken>((n, v, t) => (long)v[1] == 2 ? firstBatchTCS.Task : secondBatchTCS.Task);

        // This produces the initial batch (id = 2)
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<AutoParameterTestComponent>(
            ParameterView.FromDictionary(new Dictionary<string, object>
            {
                [nameof(AutoParameterTestComponent.Content)] = initialContent,
                [nameof(AutoParameterTestComponent.Trigger)] = trigger
            })));
        trigger.Component.Content = (builder) =>
        {
            builder.OpenElement(0, "offline element");
            builder.AddContent(1, "offline text");
            builder.CloseElement();
        };
        // This produces an additional batch (id = 3)
        trigger.TriggerRender();
        var originallyQueuedBatches = renderer._unacknowledgedRenderBatches.Count;

        // Act
        offlineClient.Transfer(onlineClient.Object, "new-connection");
        var task = renderer.ProcessBufferedRenderBatches();
        var exceptions = new List<Exception>();
        renderer.UnhandledException += (sender, e) =>
        {
            exceptions.Add(e);
        };

        // Receive the ack for the initial batch
        _ = renderer.OnRenderCompletedAsync(2, null);
        // Receive the ack for the second batch
        _ = renderer.OnRenderCompletedAsync(3, null);

        firstBatchTCS.SetResult();
        secondBatchTCS.SetResult();
        // Repeat the ack for the third batch
        _ = renderer.OnRenderCompletedAsync(3, null);

        // Assert
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task OnRenderCompletedAsync_DoesNotThrowWhenThereAreNoPendingBatchesToAck()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var firstBatchTCS = new TaskCompletionSource();
        var secondBatchTCS = new TaskCompletionSource();
        var offlineClient = new CircuitClientProxy(new Mock<IClientProxy>(MockBehavior.Strict).Object, "offline-client");
        offlineClient.SetDisconnected();
        var renderer = GetRemoteRenderer(serviceProvider, offlineClient);
        RenderFragment initialContent = (builder) =>
        {
            builder.OpenElement(0, "my element");
            builder.AddContent(1, "some text");
            builder.CloseElement();
        };
        var trigger = new Trigger();
        var renderIds = new List<long>();
        var onlineClient = new Mock<IClientProxy>();
        onlineClient.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback((string name, object[] value, CancellationToken token) => renderIds.Add((long)value[1]))
            .Returns<string, object[], CancellationToken>((n, v, t) => (long)v[1] == 2 ? firstBatchTCS.Task : secondBatchTCS.Task);

        // This produces the initial batch (id = 2)
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<AutoParameterTestComponent>(
            ParameterView.FromDictionary(new Dictionary<string, object>
            {
                [nameof(AutoParameterTestComponent.Content)] = initialContent,
                [nameof(AutoParameterTestComponent.Trigger)] = trigger
            })));
        trigger.Component.Content = (builder) =>
        {
            builder.OpenElement(0, "offline element");
            builder.AddContent(1, "offline text");
            builder.CloseElement();
        };
        // This produces an additional batch (id = 3)
        trigger.TriggerRender();
        var originallyQueuedBatches = renderer._unacknowledgedRenderBatches.Count;

        // Act
        offlineClient.Transfer(onlineClient.Object, "new-connection");
        var task = renderer.ProcessBufferedRenderBatches();
        var exceptions = new List<Exception>();
        renderer.UnhandledException += (sender, e) =>
        {
            exceptions.Add(e);
        };

        // Receive the ack for the initial batch
        _ = renderer.OnRenderCompletedAsync(2, null);
        // Receive the ack for the second batch
        _ = renderer.OnRenderCompletedAsync(2, null);

        firstBatchTCS.SetResult();
        secondBatchTCS.SetResult();
        // Repeat the ack for the third batch
        _ = renderer.OnRenderCompletedAsync(3, null);

        // Assert
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task ConsumesAllPendingBatchesWhenReceivingAHigherSequenceBatchId()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var firstBatchTCS = new TaskCompletionSource();
        var secondBatchTCS = new TaskCompletionSource();
        var renderIds = new List<long>();

        var onlineClient = new Mock<IClientProxy>();
        onlineClient.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback((string name, object[] value, CancellationToken token) => renderIds.Add((long)value[1]))
            .Returns<string, object[], CancellationToken>((n, v, t) => (long)v[1] == 2 ? firstBatchTCS.Task : secondBatchTCS.Task);

        var renderer = GetRemoteRenderer(serviceProvider, new CircuitClientProxy(onlineClient.Object, "online-client"));
        RenderFragment initialContent = (builder) =>
        {
            builder.OpenElement(0, "my element");
            builder.AddContent(1, "some text");
            builder.CloseElement();
        };
        var trigger = new Trigger();

        // This produces the initial batch (id = 2)
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<AutoParameterTestComponent>(
            ParameterView.FromDictionary(new Dictionary<string, object>
            {
                [nameof(AutoParameterTestComponent.Content)] = initialContent,
                [nameof(AutoParameterTestComponent.Trigger)] = trigger
            })));
        trigger.Component.Content = (builder) =>
        {
            builder.OpenElement(0, "offline element");
            builder.AddContent(1, "offline text");
            builder.CloseElement();
        };
        // This produces an additional batch (id = 3)
        trigger.TriggerRender();
        var originallyQueuedBatches = renderer._unacknowledgedRenderBatches.Count;

        // Act
        var exceptions = new List<Exception>();
        renderer.UnhandledException += (sender, e) =>
        {
            exceptions.Add(e);
        };

        // Pretend that we missed the ack for the initial batch
        _ = renderer.OnRenderCompletedAsync(3, null);
        firstBatchTCS.SetResult();
        secondBatchTCS.SetResult();

        // Assert
        Assert.Empty(exceptions);
        Assert.Empty(renderer._unacknowledgedRenderBatches);
    }

    [Fact]
    public async Task ThrowsIfWeReceivedAnAcknowledgeForANeverProducedBatch()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var firstBatchTCS = new TaskCompletionSource();
        var secondBatchTCS = new TaskCompletionSource();
        var renderIds = new List<long>();

        var onlineClient = new Mock<IClientProxy>();
        onlineClient.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback((string name, object[] value, CancellationToken token) => renderIds.Add((long)value[1]))
            .Returns<string, object[], CancellationToken>((n, v, t) => (long)v[1] == 2 ? firstBatchTCS.Task : secondBatchTCS.Task);

        var renderer = GetRemoteRenderer(serviceProvider, new CircuitClientProxy(onlineClient.Object, "online-client"));
        RenderFragment initialContent = (builder) =>
        {
            builder.OpenElement(0, "my element");
            builder.AddContent(1, "some text");
            builder.CloseElement();
        };
        var trigger = new Trigger();

        // This produces the initial batch (id = 2)
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<AutoParameterTestComponent>(
            ParameterView.FromDictionary(new Dictionary<string, object>
            {
                [nameof(AutoParameterTestComponent.Content)] = initialContent,
                [nameof(AutoParameterTestComponent.Trigger)] = trigger
            })));
        trigger.Component.Content = (builder) =>
        {
            builder.OpenElement(0, "offline element");
            builder.AddContent(1, "offline text");
            builder.CloseElement();
        };
        // This produces an additional batch (id = 3)
        trigger.TriggerRender();
        var originallyQueuedBatches = renderer._unacknowledgedRenderBatches.Count;

        // Act
        var exceptions = new List<Exception>();
        renderer.UnhandledException += (sender, e) =>
        {
            exceptions.Add(e);
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => renderer.OnRenderCompletedAsync(4, null));
        firstBatchTCS.SetResult();
        secondBatchTCS.SetResult();

        // Assert
        Assert.Equal(
            "Received an acknowledgement for batch with id '4' when the last batch produced was '3'.",
            exception.Message);
    }

    [Fact]
    public async Task WebRootComponentManager_AddRootComponentAsync_Throws_IfMaxInteractiveServerComponentCountIsExceeded()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var renderer = GetRemoteRenderer(serviceProvider);

        // Act
        for (var i = 0; i < MaxInteractiveServerRootComponentCount; i++)
        {
            await AddWebRootComponentAsync(renderer, i);
        }

        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => AddWebRootComponentAsync(renderer, MaxInteractiveServerRootComponentCount));

        Assert.Equal("Exceeded the maximum number of allowed server interactive root components.", ex.Message);
    }

    [Fact]
    public async Task WebRootComponentManager_AddRootComponentAsync_Throws_IfDuplicateSsrComponentIdIsProvided()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var renderer = GetRemoteRenderer(serviceProvider);

        // Act
        await AddWebRootComponentAsync(renderer, 0);

        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => AddWebRootComponentAsync(renderer, 0));

        Assert.Equal("A root component with SSR component ID 0 already exists.", ex.Message);
    }

    [Fact]
    public async Task WebRootComponentManager_AddRootComponentAsync_Throws_IfKeyIsInvalid()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var renderer = GetRemoteRenderer(serviceProvider);

        // Act/assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var webRootComponentManager = renderer.GetOrCreateWebRootComponentManager();
            await webRootComponentManager.AddRootComponentAsync(
                0,
                typeof(TestComponent),
                default, // Invalid key
                WebRootComponentParameters.Empty);
        });

        Assert.Equal("An invalid component marker key was provided.", ex.Message);
    }

    [Fact]
    public async Task WebRootComponentManager_AddRootComponentAsync_CanAddAndRenderRootComponent()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var renderer = GetRemoteRenderer(serviceProvider);

        // Act
        await AddWebRootComponentAsync(renderer, 0);

        // Assert
        Assert.Single(renderer._unacknowledgedRenderBatches);
    }

    [Fact]
    public async Task WebRootComponentManager_UpdateRootComponentAsync_Throws_IfSsrComponentIdIsInvalid()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var renderer = GetRemoteRenderer(serviceProvider);

        // Act
        var key = await AddWebRootComponentAsync(renderer, 0);

        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var webRootComponentManager = renderer.GetOrCreateWebRootComponentManager();
            await webRootComponentManager.UpdateRootComponentAsync(1, typeof(TestComponent), key, WebRootComponentParameters.Empty);
        });

        Assert.Equal($"No root component exists with SSR component ID 1.", ex.Message);
    }

    [Fact]
    public async Task WebRootComponentManager_UpdateRootComponentAsync_Throws_IfKeyDoesNotMatch()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var renderer = GetRemoteRenderer(serviceProvider);

        // Act
        await AddWebRootComponentAsync(renderer, 0);

        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var webRootComponentManager = renderer.GetOrCreateWebRootComponentManager();
            await webRootComponentManager.UpdateRootComponentAsync(0, typeof(TestComponent), new("1", null), WebRootComponentParameters.Empty);
        });

        Assert.Equal("Cannot update components with mismatching keys.", ex.Message);
    }

    [Fact]
    public async Task WebRootComponentManager_UpdateRootComponentAsync_Works_IfComponentKeyWasSupplied()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var renderer = GetRemoteRenderer(serviceProvider);

        // Act
        var key = await AddWebRootComponentAsync(renderer, 0, "mykey");
        await renderer.Dispatcher.InvokeAsync(() =>
        {
            var webRootComponentManager = renderer.GetOrCreateWebRootComponentManager();
            var parameters = new Dictionary<string, object> { ["Name"] = "value" };
            webRootComponentManager.UpdateRootComponentAsync(0, typeof(TestComponent), key, CreateWebRootComponentParameters(parameters));
        });

        // Assert
        Assert.Equal(2, renderer._unacknowledgedRenderBatches.Count); // Initial render, re-render
    }

    [Fact]
    public async Task WebRootComponentManager_UpdateRootComponentAsync_DoesNothing_IfNoComponentKeyWasSuppliedAndParametersDidNotChange()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var renderer = GetRemoteRenderer(serviceProvider);

        // Act
        var key = await AddWebRootComponentAsync(renderer, 0);
        await renderer.Dispatcher.InvokeAsync(() =>
        {
            var webRootComponentManager = renderer.GetOrCreateWebRootComponentManager();
            webRootComponentManager.UpdateRootComponentAsync(0, typeof(TestComponent), key, WebRootComponentParameters.Empty);
        });

        // Assert
        Assert.Single(renderer._unacknowledgedRenderBatches);
    }

    [Fact]
    public async Task WebRootComponentManager_UpdateRootComponentAsync_ReinitializesComponent_IfNoComponentKeyWasSuppliedAndParameterChanged()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var renderer = GetRemoteRenderer(serviceProvider);

        // Act
        var key = await AddWebRootComponentAsync(renderer, 0);
        await renderer.Dispatcher.InvokeAsync(() =>
        {
            var webRootComponentManager = renderer.GetOrCreateWebRootComponentManager();
            var parameters = new Dictionary<string, object> { ["Name"] = "value" };
            webRootComponentManager.UpdateRootComponentAsync(0, typeof(TestComponent), key, CreateWebRootComponentParameters(parameters));
        });

        // Assert
        Assert.Equal(3, renderer._unacknowledgedRenderBatches.Count); // Initial render, dispose, and re-initialize
    }

    [Fact]
    public async Task WebRootComponentManager_RemoveRootComponent_Throws_IfSsrComponentIdIsInvalid()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var renderer = GetRemoteRenderer(serviceProvider);

        // Act
        var key = await AddWebRootComponentAsync(renderer, 0);

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(() => renderer.GetOrCreateWebRootComponentManager().RemoveRootComponent(1));

        Assert.Equal($"No root component exists with SSR component ID 1.", ex.Message);
    }

    [Fact]
    public async Task WebRootComponentManager_RemoveRootComponent_Works()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var renderer = GetRemoteRenderer(serviceProvider);

        // Act
        var key = await AddWebRootComponentAsync(renderer, 0);
        await renderer.Dispatcher.InvokeAsync(() =>
        {
            var webRootComponentManager = renderer.GetOrCreateWebRootComponentManager();
            webRootComponentManager.RemoveRootComponent(0);
        });

        // Assert
        Assert.Equal(2, renderer._unacknowledgedRenderBatches.Count); // Initial render, dispose
    }

    private IServiceProvider CreateServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(new Mock<IJSRuntime>().Object);
        return serviceCollection.BuildServiceProvider();
    }

    private TestRemoteRenderer GetRemoteRenderer(IServiceProvider serviceProvider, CircuitClientProxy circuitClient = null)
    {
        var serverComponentDeserializer = new ServerComponentDeserializer(
            _ephemeralDataProtectionProvider,
            NullLogger<ServerComponentDeserializer>.Instance,
            new RootComponentTypeCache(),
            new ComponentParameterDeserializer(
                NullLogger<ComponentParameterDeserializer>.Instance,
                new ComponentParametersTypeCache()));

        return new TestRemoteRenderer(
            serviceProvider,
            NullLoggerFactory.Instance,
            new CircuitOptions
            {
                RootComponents =
                {
                    MaxJSRootComponents = MaxInteractiveServerRootComponentCount
                },
            },
            circuitClient ?? new CircuitClientProxy(),
            serverComponentDeserializer,
            NullLogger.Instance);
    }

    private static Task<ComponentMarkerKey> AddWebRootComponentAsync(RemoteRenderer renderer, int ssrComponentId, string componentKey = null)
        => renderer.Dispatcher.InvokeAsync(async () =>
        {
            var webRootComponentManager = renderer.GetOrCreateWebRootComponentManager();
            var componentMarkerKey = new ComponentMarkerKey()
            {
                LocationHash = ssrComponentId.ToString(CultureInfo.CurrentCulture),
                FormattedComponentKey = componentKey,
            };
            await webRootComponentManager.AddRootComponentAsync(
                ssrComponentId,
                typeof(TestComponent),
                componentMarkerKey,
                WebRootComponentParameters.Empty);
            return componentMarkerKey;
        });

    private static WebRootComponentParameters CreateWebRootComponentParameters(IDictionary<string, object> parameters)
    {
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
        public TestRemoteRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, CircuitOptions options, CircuitClientProxy client, IServerComponentDeserializer serverComponentDeserializer, ILogger logger)
            : base(serviceProvider, loggerFactory, options, client, serverComponentDeserializer, logger, CreateJSRuntime(options), new CircuitJSComponentInterop(options))
        {
        }

        public async Task RenderComponentAsync<TComponent>(ParameterView initialParameters)
        {
            var component = InstantiateComponent(typeof(TComponent));
            var componentId = AssignRootComponentId(component);
            await RenderRootComponentAsync(componentId, initialParameters);
        }

        protected override void AttachRootComponentToBrowser(int componentId, string domElementSelector)
        {
        }

        public new ComponentState GetComponentState(int componentId)
        {
            return base.GetComponentState(componentId);
        }

        private static RemoteJSRuntime CreateJSRuntime(CircuitOptions options)
            => new RemoteJSRuntime(Options.Create(options), Options.Create(new HubOptions<ComponentHub>()), null);
    }

    private class TestComponent : IComponent, IHandleAfterRender
    {
        private RenderHandle _renderHandle;
        private readonly RenderFragment _renderFragment = (builder) =>
        {
            builder.OpenElement(0, "my element");
            builder.AddContent(1, "some text");
            builder.CloseElement();
        };

        public TestComponent()
        {
        }

        internal TestComponent(RenderFragment renderFragment)
        {
            _renderFragment = renderFragment;
        }

        public Action OnAfterRenderComplete { get; set; }

        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

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

    private class AutoParameterTestComponent : IComponent
    {
        private RenderHandle _renderHandle;

        [Parameter] public RenderFragment Content { get; set; }

        [Parameter] public Trigger Trigger { get; set; }

        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            Content = parameters.GetValueOrDefault<RenderFragment>(nameof(Content));
            Trigger ??= parameters.GetValueOrDefault<Trigger>(nameof(Trigger));
            Trigger.Component = this;
            TriggerRender();
            return Task.CompletedTask;
        }

        public void TriggerRender()
        {
            var task = _renderHandle.Dispatcher.InvokeAsync(() => _renderHandle.Render(Content));
            Assert.True(task.IsCompletedSuccessfully);
        }
    }

    private class Trigger
    {
        public AutoParameterTestComponent Component { get; set; }
        public void TriggerRender()
        {
            Component.TriggerRender();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components;

public class ComponentsActivityStateTest
{
    [Theory]
    [InlineData(typeof(ComponentsActivityStateUpdate))]
    [InlineData(typeof(ComponentsActivityRouteState))]
    public void Serialization_DoesNotRequireConstructorParameterMetadata(Type type)
    {
        var typeInfo = JsonSerializerOptions.Web.GetTypeInfo(type);

        Assert.NotNull(typeInfo.CreateObject);
    }

    [Theory]
    [InlineData("""{"route":null}""")]
    [InlineData("""{"route":{"traceParent":"00-0123456789abcdef0123456789abcdef-0123456789abcdef-01","traceState":"vendor=value","isRemote":true,"route":"/counter"}}""")]
    public void Serialization_PreservesPayload(string json)
    {
        var state = JsonSerializer.Deserialize<ComponentsActivityStateUpdate>(json, JsonSerializerOptions.Web);

        Assert.NotNull(state);
        Assert.Equal(json, JsonSerializer.Serialize(state, JsonSerializerOptions.Web));
    }

    [Fact]
    public void NavigationUpdate_MissingTraceParentClearsStaleRoute()
    {
        var store = new ComponentsActivityLinkStore(null);
        var state = CreateInitializedState(store);
        state.Apply(CreateState(CreateContext(), "/initial"));
        var update = JsonSerializer.Deserialize<ComponentsActivityStateUpdate>(
            """{"route":{"route":"/updated"}}""", JsonSerializerOptions.Web);

        state.Apply(update);

        Assert.False(store.TryGetActivityContext(ComponentsActivityLinkStore.Route, out _, out _));
    }

    [Fact]
    public void Initialization_AppliesStateRestoredBeforeRendererInitialization()
    {
        var context = CreateContext();
        var state = new ComponentsActivityState(NullLogger.Instance);
        state.Apply(CreateState(context, "/counter"));
        var store = new ComponentsActivityLinkStore(null);
        var source = new ComponentsActivitySource();
        source.Init(store);

        state.Initialize(store, source);

        AssertRoute(store, context, "/counter");
    }

    [Fact]
    public void NavigationUpdate_ReplacesRouteAndPreservesCircuit()
    {
        var store = new ComponentsActivityLinkStore(null);
        var state = CreateInitializedState(store);
        var circuitContext = CreateContext();
        var routeContext = CreateContext();
        store.SetActivityContext(ComponentsActivityLinkStore.Circuit, circuitContext, null);
        state.Apply(CreateState(CreateContext(), "/initial"));

        state.Apply(CreateState(routeContext, "/updated"));

        AssertRoute(store, routeContext, "/updated");
        Assert.True(store.TryGetActivityContext(ComponentsActivityLinkStore.Circuit, out var actualCircuit, out _));
        Assert.Equal(circuitContext, actualCircuit);
    }

    [Fact]
    public void NavigationUpdate_ClearsStaleRoute()
    {
        var store = new ComponentsActivityLinkStore(null);
        var state = CreateInitializedState(store);
        state.Apply(CreateState(CreateContext(), "/counter"));

        state.Apply(new ComponentsActivityStateUpdate(null));

        Assert.False(store.TryGetActivityContext(ComponentsActivityLinkStore.Route, out _, out _));
    }

    [Fact]
    public void NavigationUpdate_InvalidContextClearsStaleRoute()
    {
        var store = new ComponentsActivityLinkStore(null);
        var state = CreateInitializedState(store);
        state.Apply(CreateState(CreateContext(), "/counter"));

        state.Apply(new(new("invalid", null, false, "/updated")));

        Assert.False(store.TryGetActivityContext(ComponentsActivityLinkStore.Route, out _, out _));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Capture_RoundTripsRouteContext(bool isRemote)
    {
        using var listener = CreateListener();
        var store = new ComponentsActivityLinkStore(null);
        var state = CreateInitializedState(store);
        var context = CreateContext(isRemote);
        store.SetActivityContext(
            ComponentsActivityLinkStore.Route,
            context,
            new KeyValuePair<string, object>("aspnetcore.components.route", "/counter"));
        var restoredStore = new ComponentsActivityLinkStore(null);
        var restoredState = CreateInitializedState(restoredStore);

        restoredState.Apply(state.Capture());

        AssertRoute(restoredStore, context, "/counter");
    }

    [Fact]
    public void Capture_WithoutRendererReturnsNoState()
    {
        var state = new ComponentsActivityState(NullLogger.Instance);

        Assert.Null(state.Capture());
    }

    [Fact]
    public void Capture_WithListenerAndNoRouteClearsPreviouslyRestoredRoute()
    {
        using var listener = CreateListener();
        var state = CreateInitializedState(new ComponentsActivityLinkStore(null));

        var update = state.Capture();

        Assert.NotNull(update);
        Assert.Null(update.Route);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NavigationUpdate_DoesNotReplaceInteractiveRouterContext(bool clearRoute)
    {
        using var listener = CreateListener();
        var store = new ComponentsActivityLinkStore(null);
        var source = new ComponentsActivitySource();
        source.Init(store);
        var state = new ComponentsActivityState(NullLogger.Instance);
        state.Initialize(store, source);
        var navigation = source.StartNavigateActivity("Counter", "/interactive");
        source.StopNavigateActivity(navigation, null);

        state.Apply(clearRoute ? new ComponentsActivityStateUpdate(null) : CreateState(CreateContext(), "/ssr"));

        AssertRoute(store, navigation.Activity!.Context, "/interactive");
    }

    [Fact]
    public void ScopedPersistenceManager_IsolatesRendererState()
    {
        var services = new ServiceCollection().AddLogging().AddScoped<ComponentStatePersistenceManager>();
        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>().ActivityState;
        var second = secondScope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>().ActivityState;

        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task ActivityState_ConcurrentAccessReturnsSameInstance()
    {
        const int callerCount = 16;
        const int iterationCount = 256;
        var managers = Enumerable.Range(0, iterationCount)
            .Select(_ => new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance))
            .ToArray();
        var states = new ComponentsActivityState[iterationCount, callerCount];
        using var barrier = new Barrier(callerCount);
        var callers = Enumerable.Range(0, callerCount).Select(caller => Task.Factory.StartNew(() =>
        {
            for (var iteration = 0; iteration < iterationCount; iteration++)
            {
                barrier.SignalAndWait();
                states[iteration, caller] = managers[iteration].ActivityState;
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

        await Task.WhenAll(callers);

        for (var iteration = 0; iteration < iterationCount; iteration++)
        {
            for (var caller = 0; caller < callerCount; caller++)
            {
                Assert.Same(states[iteration, 0], states[iteration, caller]);
            }
        }
    }

    [Fact]
    public void TracingRegistration_DoesNotRequirePersistenceServices()
    {
        var services = new ServiceCollection().AddLogging();
        ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing(services);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        using var scope = provider.CreateScope();
        using var renderer = new TestRenderer(scope.ServiceProvider);

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ComponentsActivitySource>());
        Assert.Null(provider.GetService<ComponentStatePersistenceManager>());
        Assert.Null(provider.GetService<RegisteredPersistentServiceRegistrationCollection>());
    }

    [Fact]
    public async Task SingletonPersistenceManager_RestoresStateAcrossHostAndRendererScopes()
    {
        var services = new ServiceCollection().AddLogging().AddSingleton<ComponentStatePersistenceManager>();
        ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing(services);
        services.AddSingleton(sp => sp.GetRequiredService<ComponentStatePersistenceManager>().State);
        services.AddSupplyValueFromPersistentComponentStateProvider();
        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        await provider.GetRequiredService<ComponentStatePersistenceManager>().RestoreStateAsync(new TestStore());

        var host = provider.GetRequiredService<ComponentStatePersistenceManager>().ActivityState;
        var renderer = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>().ActivityState;

        Assert.Same(host, renderer);
    }

    [Fact]
    public async Task Persistence_RestoresServerRegistrationsWithoutTracing()
    {
        var serverServices = new ServiceCollection().AddLogging().AddScoped<ComponentStatePersistenceManager>();
        ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing(serverServices);
        serverServices.AddScoped(sp => sp.GetRequiredService<ComponentStatePersistenceManager>().State);
        serverServices.AddSupplyValueFromPersistentComponentStateProvider();
        var providerOptions = new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        };
        using var serverProvider = serverServices.BuildServiceProvider(providerOptions);
        using var serverScope = serverProvider.CreateScope();
        using var renderer = new TestRenderer(serverScope.ServiceProvider);
        var serverManager = serverScope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>();
        var renderMode = new TestRenderMode();
        serverManager.SetPlatformRenderMode(renderMode);
        var store = new TestStore();
        await serverManager.RestoreStateAsync(store);
        using var subscription = serverManager.State.RegisterOnPersisting(() =>
        {
            serverManager.State.PersistAsJson("application-state", 42);
            return Task.CompletedTask;
        }, renderMode);
        await serverManager.PersistStateAsync(store, renderer);
        Assert.Contains(typeof(PersistentServicesRegistry).FullName!, store.State.Keys);

        var clientServices = new ServiceCollection().AddLogging().AddSingleton<ComponentStatePersistenceManager>();
        clientServices.AddSingleton(sp => sp.GetRequiredService<ComponentStatePersistenceManager>().State);
        clientServices.AddSupplyValueFromPersistentComponentStateProvider();
        using var clientProvider = clientServices.BuildServiceProvider(providerOptions);
        var clientManager = clientProvider.GetRequiredService<ComponentStatePersistenceManager>();

        await clientManager.RestoreStateAsync(store);

        Assert.Null(clientProvider.GetService<ComponentsActivitySource>());
        Assert.True(clientManager.State.TryTakeFromJson<int>("application-state", out var applicationState));
        Assert.Equal(42, applicationState);
        Assert.Single(clientManager.ServicesRegistry!.Registrations,
            registration => registration.GetResolvedTypeOrNull() == typeof(ServerComponentsActivityState));
        Assert.Single(clientManager.ServicesRegistry.Registrations,
            registration => registration.GetResolvedTypeOrNull() == typeof(WebAssemblyComponentsActivityState));
    }

    [Fact]
    public void Persistence_DisablingActivityListenerClearsPreviouslyRestoredRoute()
    {
        using var remote = RemoteExecutor.Invoke(static async () =>
        {
            using var serverListener = CreateListener();
            var serverServices = new ServiceCollection().AddLogging().AddScoped<ComponentStatePersistenceManager>();
            ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing(serverServices);
            serverServices.AddScoped(sp => sp.GetRequiredService<ComponentStatePersistenceManager>().State);
            serverServices.AddSupplyValueFromPersistentComponentStateProvider();
            using var serverProvider = serverServices.BuildServiceProvider(validateScopes: true);
            var initialStore = new TestStore();
            ActivityContext navigationContext;
            using (var scope = serverProvider.CreateScope())
            {
                using var renderer = new TestRenderer(scope.ServiceProvider);
                var manager = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>();
                await manager.RestoreStateAsync(new TestStore());
                var source = scope.ServiceProvider.GetRequiredService<ComponentsActivitySource>();
                var navigation = source.StartNavigateActivity("PageA", "/initial");
                Assert.NotNull(navigation.Activity);
                navigationContext = navigation.Activity.Context;
                source.StopNavigateActivity(navigation, null);
                await manager.PersistStateAsync(initialStore, renderer);
            }

            var clientServices = new ServiceCollection().AddLogging().AddSingleton<ComponentStatePersistenceManager>();
            ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing(clientServices);
            clientServices.AddSingleton(sp => sp.GetRequiredService<ComponentStatePersistenceManager>().State);
            clientServices.AddSupplyValueFromPersistentComponentStateProvider();
            using var clientProvider = clientServices.BuildServiceProvider(validateScopes: true);
            using var clientScope = clientProvider.CreateScope();
            using var clientRenderer = new TestRenderer(clientScope.ServiceProvider);
            var clientManager = clientProvider.GetRequiredService<ComponentStatePersistenceManager>();
            await clientManager.RestoreStateAsync(initialStore);
            var clientStore = new ComponentsActivityLinkStore(clientRenderer);
            var circuitContext = CreateContext();
            clientStore.SetActivityContext(ComponentsActivityLinkStore.Circuit, circuitContext, null);
            AssertRoute(clientStore, navigationContext, "/initial");
            var button = new ActivityTestButton();
            var componentId = clientRenderer.AssignRootComponentId(button);
            await clientRenderer.RenderRootComponentAsync(componentId);
            var eventHandlerId = clientRenderer.GetCurrentRenderTreeFrames(componentId).AsEnumerable()
                .Single(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeEventHandlerId != 0).AttributeEventHandlerId;
            Activity initialEvent = null;
            serverListener.ActivityStopped = activity =>
            {
                if (activity.OperationName == ComponentsActivitySource.OnEventName)
                {
                    initialEvent = activity;
                }
            };
            await clientRenderer.DispatchEventAsync(eventHandlerId, EventArgs.Empty);
            Assert.NotNull(initialEvent);
            Assert.Equal("/initial", initialEvent.GetTagItem("aspnetcore.components.route"));
            Assert.Contains(initialEvent.Links, link => link.Context == navigationContext);

            serverListener.Dispose();
            Assert.False(ComponentsActivitySource.HasListeners);
            var updateStore = new TestStore();
            using (var scope = serverProvider.CreateScope())
            {
                using var renderer = new TestRenderer(scope.ServiceProvider);
                var manager = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>();
                await manager.RestoreStateAsync(new TestStore());
                var source = scope.ServiceProvider.GetRequiredService<ComponentsActivitySource>();
                var navigation = source.StartNavigateActivity("PageB", "/updated");
                Assert.Null(navigation.Activity);
                await manager.PersistStateAsync(updateStore, renderer);
            }

            using var clientListener = CreateListener();
            Activity updatedEvent = null;
            clientListener.ActivityStopped = activity =>
            {
                if (activity.OperationName == ComponentsActivitySource.OnEventName)
                {
                    updatedEvent = activity;
                }
            };
            await clientManager.RestoreStateAsync(updateStore, RestoreContext.ValueUpdate);
            await clientRenderer.DispatchEventAsync(eventHandlerId, EventArgs.Empty);

            Assert.Equal(2, button.ClickCount);
            Assert.NotNull(updatedEvent);
            Assert.Null(updatedEvent.GetTagItem("aspnetcore.components.route"));
            Assert.DoesNotContain(updatedEvent.Links, link => link.Context == navigationContext);
            Assert.Equal(circuitContext, Assert.Single(updatedEvent.Links).Context);
        });
    }

    [Fact]
    public void Persistence_WithoutActivityListenerPersistsRouteClearUpdate()
    {
        using var remote = RemoteExecutor.Invoke(static async () =>
        {
            var services = new ServiceCollection().AddLogging().AddScoped<ComponentStatePersistenceManager>();
            ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing(services);
            services.AddScoped(sp => sp.GetRequiredService<ComponentStatePersistenceManager>().State);
            services.AddSupplyValueFromPersistentComponentStateProvider();
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            using var renderer = new TestRenderer(scope.ServiceProvider);
            var manager = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>();
            var store = new TestStore();
            Assert.False(ComponentsActivitySource.HasListeners);
            await manager.RestoreStateAsync(store);

            await manager.PersistStateAsync(store, renderer);

            Assert.Equal(2, store.State.Count);
            foreach (var payload in store.State.Values)
            {
                Assert.Equal("""{"route":null}""", System.Text.Encoding.UTF8.GetString(payload));
            }
        });
    }

    private sealed class ActivityTestButton : ComponentBase
    {
        public int ClickCount { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, () => ClickCount++));
            builder.CloseElement();
        }
    }

    private static ComponentsActivityState CreateInitializedState(ComponentsActivityLinkStore store)
    {
        var source = new ComponentsActivitySource();
        source.Init(store);
        var state = new ComponentsActivityState(NullLogger.Instance);
        state.Initialize(store, source);
        return state;
    }

    private static ActivityContext CreateContext(bool isRemote = false)
        => new(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded, "vendor=value", isRemote);

    private static ComponentsActivityStateUpdate CreateState(ActivityContext context, string route)
        => new(new($"00-{context.TraceId}-{context.SpanId}-{(byte)context.TraceFlags:x2}", context.TraceState, context.IsRemote, route));

    private static void AssertRoute(ComponentsActivityLinkStore store, ActivityContext expectedContext, string expectedRoute)
    {
        Assert.True(store.TryGetActivityContext(ComponentsActivityLinkStore.Route, out var context, out var tag));
        Assert.Equal(expectedContext, context);
        Assert.Equal(new KeyValuePair<string, object>("aspnetcore.components.route", expectedRoute), tag);
    }

    private static ActivityListener CreateListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ComponentsActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private sealed class TestRenderMode : IComponentRenderMode;

    private sealed class TestStore : IPersistentComponentStateStore
    {
        public IDictionary<string, byte[]> State { get; private set; } = new Dictionary<string, byte[]>();

        public Task<IDictionary<string, byte[]>> GetPersistedStateAsync() => Task.FromResult(State);

        public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
        {
            State = new Dictionary<string, byte[]>(state);
            return Task.CompletedTask;
        }
    }
}

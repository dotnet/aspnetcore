// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

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
    public async Task NavigationUpdate_DoesNotReplaceInteractiveRouterContext(bool clearRoute)
    {
        using var listener = CreateListener();
        using var provider = CreateRouterServiceProvider();
        using var scope = provider.CreateScope();
        using var renderer = new TestRenderer(scope.ServiceProvider);
        var host = new ActivityTestRouterHost();
        var componentId = renderer.AssignRootComponentId(host);
        await renderer.RenderRootComponentAsync(componentId);
        var store = new ComponentsActivityLinkStore(renderer);
        Assert.True(store.TryGetActivityContext(ComponentsActivityLinkStore.Route, out var navigationContext, out _));
        var state = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>().ActivityState;

        state.Apply(clearRoute ? new ComponentsActivityStateUpdate(null) : CreateState(CreateContext(), "/ssr"));

        AssertRoute(store, navigationContext, "/interactive");
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public async Task NavigationUpdate_AppliesAfterLastInteractiveRouterIsDisposed(int routerCount, bool clearRoute)
    {
        using var listener = CreateListener();
        using var provider = CreateRouterServiceProvider();
        using var scope = provider.CreateScope();
        using var renderer = new TestRenderer(scope.ServiceProvider);
        var host = new ActivityTestRouterHost { RouterCount = routerCount };
        var componentId = renderer.AssignRootComponentId(host);
        await renderer.RenderRootComponentAsync(componentId);
        var store = new ComponentsActivityLinkStore(renderer);
        Assert.True(store.TryGetActivityContext(ComponentsActivityLinkStore.Route, out var navigationContext, out _));
        AssertRoute(store, navigationContext, "/interactive");
        var circuitContext = CreateContext();
        store.SetActivityContext(ComponentsActivityLinkStore.Circuit, circuitContext, null);
        var state = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>().ActivityState;
        var ssrContext = CreateContext();
        var update = clearRoute ? new ComponentsActivityStateUpdate(null) : CreateState(ssrContext, "/ssr");

        for (var remaining = routerCount - 1; remaining >= 0; remaining--)
        {
            host.RouterCount = remaining;
            await renderer.RenderRootComponentAsync(componentId);
            await renderer.Dispatcher.InvokeAsync(host.Routers[remaining].Dispose);
            Assert.True(store.TryGetActivityContext(ComponentsActivityLinkStore.Route, out var currentContext, out _));

            state.Apply(update);

            if (remaining > 0)
            {
                AssertRoute(store, currentContext, "/interactive");
            }
            else if (clearRoute)
            {
                Assert.False(store.TryGetActivityContext(ComponentsActivityLinkStore.Route, out _, out _));
            }
            else
            {
                AssertRoute(store, ssrContext, "/ssr");
            }

            Assert.True(store.TryGetActivityContext(ComponentsActivityLinkStore.Circuit, out var actualCircuit, out _));
            Assert.Equal(circuitContext, actualCircuit);
        }
    }

    [Fact]
    public async Task NavigationUpdate_NewInteractiveRouterRetakesOwnership()
    {
        using var listener = CreateListener();
        using var provider = CreateRouterServiceProvider();
        using var scope = provider.CreateScope();
        using var renderer = new TestRenderer(scope.ServiceProvider);
        var host = new ActivityTestRouterHost();
        var componentId = renderer.AssignRootComponentId(host);
        await renderer.RenderRootComponentAsync(componentId);
        var store = new ComponentsActivityLinkStore(renderer);
        var state = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>().ActivityState;

        host.RouterCount = 0;
        await renderer.RenderRootComponentAsync(componentId);
        var ssrContext = CreateContext();
        state.Apply(CreateState(ssrContext, "/ssr"));
        AssertRoute(store, ssrContext, "/ssr");

        host.RouterCount = 1;
        await renderer.RenderRootComponentAsync(componentId);
        Assert.True(store.TryGetActivityContext(ComponentsActivityLinkStore.Route, out var navigationContext, out _));
        Assert.NotEqual(ssrContext, navigationContext);

        state.Apply(CreateState(CreateContext(), "/next-ssr"));

        AssertRoute(store, navigationContext, "/interactive");
    }

    [Theory]
    [InlineData(ActivitySamplingResult.None)]
    [InlineData(ActivitySamplingResult.PropagationData)]
    [InlineData(ActivitySamplingResult.AllData)]
    public void Navigation_SamplingDoesNotRetainImportedSsrCorrelation(ActivitySamplingResult samplingResult)
    {
        using var remote = RemoteExecutor.Invoke(static async samplingText =>
        {
            var navigationSampling = Enum.Parse<ActivitySamplingResult>(samplingText);
            var navigationSampleCount = 0;
            var navigations = new List<Activity>();
            Activity stoppedEvent = null;
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == ComponentsActivitySource.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                {
                    if (options.Name == ComponentsActivitySource.OnRouteName)
                    {
                        navigationSampleCount++;
                        return navigationSampling;
                    }

                    return ActivitySamplingResult.AllData;
                },
                ActivityStarted = activity =>
                {
                    if (activity.OperationName == ComponentsActivitySource.OnRouteName)
                    {
                        navigations.Add(activity);
                    }
                },
                ActivityStopped = activity =>
                {
                    if (activity.OperationName == ComponentsActivitySource.OnEventName)
                    {
                        stoppedEvent = activity;
                    }
                },
            };
            ActivitySource.AddActivityListener(listener);
            using var provider = CreateRouterServiceProvider(useRoutingStateProvider: false);
            using var scope = provider.CreateScope();
            var navigationManager = scope.ServiceProvider.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("/activity-test-initial");
            using var renderer = new TestRenderer(scope.ServiceProvider);
            var state = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>().ActivityState;
            var importedContext = CreateContext();
            state.Apply(CreateState(importedContext, "/activity-test-initial"));
            var store = new ComponentsActivityLinkStore(renderer);
            AssertRoute(store, importedContext, "/activity-test-initial");
            var circuitContext = CreateContext();
            store.SetActivityContext(ComponentsActivityLinkStore.Circuit, circuitContext, null);
            var button = new ActivityTestButton();
            var buttonId = renderer.AssignRootComponentId(button);
            await renderer.RenderRootComponentAsync(buttonId);
            var hostId = renderer.AssignRootComponentId(new ActivityTestRouterHost());
            await renderer.RenderRootComponentAsync(hostId);
            var initialSampleCount = navigationSampleCount;

            await renderer.Dispatcher.InvokeAsync(() => navigationManager.NavigateTo("/activity-test-updated"));
            Assert.True(navigationSampleCount > initialSampleCount);
            state.Apply(CreateState(CreateContext(), "/ignored-ssr"));
            var eventHandlerId = renderer.GetCurrentRenderTreeFrames(buttonId).AsEnumerable()
                .Single(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeEventHandlerId != 0).AttributeEventHandlerId;
            await renderer.DispatchEventAsync(eventHandlerId, EventArgs.Empty);

            Assert.Equal(1, button.ClickCount);
            Assert.NotNull(stoppedEvent);
            Assert.DoesNotContain(stoppedEvent.Links, link => link.Context == importedContext);
            if (navigationSampling == ActivitySamplingResult.AllData)
            {
                Assert.Equal("/activity-test-updated", stoppedEvent.GetTagItem("aspnetcore.components.route"));
                Assert.Contains(stoppedEvent.Links, link => link.Context == navigations[^1].Context);
                Assert.Contains(stoppedEvent.Links, link => link.Context == circuitContext);
                Assert.Equal(2, stoppedEvent.Links.Count());
            }
            else
            {
                Assert.Null(stoppedEvent.GetTagItem("aspnetcore.components.route"));
                Assert.Equal(circuitContext, Assert.Single(stoppedEvent.Links).Context);
                if (navigationSampling == ActivitySamplingResult.None)
                {
                    Assert.Empty(navigations);
                }
                else
                {
                    Assert.NotEmpty(navigations);
                    Assert.All(navigations, activity => Assert.False(activity.IsAllDataRequested));
                }
            }
        }, samplingResult.ToString());
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PersistenceRegistration_RequiresOnlyPersistentComponentState(bool registerTracing)
    {
        using var listener = CreateListener();
        var manager = new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance);
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton(manager.State);
        services.AddSupplyValueFromPersistentComponentStateProvider();
        if (registerTracing)
        {
            ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing(services);
        }

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        using var scope = provider.CreateScope();
        Assert.Null(scope.ServiceProvider.GetService<ComponentStatePersistenceManager>());
        Assert.Same(manager.State, scope.ServiceProvider.GetRequiredService<PersistentComponentState>());
        Assert.IsType<PersistentStateValueProvider>(Assert.Single(scope.ServiceProvider.GetServices<ICascadingValueSupplier>()));
        var store = new ComponentsActivityLinkStore(null);
        var source = new ComponentsActivitySource();
        source.Init(store);
        manager.ActivityState.Initialize(store, source);
        var server = scope.ServiceProvider.GetRequiredService<ServerComponentsActivityState>();
        var webAssembly = scope.ServiceProvider.GetRequiredService<WebAssemblyComponentsActivityState>();
        var serverContext = CreateContext();
        var serverUpdate = CreateState(serverContext, "/server");

        server.ActivityState = serverUpdate;

        AssertRoute(store, serverContext, "/server");
        Assert.Equal(serverUpdate, webAssembly.ActivityState);
        var webAssemblyContext = CreateContext();
        var webAssemblyUpdate = CreateState(webAssemblyContext, "/webassembly");

        webAssembly.ActivityState = webAssemblyUpdate;

        AssertRoute(store, webAssemblyContext, "/webassembly");
        Assert.Equal(webAssemblyUpdate, server.ActivityState);
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

    private static ServiceProvider CreateRouterServiceProvider(bool useRoutingStateProvider = true)
    {
        var services = new ServiceCollection().AddLogging().AddScoped<ComponentStatePersistenceManager>();
        ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing(services);
        services.AddSingleton<NavigationManager, ActivityTestNavigationManager>();
        services.AddSingleton(Mock.Of<INavigationInterception>(
            interception => interception.EnableNavigationInterceptionAsync() == Task.CompletedTask));
        services.AddSingleton(Mock.Of<IScrollToLocationHash>(
            scroll => scroll.RefreshScrollPositionForHash(It.IsAny<string>()) == Task.CompletedTask));
        if (useRoutingStateProvider)
        {
            var routeData = new RouteData(typeof(ActivityTestRoutePage), new Dictionary<string, object>())
            {
                Template = "/interactive",
            };
            services.AddSingleton(Mock.Of<IRoutingStateProvider>(provider => provider.RouteData == routeData));
        }

        return services.BuildServiceProvider(validateScopes: true);
    }

    private sealed class ActivityTestNavigationManager : NavigationManager
    {
        public ActivityTestNavigationManager()
        {
            Initialize("https://example.com/", "https://example.com/interactive");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Uri = ToAbsoluteUri(uri).AbsoluteUri;
            NotifyLocationChanged(isInterceptedLink: false);
        }
    }

    [Route("/activity-test-initial")]
    [Route("/activity-test-updated")]
    [Route("/interactive")]
    public sealed class ActivityTestRoutePage : ComponentBase
    {
    }

    private sealed class ActivityTestRouterHost : ComponentBase
    {
        public int RouterCount { get; set; } = 1;
        public List<Router> Routers { get; } = [];

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            for (var i = 0; i < RouterCount; i++)
            {
                builder.OpenComponent<Router>(0);
                builder.SetKey(i);
                builder.AddComponentParameter(1, nameof(Router.AppAssembly), typeof(ActivityTestButton).Assembly);
                builder.AddComponentParameter(2, nameof(Router.Found), (RenderFragment<RouteData>)(_ => content => { }));
                builder.AddComponentReferenceCapture(3, component => Routers.Add((Router)component));
                builder.CloseComponent();
            }
        }
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

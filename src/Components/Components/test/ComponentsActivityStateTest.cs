// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components;

public class ComponentsActivityStateTest
{
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

    [Fact]
    public void NavigationUpdate_DoesNotReplaceInteractiveRouterContext()
    {
        using var listener = CreateListener();
        var store = new ComponentsActivityLinkStore(null);
        var source = new ComponentsActivitySource();
        source.Init(store);
        var state = new ComponentsActivityState(NullLogger.Instance);
        state.Initialize(store, source);
        var navigation = source.StartNavigateActivity("Counter", "/interactive");
        source.StopNavigateActivity(navigation, null);

        state.Apply(CreateState(CreateContext(), "/ssr"));

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
    public async Task SingletonPersistenceManager_RestoresStateAcrossHostAndRendererScopes()
    {
        var services = new ServiceCollection().AddLogging().AddSingleton<ComponentStatePersistenceManager>();
        ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing(services);
        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        await provider.GetRequiredService<ComponentStatePersistenceManager>().RestoreStateAsync(new TestStore());

        var host = provider.GetRequiredService<ComponentStatePersistenceManager>().ActivityState;
        var renderer = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>().ActivityState;

        Assert.Same(host, renderer);
    }

    [Fact]
    public void Persistence_WithoutActivityListenerDoesNotAddActivityPayload()
    {
        using var remote = RemoteExecutor.Invoke(static async () =>
        {
            var services = new ServiceCollection().AddLogging().AddScoped<ComponentStatePersistenceManager>();
            ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing(services);
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            using var renderer = new TestRenderer(scope.ServiceProvider);
            var manager = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>();
            var store = new TestStore();
            Assert.False(ComponentsActivitySource.HasListeners);
            await manager.RestoreStateAsync(store);

            await manager.PersistStateAsync(store, renderer);

            Assert.Empty(store.State);
        });
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

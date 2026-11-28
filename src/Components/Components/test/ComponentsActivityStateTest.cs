// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Components;

public class ComponentsActivityStateTest
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Persistence_RestoresRouteBeforeFirstEvent(bool createRendererBeforeRestore)
    {
        using var listener = CreateListener();
        using var provider = CreateServiceProvider();
        var (state, navigation) = await PersistRouteAsync(provider, "/counter");
        using var scope = provider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>();
        manager.SetPlatformRenderMode(new TestRenderMode());
        using var renderer = createRendererBeforeRestore ? new TestRenderer(scope.ServiceProvider) : null;
        await manager.RestoreStateAsync(state);
        using var laterRenderer = createRendererBeforeRestore ? null : new TestRenderer(scope.ServiceProvider);
        var interactiveRenderer = renderer ?? laterRenderer;
        var buttonId = interactiveRenderer.AssignRootComponentId(new ActivityTestButton());
        await interactiveRenderer.RenderRootComponentAsync(buttonId);

        var activity = await DispatchEventAsync(interactiveRenderer, buttonId, listener);

        Assert.Equal("/counter", activity.GetTagItem("aspnetcore.components.route"));
        Assert.Equal(navigation, Assert.Single(activity.Links).Context);
    }

    [Fact]
    public async Task Persistence_UpdatesRetainedRootAndClearsStaleRouteWithoutRemovingCircuit()
    {
        using var listener = CreateListener();
        using var provider = CreateServiceProvider();
        var (initial, _) = await PersistRouteAsync(provider, "/initial");
        using var scope = provider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>();
        manager.SetPlatformRenderMode(new TestRenderMode());
        await manager.RestoreStateAsync(initial);
        using var renderer = new TestRenderer(scope.ServiceProvider);
        var buttonId = renderer.AssignRootComponentId(new ActivityTestButton());
        await renderer.RenderRootComponentAsync(buttonId);
        var links = new ComponentsActivityLinkStore(renderer);
        var circuit = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
        links.SetActivityContext(ComponentsActivityLinkStore.Circuit, circuit, null);
        var (update, navigation) = await PersistRouteAsync(provider, "/updated");

        await manager.RestoreStateAsync(update, RestoreContext.ValueUpdate);
        update.State.Clear();
        var activity = await DispatchEventAsync(renderer, buttonId, listener);

        Assert.Equal("/updated", activity.GetTagItem("aspnetcore.components.route"));
        Assert.Contains(activity.Links, link => link.Context == navigation);
        Assert.Contains(activity.Links, link => link.Context == circuit);

        var (clear, _) = await PersistRouteAsync(provider, route: null);
        await manager.RestoreStateAsync(clear, RestoreContext.ValueUpdate);
        activity = await DispatchEventAsync(renderer, buttonId, listener);

        Assert.Null(activity.GetTagItem("aspnetcore.components.route"));
        Assert.Equal(circuit, Assert.Single(activity.Links).Context);
    }

    [Fact]
    public async Task Persistence_InteractiveRouterOwnsRouteUntilDisposed()
    {
        using var listener = CreateListener();
        using var provider = CreateServiceProvider();
        var (initial, _) = await PersistRouteAsync(provider, "/initial");
        using var scope = provider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>();
        manager.SetPlatformRenderMode(new TestRenderMode());
        await manager.RestoreStateAsync(initial);
        using var renderer = new TestRenderer(scope.ServiceProvider);
        var host = new ActivityTestRouterHost();
        var hostId = renderer.AssignRootComponentId(host);
        await renderer.RenderRootComponentAsync(hostId);
        var links = new ComponentsActivityLinkStore(renderer);
        Assert.True(links.TryGetActivityContext(ComponentsActivityLinkStore.Route, out var interactiveRoute, out _));
        var (update, ssrRoute) = await PersistRouteAsync(provider, "/updated");

        await manager.RestoreStateAsync(update, RestoreContext.ValueUpdate);

        Assert.True(links.TryGetActivityContext(ComponentsActivityLinkStore.Route, out var current, out _));
        Assert.Equal(interactiveRoute, current);
        host.ShowRouter = false;
        await renderer.RenderRootComponentAsync(hostId);
        Assert.True(links.TryGetActivityContext(ComponentsActivityLinkStore.Route, out current, out _));
        Assert.Equal(ssrRoute, current);

        host.ShowRouter = true;
        await renderer.RenderRootComponentAsync(hostId);
        Assert.True(links.TryGetActivityContext(ComponentsActivityLinkStore.Route, out current, out _));
        Assert.NotEqual(ssrRoute, current);
    }

    [Fact]
    public void Persistence_DisablingListenerClearsStaleRoute()
    {
        using var remote = RemoteExecutor.Invoke(static async () =>
        {
            using var listener = CreateListener();
            using var provider = CreateServiceProvider();
            var (initial, _) = await PersistRouteAsync(provider, "/initial");
            using var scope = provider.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>();
            manager.SetPlatformRenderMode(new TestRenderMode());
            await manager.RestoreStateAsync(initial);
            using var renderer = new TestRenderer(scope.ServiceProvider);
            var buttonId = renderer.AssignRootComponentId(new ActivityTestButton());
            await renderer.RenderRootComponentAsync(buttonId);
            Assert.Equal("/initial", (await DispatchEventAsync(renderer, buttonId, listener)).GetTagItem("aspnetcore.components.route"));
            listener.Dispose();

            var (update, _) = await PersistRouteAsync(provider, "/unsampled");
            await manager.RestoreStateAsync(update, RestoreContext.ValueUpdate);
            using var newListener = CreateListener();
            var activity = await DispatchEventAsync(renderer, buttonId, newListener);

            Assert.Null(activity.GetTagItem("aspnetcore.components.route"));
            Assert.Empty(activity.Links);
        });
    }

    [Theory]
    [InlineData(ActivitySamplingResult.None)]
    [InlineData(ActivitySamplingResult.PropagationData)]
    public void Navigation_UnsampledRouteClearsPreviousCorrelation(ActivitySamplingResult sampling)
    {
        using var remote = RemoteExecutor.Invoke(static samplingText =>
        {
            using var listener = CreateListener();
            var source = new ComponentsActivitySource();
            var links = new ComponentsActivityLinkStore(null);
            source.Init(links);
            var first = source.StartNavigateActivity("Page", "/initial");
            source.StopNavigateActivity(first, null);
            Assert.True(links.TryGetActivityContext(ComponentsActivityLinkStore.Route, out _, out _));
            var result = Enum.Parse<ActivitySamplingResult>(samplingText);
            listener.Sample = (ref ActivityCreationOptions<ActivityContext> options) => result;

            var next = source.StartNavigateActivity("Page", "/unsampled");
            source.StopNavigateActivity(next, null);

            Assert.False(links.TryGetActivityContext(ComponentsActivityLinkStore.Route, out _, out _));
        }, sampling.ToString());
    }

    private static async Task<(TestStore State, ActivityContext Navigation)> PersistRouteAsync(ServiceProvider provider, string route)
    {
        using var scope = provider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>();
        manager.SetPlatformRenderMode(new TestRenderMode());
        await manager.RestoreStateAsync(new TestStore());
        using var renderer = new TestRenderer(scope.ServiceProvider);
        renderer.AssignRootComponentId(new ActivityTestButton());
        var source = scope.ServiceProvider.GetRequiredService<ComponentsActivitySource>();
        var navigation = route is not null ? source.StartNavigateActivity("Page", route) : default;
        source.StopNavigateActivity(navigation, null);
        var store = new TestStore();

        await manager.PersistStateAsync(store, renderer);

        return (store, navigation.Activity?.Context ?? default);
    }

    private static async Task<Activity> DispatchEventAsync(TestRenderer renderer, int componentId, ActivityListener listener)
    {
        Activity stoppedEvent = null;
        listener.ActivityStopped = activity =>
        {
            if (activity.OperationName == ComponentsActivitySource.OnEventName)
            {
                stoppedEvent = activity;
            }
        };
        var eventHandlerId = renderer.GetCurrentRenderTreeFrames(componentId).AsEnumerable()
            .Single(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeEventHandlerId != 0).AttributeEventHandlerId;

        await renderer.DispatchEventAsync(eventHandlerId, EventArgs.Empty);

        Assert.NotNull(stoppedEvent);
        return stoppedEvent;
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection().AddLogging().AddScoped<ComponentStatePersistenceManager>();
        ComponentsMetricsServiceCollectionExtensions.AddComponentsTracing(services);
        services.AddSingleton<NavigationManager, ActivityTestNavigationManager>();
        services.AddSingleton(Mock.Of<INavigationInterception>(
            interception => interception.EnableNavigationInterceptionAsync() == Task.CompletedTask));
        services.AddSingleton(Mock.Of<IScrollToLocationHash>(
            scroll => scroll.RefreshScrollPositionForHash(It.IsAny<string>()) == Task.CompletedTask));
        var routeData = new RouteData(typeof(ActivityTestRoutePage), new Dictionary<string, object>()) { Template = "/interactive" };
        services.AddSingleton(Mock.Of<IRoutingStateProvider>(provider => provider.RouteData == routeData));

        return services.BuildServiceProvider(validateScopes: true);
    }

    private sealed class ActivityTestNavigationManager : NavigationManager
    {
        public ActivityTestNavigationManager() => Initialize("https://example.com/", "https://example.com/interactive");
    }

    [Route("/interactive")]
    public sealed class ActivityTestRoutePage : ComponentBase;

    private sealed class ActivityTestRouterHost : ComponentBase
    {
        public bool ShowRouter { get; set; } = true;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (ShowRouter)
            {
                builder.OpenComponent<Router>(0);
                builder.AddComponentParameter(1, nameof(Router.AppAssembly), typeof(ActivityTestRoutePage).Assembly);
                builder.AddComponentParameter(2, nameof(Router.Found), (RenderFragment<RouteData>)(_ => content => { }));
                builder.CloseComponent();
            }
        }
    }

    private sealed class ActivityTestButton : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, () => { }));
            builder.CloseElement();
        }
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

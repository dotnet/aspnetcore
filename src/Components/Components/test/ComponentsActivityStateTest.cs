// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components;

public class ComponentsActivityStateTest
{
    [Fact]
    public void CircuitInitialization_AppliesStateRestoredBeforeRendererInitialization()
    {
        var routeContext = CreateActivityContext();
        var activityState = new ComponentsActivityState
        {
            ActivityState = CreateState(routeContext, "/counter")
        };
        var linkStore = new ComponentsActivityLinkStore(null);

        activityState.Initialize(linkStore);

        AssertRoute(linkStore, routeContext, "/counter");
    }

    [Fact]
    public void NavigationUpdate_ReplacesRouteWhenInteractiveTreeHasNoRouter()
    {
        var initialContext = CreateActivityContext();
        var updatedContext = CreateActivityContext();
        var linkStore = new ComponentsActivityLinkStore(null);
        var activityState = new ComponentsActivityState();
        activityState.Initialize(linkStore);
        activityState.ActivityState = CreateState(initialContext, "/initial");

        activityState.ActivityState = CreateState(updatedContext, "/updated");

        AssertRoute(linkStore, updatedContext, "/updated");
    }

    [Fact]
    public void NavigationUpdate_ClearsRouteWhenStateHasNoRoute()
    {
        var linkStore = new ComponentsActivityLinkStore(null);
        var activityState = new ComponentsActivityState();
        activityState.Initialize(linkStore);
        activityState.ActivityState = CreateState(CreateActivityContext(), "/counter");

        activityState.ActivityState = new ComponentsActivityPersistentStateUpdate(null);

        Assert.False(linkStore.TryGetActivityContext(ComponentsActivityLinkStore.Route, out _, out _));
    }

    private static ComponentsActivityPersistentStateUpdate CreateState(ActivityContext context, string route)
        => new(new ComponentsActivityPersistentState(
            $"00-{context.TraceId}-{context.SpanId}-{(byte)context.TraceFlags:x2}",
            context.TraceState,
            context.IsRemote,
            route));

    private static ActivityContext CreateActivityContext()
        => new(
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.Recorded,
            traceState: "vendor=value",
            isRemote: false);

    private static void AssertRoute(
        ComponentsActivityLinkStore linkStore,
        ActivityContext expectedContext,
        string expectedRoute)
    {
        Assert.True(linkStore.TryGetActivityContext(
            ComponentsActivityLinkStore.Route,
            out var context,
            out var tag));
        Assert.Equal(expectedContext, context);
        Assert.Equal(new KeyValuePair<string, object>("aspnetcore.components.route", expectedRoute), tag);
    }
}

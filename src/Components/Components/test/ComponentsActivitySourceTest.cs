// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components;

public class ComponentsActivitySourceTest
{
    private readonly ActivityListener _listener;
    private readonly List<Activity> _activities;

    public ComponentsActivitySourceTest()
    {
        _activities = new List<Activity>();
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ComponentsActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => _activities.Add(activity),
            ActivityStopped = activity => { }
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [Fact]
    public void Constructor_CreatesActivitySourceCorrectly()
    {
        // Arrange & Act
        var componentsActivitySource = new ComponentsActivitySource();
        var linkstore = new ComponentsActivityLinkStore(null);
        componentsActivitySource.Init(linkstore);

        // Assert
        Assert.NotNull(componentsActivitySource);
    }

    [Fact]
    public void StartRouteActivity_CreatesAndStartsActivity()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();
        var linkstore = new ComponentsActivityLinkStore(null);
        componentsActivitySource.Init(linkstore);
        var componentType = "TestComponent";
        var route = "/test-route";

        // First set up a circuit context
        linkstore.SetActivityContext(ComponentsActivityLinkStore.Circuit, new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded), new KeyValuePair<string, object>("aspnetcore.components.circuit.id", "test-circuit-id"));

        // Act
        var activityHandle = componentsActivitySource.StartNavigateActivity(componentType, route);
        var activity = activityHandle.Activity;

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(ComponentsActivitySource.OnRouteName, activity.OperationName);
        Assert.Equal($"Route {route} -> {componentType}", activity.DisplayName);
        Assert.Equal(ActivityKind.Internal, activity.Kind);
        Assert.True(activity.IsAllDataRequested);
        Assert.Equal(componentType, activity.GetTagItem("aspnetcore.components.type"));
        Assert.Equal(route, activity.GetTagItem("aspnetcore.components.route"));
        Assert.False(activity.IsStopped);

        componentsActivitySource.StopNavigateActivity(activityHandle, null);
        Assert.True(activity.IsStopped);
        Assert.Equal("test-circuit-id", activity.GetTagItem("aspnetcore.components.circuit.id"));
        Assert.Single(activity.Links);

    }

    [Fact]
    public void StartEventActivity_WithSharedLinkStore_LinksToRoute()
    {
        var componentsActivitySource = new ComponentsActivitySource();
        var linkstore = new ComponentsActivityLinkStore(null);
        componentsActivitySource.Init(linkstore);
        var componentType = "TestComponent";
        var methodName = "OnClick";
        var attributeName = "onclick";

        linkstore.SetActivityContext(ComponentsActivityLinkStore.Circuit, default, new KeyValuePair<string, object>("aspnetcore.components.circuit.id", "test-circuit-id"));
        var routeActivityHandle = componentsActivitySource.StartNavigateActivity("ParentComponent", "/parent");
        componentsActivitySource.StopNavigateActivity(routeActivityHandle, null);

        var activityHandle = ComponentsActivitySource.StartHandleEventActivity(componentType, methodName, attributeName);
        var activity = activityHandle.Activity;

        Assert.NotNull(activity);
        Assert.Equal(ComponentsActivitySource.OnEventName, activity.OperationName);
        Assert.Equal($"Event {attributeName} -> {componentType}.{methodName}", activity.DisplayName);
        Assert.Equal(ActivityKind.Internal, activity.Kind);
        Assert.True(activity.IsAllDataRequested);
        Assert.Equal(componentType, activity.GetTagItem("aspnetcore.components.type"));
        Assert.Equal(methodName, activity.GetTagItem("code.function.name"));
        Assert.Equal(attributeName, activity.GetTagItem("aspnetcore.components.attribute.name"));
        Assert.False(activity.IsStopped);

        componentsActivitySource.StopHandleEventActivity(activityHandle, null);
        Assert.True(activity.IsStopped);
        Assert.Equal("test-circuit-id", activity.GetTagItem("aspnetcore.components.circuit.id"));
        Assert.Collection(activity.Links, link => Assert.Equal(routeActivityHandle.Activity.Context, link.Context));
    }

    [Fact]
    public void StartEventActivity_WithSeparateLinkStores_DoesNotLinkToRoute()
    {
        var endpointActivitySource = new ComponentsActivitySource();
        var endpointLinkStore = new ComponentsActivityLinkStore(null);
        endpointActivitySource.Init(endpointLinkStore);

        var routeActivityHandle = endpointActivitySource.StartNavigateActivity("ParentComponent", "/parent");
        endpointActivitySource.StopNavigateActivity(routeActivityHandle, null);
        Assert.NotNull(routeActivityHandle.Activity);

        var circuitActivitySource = new ComponentsActivitySource();
        var circuitLinkStore = new ComponentsActivityLinkStore(null);
        circuitActivitySource.Init(circuitLinkStore);

        var eventActivityHandle = ComponentsActivitySource.StartHandleEventActivity("TestComponent", "OnClick", "onclick");
        circuitActivitySource.StopHandleEventActivity(eventActivityHandle, null);
        Assert.NotNull(eventActivityHandle.Activity);

        Assert.Empty(eventActivityHandle.Activity.Links);
        Assert.DoesNotContain(eventActivityHandle.Activity.Links, link => link.Context == routeActivityHandle.Activity.Context);
    }

    [Fact]
    public void FailEventActivity_SetsErrorStatusAndStopsActivity()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();
        var linkstore = new ComponentsActivityLinkStore(null);
        componentsActivitySource.Init(linkstore);
        var activityHandle = ComponentsActivitySource.StartHandleEventActivity("TestComponent", "OnClick", "onclick");
        var activity = activityHandle.Activity;
        var exception = new InvalidOperationException("Test exception");

        // Act
        componentsActivitySource.StopHandleEventActivity(activityHandle, exception);

        // Assert
        Assert.True(activity!.IsStopped);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal(exception.GetType().FullName, activity.GetTagItem("error.type"));
    }

    [Fact]
    public async Task CaptureEventStopAsync_StopsActivityOnSuccessfulTask()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();
        var linkstore = new ComponentsActivityLinkStore(null);
        componentsActivitySource.Init(linkstore);
        var activityHandle = ComponentsActivitySource.StartHandleEventActivity("TestComponent", "OnClick", "onclick");
        var activity = activityHandle.Activity;
        var task = Task.CompletedTask;

        // Act
        await componentsActivitySource.CaptureHandleEventStopAsync(task, activityHandle);

        // Assert
        Assert.True(activity!.IsStopped);
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);
    }

    [Fact]
    public async Task CaptureEventStopAsync_FailsActivityOnException()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();
        var linkstore = new ComponentsActivityLinkStore(null);
        componentsActivitySource.Init(linkstore);
        var activityHandle = ComponentsActivitySource.StartHandleEventActivity("TestComponent", "OnClick", "onclick");
        var activity = activityHandle.Activity;
        var exception = new InvalidOperationException("Test exception");
        var task = Task.FromException(exception);

        // Act
        await componentsActivitySource.CaptureHandleEventStopAsync(task, activityHandle);

        // Assert
        Assert.True(activity!.IsStopped);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal(exception.GetType().FullName, activity.GetTagItem("error.type"));
    }

    [Fact]
    public void StartRouteActivity_HandlesNullValues()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();
        var linkstore = new ComponentsActivityLinkStore(null);
        componentsActivitySource.Init(linkstore);

        // Act
        var activityHandle = componentsActivitySource.StartNavigateActivity(null, null);
        var activity = activityHandle.Activity;

        // Assert
        Assert.NotNull(activity);
        Assert.Equal("Route [unknown path] -> [unknown component]", activity.DisplayName);
    }

    [Fact]
    public void StartEventActivity_HandlesNullValues()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();
        var linkstore = new ComponentsActivityLinkStore(null);
        componentsActivitySource.Init(linkstore);

        // Act
        var activityHandle = ComponentsActivitySource.StartHandleEventActivity(null, null, null);
        var activity = activityHandle.Activity;

        // Assert
        Assert.NotNull(activity);
        Assert.Equal("Event [unknown attribute] -> [unknown component].[unknown method]", activity.DisplayName);
    }
}

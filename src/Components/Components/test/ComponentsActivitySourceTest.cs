// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

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

        // Assert
        Assert.NotNull(componentsActivitySource);
    }

    [Fact]
    public void StartRouteActivity_CreatesAndStartsActivity()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();
        var componentType = "TestComponent";
        var route = "/test-route";

        // First set up a circuit context
        componentsActivitySource._circuitId = "test-circuit-id";

        // Act
        var wrapper = componentsActivitySource.StartRouteActivity(componentType, route);
        var activity = wrapper.Activity;

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(ComponentsActivitySource.OnRouteName, activity.OperationName);
        Assert.Equal($"Route {route} -> {componentType}", activity.DisplayName);
        Assert.Equal(ActivityKind.Internal, activity.Kind);
        Assert.True(activity.IsAllDataRequested);
        Assert.Equal(componentType, activity.GetTagItem("aspnetcore.components.type"));
        Assert.Equal(route, activity.GetTagItem("aspnetcore.components.route"));
        Assert.Equal("test-circuit-id", activity.GetTagItem("aspnetcore.components.circuit.id"));
        Assert.False(activity.IsStopped);
    }

    [Fact]
    public void StartEventActivity_CreatesAndStartsActivity()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();
        var componentType = "TestComponent";
        var methodName = "OnClick";
        var attributeName = "onclick";

        // First set up a circuit and route context
        componentsActivitySource._circuitId = "test-circuit-id";
        componentsActivitySource.StartRouteActivity("ParentComponent", "/parent");

        // Act
        var wrapper = componentsActivitySource.StartEventActivity(componentType, methodName, attributeName);
        var activity = wrapper.Activity;

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(ComponentsActivitySource.OnEventName, activity.OperationName);
        Assert.Equal($"Event {attributeName} -> {componentType}.{methodName}", activity.DisplayName);
        Assert.Equal(ActivityKind.Internal, activity.Kind);
        Assert.True(activity.IsAllDataRequested);
        Assert.Equal(componentType, activity.GetTagItem("aspnetcore.components.type"));
        Assert.Equal(methodName, activity.GetTagItem("aspnetcore.components.method"));
        Assert.Equal(attributeName, activity.GetTagItem("aspnetcore.components.attribute.name"));
        Assert.Equal("test-circuit-id", activity.GetTagItem("aspnetcore.components.circuit.id"));
        Assert.False(activity.IsStopped);
    }

    [Fact]
    public void FailEventActivity_SetsErrorStatusAndStopsActivity()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();
        var wrapper = componentsActivitySource.StartEventActivity("TestComponent", "OnClick", "onclick");
        var activity = wrapper.Activity;
        var exception = new InvalidOperationException("Test exception");

        // Act
        componentsActivitySource.StopComponentActivity(wrapper, exception);

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
        var wrapper = componentsActivitySource.StartEventActivity("TestComponent", "OnClick", "onclick");
        var activity = wrapper.Activity;
        var task = Task.CompletedTask;

        // Act
        await componentsActivitySource.CaptureEventStopAsync(task, wrapper);

        // Assert
        Assert.True(activity!.IsStopped);
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);
    }

    [Fact]
    public async Task CaptureEventStopAsync_FailsActivityOnException()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();
        var wrapper = componentsActivitySource.StartEventActivity("TestComponent", "OnClick", "onclick");
        var activity = wrapper.Activity;
        var exception = new InvalidOperationException("Test exception");
        var task = Task.FromException(exception);

        // Act
        await componentsActivitySource.CaptureEventStopAsync(task, wrapper);

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

        // Act
        var wrapper = componentsActivitySource.StartRouteActivity(null, null);
        var activity = wrapper.Activity;

        // Assert
        Assert.NotNull(activity);
        Assert.Equal("Route [unknown path] -> [unknown component]", activity.DisplayName);
    }

    [Fact]
    public void StartEventActivity_HandlesNullValues()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();

        // Act
        var wrapper = componentsActivitySource.StartEventActivity(null, null, null);
        var activity = wrapper.Activity;

        // Assert
        Assert.NotNull(activity);
        Assert.Equal("Event [unknown attribute] -> [unknown component].[unknown method]", activity.DisplayName);
    }
}

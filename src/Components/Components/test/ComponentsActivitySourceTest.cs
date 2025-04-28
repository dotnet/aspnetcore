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
    public void CaptureHttpContext_ReturnsDefault_WhenNoCurrentActivity()
    {
        // Arrange
        Activity.Current = null;

        // Act
        var result = ComponentsActivitySource.CaptureHttpContext();

        // Assert
        Assert.Equal(default, result);
    }

    [Fact]
    public void CaptureHttpContext_ReturnsDefault_WhenActivityHasWrongName()
    {
        // Arrange
        using var activity = new ActivitySource("Test").StartActivity("WrongName");
        Activity.Current = activity;

        // Act
        var result = ComponentsActivitySource.CaptureHttpContext();

        // Assert
        Assert.Equal(default, result);
    }

    [Fact]
    public void StartCircuitActivity_CreatesAndStartsActivity()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();
        var circuitId = "test-circuit-id";
        var httpContext = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);

        // Act
        var activity = componentsActivitySource.StartCircuitActivity(circuitId, httpContext);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(ComponentsActivitySource.OnCircuitName, activity.OperationName);
        Assert.Equal($"CIRCUIT {circuitId}", activity.DisplayName);
        Assert.Equal(ActivityKind.Server, activity.Kind);
        Assert.True(activity.IsAllDataRequested);
        Assert.Equal(circuitId, activity.GetTagItem("circuit.id"));
        Assert.Contains(activity.Links, link => link.Context == httpContext);
        Assert.False(activity.IsStopped);
    }

    [Fact]
    public void FailCircuitActivity_SetsErrorStatusAndStopsActivity()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();
        var circuitId = "test-circuit-id";
        var httpContext = default(ActivityContext);
        var activity = componentsActivitySource.StartCircuitActivity(circuitId, httpContext);
        var exception = new InvalidOperationException("Test exception");

        // Act
        componentsActivitySource.FailCircuitActivity(activity, exception);

        // Assert
        Assert.True(activity!.IsStopped);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal(exception.GetType().FullName, activity.GetTagItem("error.type"));
    }

    [Fact]
    public void StartRouteActivity_CreatesAndStartsActivity()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();
        var componentType = "TestComponent";
        var route = "/test-route";

        // First set up a circuit context
        componentsActivitySource.StartCircuitActivity("test-circuit-id", default);

        // Act
        var activity = componentsActivitySource.StartRouteActivity(componentType, route);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(ComponentsActivitySource.OnRouteName, activity.OperationName);
        Assert.Equal($"ROUTE {route} -> {componentType}", activity.DisplayName);
        Assert.Equal(ActivityKind.Server, activity.Kind);
        Assert.True(activity.IsAllDataRequested);
        Assert.Equal(componentType, activity.GetTagItem("component.type"));
        Assert.Equal(route, activity.GetTagItem("route"));
        Assert.Equal("test-circuit-id", activity.GetTagItem("circuit.id"));
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
        componentsActivitySource.StartCircuitActivity("test-circuit-id", default);
        componentsActivitySource.StartRouteActivity("ParentComponent", "/parent");

        // Act
        var activity = componentsActivitySource.StartEventActivity(componentType, methodName, attributeName);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(ComponentsActivitySource.OnEventName, activity.OperationName);
        Assert.Equal($"EVENT {attributeName} -> {componentType}.{methodName}", activity.DisplayName);
        Assert.Equal(ActivityKind.Server, activity.Kind);
        Assert.True(activity.IsAllDataRequested);
        Assert.Equal(componentType, activity.GetTagItem("component.type"));
        Assert.Equal(methodName, activity.GetTagItem("component.method"));
        Assert.Equal(attributeName, activity.GetTagItem("attribute.name"));
        Assert.Equal("test-circuit-id", activity.GetTagItem("circuit.id"));
        Assert.False(activity.IsStopped);
    }

    [Fact]
    public void FailEventActivity_SetsErrorStatusAndStopsActivity()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();
        var activity = componentsActivitySource.StartEventActivity("TestComponent", "OnClick", "onclick");
        var exception = new InvalidOperationException("Test exception");

        // Act
        ComponentsActivitySource.FailEventActivity(activity, exception);

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
        var activity = componentsActivitySource.StartEventActivity("TestComponent", "OnClick", "onclick");
        var task = Task.CompletedTask;

        // Act
        await ComponentsActivitySource.CaptureEventStopAsync(task, activity);

        // Assert
        Assert.True(activity!.IsStopped);
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);
    }

    [Fact]
    public async Task CaptureEventStopAsync_FailsActivityOnException()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();
        var activity = componentsActivitySource.StartEventActivity("TestComponent", "OnClick", "onclick");
        var exception = new InvalidOperationException("Test exception");
        var task = Task.FromException(exception);

        // Act
        await ComponentsActivitySource.CaptureEventStopAsync(task, activity);

        // Assert
        Assert.True(activity!.IsStopped);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal(exception.GetType().FullName, activity.GetTagItem("error.type"));
    }

    [Fact]
    public void StartCircuitActivity_HandlesNullValues()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();

        // Act
        var activity = componentsActivitySource.StartCircuitActivity(null, default);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal("CIRCUIT ", activity.DisplayName);
    }

    [Fact]
    public void StartRouteActivity_HandlesNullValues()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();

        // Act
        var activity = componentsActivitySource.StartRouteActivity(null, null);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal("ROUTE unknown -> unknown", activity.DisplayName);
    }

    [Fact]
    public void StartEventActivity_HandlesNullValues()
    {
        // Arrange
        var componentsActivitySource = new ComponentsActivitySource();

        // Act
        var activity = componentsActivitySource.StartEventActivity(null, null, null);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal("EVENT unknown -> unknown.unknown", activity.DisplayName);
    }
}

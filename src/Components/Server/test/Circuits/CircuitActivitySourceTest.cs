// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.InternalTesting;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

public class CircuitActivitySourceTest
{
    private readonly ActivityListener _listener;
    private readonly List<Activity> _activities;

    public CircuitActivitySourceTest()
    {
        _activities = new List<Activity>();
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == CircuitActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => _activities.Add(activity),
            ActivityStopped = activity => { }
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [Fact]
    public void StartCircuitActivity_CreatesAndStartsActivity()
    {
        // Arrange
        var circuitActivitySource = new CircuitActivitySource(new ComponentsActivityLinkStore());
        var circuitId = "test-circuit-id";
        var httpContext = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);

        // Act
        var wrapper = circuitActivitySource.StartCircuitActivity(circuitId, httpContext, null);
        var activity = wrapper.Activity;

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(CircuitActivitySource.OnCircuitName, activity.OperationName);
        Assert.Equal($"Circuit {circuitId}", activity.DisplayName);
        Assert.Equal(ActivityKind.Internal, activity.Kind);
        Assert.True(activity.IsAllDataRequested);
        Assert.Equal(circuitId, activity.GetTagItem("aspnetcore.components.circuit.id"));
        Assert.Contains(activity.Links, link => link.Context == httpContext);
        Assert.False(activity.IsStopped);
    }

    [Fact]
    public void FailCircuitActivity_SetsErrorStatusAndStopsActivity()
    {
        // Arrange
        var circuitActivitySource = new CircuitActivitySource(new ComponentsActivityLinkStore());
        var circuitId = "test-circuit-id";
        var httpContext = default(ActivityContext);
        var activityHandle = circuitActivitySource.StartCircuitActivity(circuitId, httpContext, null);
        var activity = activityHandle.Activity;
        var exception = new InvalidOperationException("Test exception");

        // Act
        circuitActivitySource.StopCircuitActivity(activityHandle, exception);

        // Assert
        Assert.True(activity!.IsStopped);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal(exception.GetType().FullName, activity.GetTagItem("error.type"));
    }

    [Fact]
    public void StartCircuitActivity_HandlesNullValues()
    {
        // Arrange
        var circuitActivitySource = new CircuitActivitySource(new ComponentsActivityLinkStore());

        // Act
        var wrapper = circuitActivitySource.StartCircuitActivity(null, default, null);
        var activity = wrapper.Activity;

        // Assert
        Assert.NotNull(activity);
        Assert.Equal("Circuit ", activity.DisplayName);
    }

}

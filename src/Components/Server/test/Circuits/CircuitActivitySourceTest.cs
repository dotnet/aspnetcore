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
using Microsoft.AspNetCore.Components.Infrastructure.Server;

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
        var circuitActivitySource = new CircuitActivitySource();
        var linkstore = new ComponentsActivityLinkStore(null);
        circuitActivitySource.Init(linkstore);
        var circuitId = "test-circuit-id";
        var httpContext = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);

        // Act
        var activityHandle = circuitActivitySource.StartCircuitActivity(circuitId, httpContext);
        var activity = activityHandle.Activity;

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(CircuitActivitySource.OnCircuitName, activity.OperationName);
        Assert.Equal($"Circuit {circuitId}", activity.DisplayName);
        Assert.Equal(ActivityKind.Internal, activity.Kind);
        Assert.True(activity.IsAllDataRequested);
        Assert.Equal(circuitId, activity.GetTagItem("aspnetcore.components.circuit.id"));
        Assert.Empty(activity.Links);
        Assert.False(activity.IsStopped);

        circuitActivitySource.StopCircuitActivity(activityHandle, null);
        Assert.True(activity.IsStopped);
        Assert.Contains(activity.Links, link => link.Context == httpContext);
    }

    [Fact]
    public void FailCircuitActivity_SetsErrorStatusAndStopsActivity()
    {
        // Arrange
        var circuitActivitySource = new CircuitActivitySource();
        var linkstore = new ComponentsActivityLinkStore(null);
        circuitActivitySource.Init(linkstore);
        var circuitId = "test-circuit-id";
        var httpContext = default(ActivityContext);
        var activityHandle = circuitActivitySource.StartCircuitActivity(circuitId, httpContext);
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
        var circuitActivitySource = new CircuitActivitySource();
        var linkstore = new ComponentsActivityLinkStore(null);
        circuitActivitySource.Init(linkstore);

        // Act
        var activityHandle = circuitActivitySource.StartCircuitActivity(null, default);
        var activity = activityHandle.Activity;

        // Assert
        Assert.NotNull(activity);
        Assert.Equal("Circuit ", activity.DisplayName);
    }

}

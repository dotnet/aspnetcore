// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.InternalTesting;
using Moq;

namespace Microsoft.AspNetCore.Components.Rendering;

public class RenderingMetricsTest
{
    private readonly TestMeterFactory _meterFactory;

    public RenderingMetricsTest()
    {
        _meterFactory = new TestMeterFactory();
    }

    [Fact]
    public void Constructor_CreatesMetersCorrectly()
    {
        // Arrange & Act
        var renderingMetrics = new RenderingMetrics(_meterFactory);

        // Assert
        Assert.Single(_meterFactory.Meters);
        Assert.Equal(RenderingMetrics.MeterName, _meterFactory.Meters[0].Name);
    }

    [Fact]
    public void RenderStart_IncreasesCounters()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var totalCounter = new MetricCollector<long>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.count");
        using var activeCounter = new MetricCollector<long>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.active_renders");

        var componentType = "TestComponent";

        // Act
        renderingMetrics.RenderStart(componentType);

        // Assert
        var totalMeasurements = totalCounter.GetMeasurementSnapshot();
        var activeMeasurements = activeCounter.GetMeasurementSnapshot();

        Assert.Single(totalMeasurements);
        Assert.Equal(1, totalMeasurements[0].Value);
        Assert.Equal(componentType, totalMeasurements[0].Tags["component.type"]);

        Assert.Single(activeMeasurements);
        Assert.Equal(1, activeMeasurements[0].Value);
        Assert.Equal(componentType, activeMeasurements[0].Tags["component.type"]);
    }

    [Fact]
    public void RenderEnd_DecreasesActiveCounterAndRecordsDuration()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var activeCounter = new MetricCollector<long>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.active_renders");
        using var durationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.duration");

        var componentType = "TestComponent";

        // Act
        var startTime = Stopwatch.GetTimestamp();
        Thread.Sleep(10); // Add a small delay to ensure a measurable duration
        var endTime = Stopwatch.GetTimestamp();
        renderingMetrics.RenderEnd(componentType, null, startTime, endTime);

        // Assert
        var activeMeasurements = activeCounter.GetMeasurementSnapshot();
        var durationMeasurements = durationCollector.GetMeasurementSnapshot();

        Assert.Single(activeMeasurements);
        Assert.Equal(-1, activeMeasurements[0].Value);
        Assert.Equal(componentType, activeMeasurements[0].Tags["component.type"]);

        Assert.Single(durationMeasurements);
        Assert.True(durationMeasurements[0].Value > 0);
        Assert.Equal(componentType, durationMeasurements[0].Tags["component.type"]);
    }

    [Fact]
    public void RenderEnd_AddsErrorTypeTag_WhenExceptionIsProvided()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var durationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.duration");

        var componentType = "TestComponent";
        var exception = new InvalidOperationException("Test exception");

        // Act
        var startTime = Stopwatch.GetTimestamp();
        Thread.Sleep(10);
        var endTime = Stopwatch.GetTimestamp();
        renderingMetrics.RenderEnd(componentType, exception, startTime, endTime);

        // Assert
        var durationMeasurements = durationCollector.GetMeasurementSnapshot();

        Assert.Single(durationMeasurements);
        Assert.True(durationMeasurements[0].Value > 0);
        Assert.Equal(componentType, durationMeasurements[0].Tags["component.type"]);
        Assert.Equal(exception.GetType().FullName, durationMeasurements[0].Tags["error.type"]);
    }

    [Fact]
    public void IsDurationEnabled_ReturnsMeterEnabledState()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);

        // Create a collector to ensure the meter is enabled
        using var durationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.duration");

        // Act & Assert
        Assert.True(renderingMetrics.IsDurationEnabled());
    }

    [Fact]
    public void FullRenderingLifecycle_RecordsAllMetricsCorrectly()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var totalCounter = new MetricCollector<long>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.count");
        using var activeCounter = new MetricCollector<long>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.active_renders");
        using var durationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.duration");

        var componentType = "TestComponent";

        // Act - Simulating a full rendering lifecycle
        var startTime = Stopwatch.GetTimestamp();

        // 1. Component render starts
        renderingMetrics.RenderStart(componentType);

        // 2. Component render ends
        Thread.Sleep(10); // Add a small delay to ensure a measurable duration
        var endTime = Stopwatch.GetTimestamp();
        renderingMetrics.RenderEnd(componentType, null, startTime, endTime);

        // Assert
        var totalMeasurements = totalCounter.GetMeasurementSnapshot();
        var activeMeasurements = activeCounter.GetMeasurementSnapshot();
        var durationMeasurements = durationCollector.GetMeasurementSnapshot();

        // Total render count should have 1 measurement with value 1
        Assert.Single(totalMeasurements);
        Assert.Equal(1, totalMeasurements[0].Value);
        Assert.Equal(componentType, totalMeasurements[0].Tags["component.type"]);

        // Active render count should have 2 measurements (1 for start, -1 for end)
        Assert.Equal(2, activeMeasurements.Count);
        Assert.Equal(1, activeMeasurements[0].Value);
        Assert.Equal(-1, activeMeasurements[1].Value);
        Assert.Equal(componentType, activeMeasurements[0].Tags["component.type"]);
        Assert.Equal(componentType, activeMeasurements[1].Tags["component.type"]);

        // Duration should have 1 measurement with a positive value
        Assert.Single(durationMeasurements);
        Assert.True(durationMeasurements[0].Value > 0);
        Assert.Equal(componentType, durationMeasurements[0].Tags["component.type"]);
    }

    [Fact]
    public void MultipleRenders_TracksMetricsIndependently()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var totalCounter = new MetricCollector<long>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.count");
        using var activeCounter = new MetricCollector<long>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.active_renders");
        using var durationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.duration");

        var componentType1 = "TestComponent1";
        var componentType2 = "TestComponent2";

        // Act
        // First component render
        var startTime1 = Stopwatch.GetTimestamp();
        renderingMetrics.RenderStart(componentType1);

        // Second component render starts while first is still rendering
        var startTime2 = Stopwatch.GetTimestamp();
        renderingMetrics.RenderStart(componentType2);

        // First component render ends
        Thread.Sleep(5);
        var endTime1 = Stopwatch.GetTimestamp();
        renderingMetrics.RenderEnd(componentType1, null, startTime1, endTime1);

        // Second component render ends
        Thread.Sleep(5);
        var endTime2 = Stopwatch.GetTimestamp();
        renderingMetrics.RenderEnd(componentType2, null, startTime2, endTime2);

        // Assert
        var totalMeasurements = totalCounter.GetMeasurementSnapshot();
        var activeMeasurements = activeCounter.GetMeasurementSnapshot();
        var durationMeasurements = durationCollector.GetMeasurementSnapshot();

        // Should have 2 total render counts (one for each component)
        Assert.Equal(2, totalMeasurements.Count);
        Assert.Contains(totalMeasurements, m => m.Value == 1 && m.Tags["component.type"] as string == componentType1);
        Assert.Contains(totalMeasurements, m => m.Value == 1 && m.Tags["component.type"] as string == componentType2);

        // Should have 4 active render counts (start and end for each component)
        Assert.Equal(4, activeMeasurements.Count);
        Assert.Contains(activeMeasurements, m => m.Value == 1 && m.Tags["component.type"] as string == componentType1);
        Assert.Contains(activeMeasurements, m => m.Value == 1 && m.Tags["component.type"] as string == componentType2);
        Assert.Contains(activeMeasurements, m => m.Value == -1 && m.Tags["component.type"] as string == componentType1);
        Assert.Contains(activeMeasurements, m => m.Value == -1 && m.Tags["component.type"] as string == componentType2);

        // Should have 2 duration measurements (one for each component)
        Assert.Equal(2, durationMeasurements.Count);
        Assert.Contains(durationMeasurements, m => m.Value > 0 && m.Tags["component.type"] as string == componentType1);
        Assert.Contains(durationMeasurements, m => m.Value > 0 && m.Tags["component.type"] as string == componentType2);
    }
}

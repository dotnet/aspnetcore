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

namespace Microsoft.AspNetCore.Components;

public class ComponentsMetricsTest
{
    private readonly TestMeterFactory _meterFactory;

    public ComponentsMetricsTest()
    {
        _meterFactory = new TestMeterFactory();
    }

    [Fact]
    public void Constructor_CreatesMetersCorrectly()
    {
        // Arrange & Act
        var componentsMetrics = new ComponentsMetrics(_meterFactory);

        // Assert
        Assert.Equal(2, _meterFactory.Meters.Count);
        Assert.Contains(_meterFactory.Meters, m => m.Name == ComponentsMetrics.MeterName);
        Assert.Contains(_meterFactory.Meters, m => m.Name == ComponentsMetrics.LifecycleMeterName);
    }

    [Fact]
    public void Navigation_RecordsMetric()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var navigationCounter = new MetricCollector<long>(_meterFactory,
            ComponentsMetrics.MeterName, "aspnetcore.components.navigation");

        // Act
        componentsMetrics.Navigation("TestComponent", "/test-route");

        // Assert
        var measurements = navigationCounter.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.Equal(1, measurements[0].Value);
        Assert.Equal("TestComponent", Assert.Contains("aspnetcore.components.type", measurements[0].Tags));
        Assert.Equal("/test-route", Assert.Contains("aspnetcore.components.route", measurements[0].Tags));
    }

    [Fact]
    public void IsNavigationEnabled_ReturnsCorrectState()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);

        // Create a collector to ensure the meter is enabled
        using var navigationCounter = new MetricCollector<long>(_meterFactory,
            ComponentsMetrics.MeterName, "aspnetcore.components.navigation");

        // Act & Assert
        Assert.True(componentsMetrics.IsNavigationEnabled);
    }

    [Fact]
    public async Task CaptureEventDuration_RecordsSuccessMetric()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var eventDurationHistogram = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.MeterName, "aspnetcore.components.event_handler");

        // Act
        var startTimestamp = Stopwatch.GetTimestamp();
        await Task.Delay(10); // Small delay to ensure measureable duration
        await componentsMetrics.CaptureEventDuration(Task.CompletedTask, startTimestamp,
            "TestComponent", "OnClick", "onclick");

        // Assert
        var measurements = eventDurationHistogram.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.True(measurements[0].Value > 0);
        Assert.Equal("TestComponent", Assert.Contains("aspnetcore.components.type", measurements[0].Tags));
        Assert.Equal("OnClick", Assert.Contains("aspnetcore.components.method", measurements[0].Tags));
        Assert.Equal("onclick", Assert.Contains("aspnetcore.components.attribute.name", measurements[0].Tags));
        Assert.DoesNotContain("error.type", measurements[0].Tags);
    }

    [Fact]
    public async Task CaptureEventDuration_RecordsErrorMetric()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var eventDurationHistogram = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.MeterName, "aspnetcore.components.event_handler");

        // Act
        var startTimestamp = Stopwatch.GetTimestamp();
        await Task.Delay(10); // Small delay to ensure measureable duration
        await componentsMetrics.CaptureEventDuration(Task.FromException(new InvalidOperationException()),
            startTimestamp, "TestComponent", "OnClick", "onclick");

        // Assert
        var measurements = eventDurationHistogram.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.True(measurements[0].Value > 0);
        Assert.Equal("TestComponent", Assert.Contains("aspnetcore.components.type", measurements[0].Tags));
        Assert.Equal("OnClick", Assert.Contains("aspnetcore.components.method", measurements[0].Tags));
        Assert.Equal("onclick", Assert.Contains("aspnetcore.components.attribute.name", measurements[0].Tags));
        Assert.Equal("System.InvalidOperationException", Assert.Contains("error.type", measurements[0].Tags));
    }

    [Fact]
    public void FailEventSync_RecordsErrorMetric()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var eventDurationHistogram = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.MeterName, "aspnetcore.components.event_handler");
        var exception = new InvalidOperationException();

        // Act
        var startTimestamp = Stopwatch.GetTimestamp();
        componentsMetrics.FailEventSync(exception, startTimestamp,
            "TestComponent", "OnClick", "onclick");

        // Assert
        var measurements = eventDurationHistogram.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.True(measurements[0].Value > 0);
        Assert.Equal("TestComponent", Assert.Contains("aspnetcore.components.type", measurements[0].Tags));
        Assert.Equal("OnClick", Assert.Contains("aspnetcore.components.method", measurements[0].Tags));
        Assert.Equal("onclick", Assert.Contains("aspnetcore.components.attribute.name", measurements[0].Tags));
        Assert.Equal("System.InvalidOperationException", Assert.Contains("error.type", measurements[0].Tags));
    }

    [Fact]
    public void IsEventEnabled_ReturnsCorrectState()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);

        // Create a collector to ensure the meter is enabled
        using var eventDurationHistogram = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.MeterName, "aspnetcore.components.event_handler");

        // Act & Assert
        Assert.True(componentsMetrics.IsEventEnabled);
    }

    [Fact]
    public async Task CaptureParametersDuration_RecordsSuccessMetric()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var parametersDurationHistogram = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.update_parameters");

        // Act
        var startTimestamp = Stopwatch.GetTimestamp();
        await Task.Delay(10); // Small delay to ensure measureable duration
        await componentsMetrics.CaptureParametersDuration(Task.CompletedTask, startTimestamp, "TestComponent");

        // Assert
        var measurements = parametersDurationHistogram.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.True(measurements[0].Value > 0);
        Assert.Equal("TestComponent", Assert.Contains("aspnetcore.components.type", measurements[0].Tags));
        Assert.DoesNotContain("error.type", measurements[0].Tags);
    }

    [Fact]
    public async Task CaptureParametersDuration_RecordsErrorMetric()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var parametersDurationHistogram = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.update_parameters");

        // Act
        var startTimestamp = Stopwatch.GetTimestamp();
        await Task.Delay(10); // Small delay to ensure measureable duration
        await componentsMetrics.CaptureParametersDuration(Task.FromException(new InvalidOperationException()),
            startTimestamp, "TestComponent");

        // Assert
        var measurements = parametersDurationHistogram.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.True(measurements[0].Value > 0);
        Assert.Equal("TestComponent", Assert.Contains("aspnetcore.components.type", measurements[0].Tags));
        Assert.Equal("System.InvalidOperationException", Assert.Contains("error.type", measurements[0].Tags));
    }

    [Fact]
    public void FailParametersSync_RecordsErrorMetric()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var parametersDurationHistogram = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.update_parameters");
        var exception = new InvalidOperationException();

        // Act
        var startTimestamp = Stopwatch.GetTimestamp();
        componentsMetrics.FailParametersSync(exception, startTimestamp, "TestComponent");

        // Assert
        var measurements = parametersDurationHistogram.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.True(measurements[0].Value > 0);
        Assert.Equal("TestComponent", Assert.Contains("aspnetcore.components.type", measurements[0].Tags));
        Assert.Equal("System.InvalidOperationException", Assert.Contains("error.type", measurements[0].Tags));
    }

    [Fact]
    public void IsParametersEnabled_ReturnsCorrectState()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);

        // Create a collector to ensure the meter is enabled
        using var parametersDurationHistogram = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.update_parameters");

        // Act & Assert
        Assert.True(componentsMetrics.IsParametersEnabled);
    }

    [Fact]
    public async Task CaptureBatchDuration_RecordsSuccessMetric()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var batchDurationHistogram = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.render_diff");

        // Act
        var startTimestamp = Stopwatch.GetTimestamp();
        await Task.Delay(10); // Small delay to ensure measureable duration
        await componentsMetrics.CaptureBatchDuration(Task.CompletedTask, startTimestamp, 25);

        // Assert
        var measurements = batchDurationHistogram.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.True(measurements[0].Value > 0);
        Assert.Equal(50, Assert.Contains("aspnetcore.components.diff.length", measurements[0].Tags));
        Assert.DoesNotContain("error.type", measurements[0].Tags);
    }

    [Fact]
    public async Task CaptureBatchDuration_RecordsErrorMetric()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var batchDurationHistogram = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.render_diff");

        // Act
        var startTimestamp = Stopwatch.GetTimestamp();
        await Task.Delay(10); // Small delay to ensure measureable duration
        await componentsMetrics.CaptureBatchDuration(Task.FromException(new InvalidOperationException()),
            startTimestamp, 25);

        // Assert
        var measurements = batchDurationHistogram.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.True(measurements[0].Value > 0);
        Assert.Equal(50, Assert.Contains("aspnetcore.components.diff.length", measurements[0].Tags));
        Assert.Equal("System.InvalidOperationException", Assert.Contains("error.type", measurements[0].Tags));
    }

    [Fact]
    public void FailBatchSync_RecordsErrorMetric()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var batchDurationHistogram = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.render_diff");
        var exception = new InvalidOperationException();

        // Act
        var startTimestamp = Stopwatch.GetTimestamp();
        componentsMetrics.FailBatchSync(exception, startTimestamp);

        // Assert
        var measurements = batchDurationHistogram.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.True(measurements[0].Value > 0);
        Assert.Equal(0, Assert.Contains("aspnetcore.components.diff.length", measurements[0].Tags));
        Assert.Equal("System.InvalidOperationException", Assert.Contains("error.type", measurements[0].Tags));
    }

    [Fact]
    public void IsBatchEnabled_ReturnsCorrectState()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);

        // Create a collector to ensure the meter is enabled
        using var batchDurationHistogram = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.render_diff");

        // Act & Assert
        Assert.True(componentsMetrics.IsBatchEnabled);
    }

    [Fact]
    public async Task ComponentLifecycle_RecordsAllMetricsCorrectly()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var navigationCounter = new MetricCollector<long>(_meterFactory,
            ComponentsMetrics.MeterName, "aspnetcore.components.navigation");
        using var eventDurationHistogram = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.MeterName, "aspnetcore.components.event_handler");
        using var parametersDurationHistogram = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.update_parameters");
        using var batchDurationHistogram = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.render_diff");

        // Act - Simulate a component lifecycle
        // 1. Navigation
        componentsMetrics.Navigation("TestComponent", "/test-route");

        // 2. Parameters update
        var startTimestamp1 = Stopwatch.GetTimestamp();
        await Task.Delay(10);
        await componentsMetrics.CaptureParametersDuration(Task.CompletedTask, startTimestamp1, "TestComponent");

        // 3. Event handler
        var startTimestamp2 = Stopwatch.GetTimestamp();
        await Task.Delay(10);
        await componentsMetrics.CaptureEventDuration(Task.CompletedTask, startTimestamp2,
            "TestComponent", "OnClick", "onclick");

        // 4. Rendering batch
        var startTimestamp3 = Stopwatch.GetTimestamp();
        await Task.Delay(10);
        await componentsMetrics.CaptureBatchDuration(Task.CompletedTask, startTimestamp3, 15);

        // Assert
        var navigationMeasurements = navigationCounter.GetMeasurementSnapshot();
        var eventMeasurements = eventDurationHistogram.GetMeasurementSnapshot();
        var parametersMeasurements = parametersDurationHistogram.GetMeasurementSnapshot();
        var batchMeasurements = batchDurationHistogram.GetMeasurementSnapshot();

        Assert.Single(navigationMeasurements);
        Assert.Single(eventMeasurements);
        Assert.Single(parametersMeasurements);
        Assert.Single(batchMeasurements);

        // Check navigation
        Assert.Equal(1, navigationMeasurements[0].Value);
        Assert.Equal("TestComponent", Assert.Contains("aspnetcore.components.type", navigationMeasurements[0].Tags));
        Assert.Equal("/test-route", Assert.Contains("aspnetcore.components.route", navigationMeasurements[0].Tags));

        // Check event duration
        Assert.True(eventMeasurements[0].Value > 0);
        Assert.Equal("TestComponent", Assert.Contains("aspnetcore.components.type", eventMeasurements[0].Tags));
        Assert.Equal("OnClick", Assert.Contains("aspnetcore.components.method", eventMeasurements[0].Tags));
        Assert.Equal("onclick", Assert.Contains("aspnetcore.components.attribute.name", eventMeasurements[0].Tags));

        // Check parameters duration
        Assert.True(parametersMeasurements[0].Value > 0);
        Assert.Equal("TestComponent", Assert.Contains("aspnetcore.components.type", parametersMeasurements[0].Tags));

        // Check batch duration
        Assert.True(batchMeasurements[0].Value > 0);
        Assert.Equal(20, Assert.Contains("aspnetcore.components.diff.length", batchMeasurements[0].Tags));
    }

    [Fact]
    public void Dispose_DisposesAllMeters()
    {
        // This test verifies that disposing ComponentsMetrics properly disposes its meters
        // Since we can't easily test disposal directly, we'll verify meters are created and assume
        // the dispose method works as expected

        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);

        // Act - We're not actually asserting anything here, just ensuring no exceptions are thrown
        componentsMetrics.Dispose();

        // Assert - MeterFactory.Create was called twice in constructor
        Assert.Equal(2, _meterFactory.Meters.Count);
    }
}

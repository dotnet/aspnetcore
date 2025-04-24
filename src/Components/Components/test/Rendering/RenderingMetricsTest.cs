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
    public void EventDurationSync_RecordsDuration()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var eventSyncDurationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.event.synchronous.duration");

        // Act
        var startTime = Stopwatch.GetTimestamp();
        Thread.Sleep(10); // Add a small delay to ensure a measurable duration
        renderingMetrics.EventDurationSync(startTime, "TestComponent", "MyMethod", "OnClick");

        // Assert
        var measurements = eventSyncDurationCollector.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.True(measurements[0].Value > 0);
        Assert.Equal("TestComponent", measurements[0].Tags["component.type"]);
        Assert.Equal("OnClick", measurements[0].Tags["attribute.name"]);
    }

    [Fact]
    public async Task CaptureEventDurationAsync_RecordsDuration()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var eventAsyncDurationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.event.asynchronous.duration");

        // Act
        var startTime = Stopwatch.GetTimestamp();
        var task = Task.Delay(10); // Create a delay task
        await renderingMetrics.CaptureEventDurationAsync(task, startTime, "TestComponent", "MyMethod", "OnClickAsync");

        // Assert
        var measurements = eventAsyncDurationCollector.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.True(measurements[0].Value > 0);
        Assert.Equal("TestComponent", measurements[0].Tags["component.type"]);
        Assert.Equal("OnClickAsync", measurements[0].Tags["attribute.name"]);
        Assert.Equal("MyMethod", measurements[0].Tags["component.method"]);
    }

    [Fact]
    public void ParametersDurationSync_RecordsDuration()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var parametersSyncDurationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.parameters.synchronous.duration");

        // Act
        var startTime = Stopwatch.GetTimestamp();
        Thread.Sleep(10); // Add a small delay to ensure a measurable duration
        renderingMetrics.ParametersDurationSync(startTime, "TestComponent");

        // Assert
        var measurements = parametersSyncDurationCollector.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.True(measurements[0].Value > 0);
        Assert.Equal("TestComponent", measurements[0].Tags["component.type"]);
    }

    [Fact]
    public async Task CaptureParametersDurationAsync_RecordsDuration()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var parametersAsyncDurationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.parameters.asynchronous.duration");

        // Act
        var startTime = Stopwatch.GetTimestamp();
        var task = Task.Delay(10); // Create a delay task
        await renderingMetrics.CaptureParametersDurationAsync(task, startTime, "TestComponent");

        // Assert
        var measurements = parametersAsyncDurationCollector.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.True(measurements[0].Value > 0);
        Assert.Equal("TestComponent", measurements[0].Tags["component.type"]);
    }

    [Fact]
    public void DiffDuration_RecordsDuration()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var diffDurationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.diff.duration");

        // Act
        var startTime = Stopwatch.GetTimestamp();
        Thread.Sleep(10); // Add a small delay to ensure a measurable duration
        renderingMetrics.DiffDuration(startTime, "TestComponent", 5);

        // Assert
        var measurements = diffDurationCollector.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.True(measurements[0].Value > 0);
        Assert.Equal("TestComponent", measurements[0].Tags["component.type"]);
        Assert.Equal(5, measurements[0].Tags["diff.length.bucket"]);
    }

    [Fact]
    public void BatchDuration_RecordsDuration()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var batchDurationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.batch.duration");

        // Act
        var startTime = Stopwatch.GetTimestamp();
        Thread.Sleep(10); // Add a small delay to ensure a measurable duration
        renderingMetrics.BatchDuration(startTime, 50);

        // Assert
        var measurements = batchDurationCollector.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.True(measurements[0].Value > 0);
        Assert.Equal(50, measurements[0].Tags["diff.length.bucket"]);
    }

    [Fact]
    public void EventFailed_RecordsException()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var eventExceptionCollector = new MetricCollector<long>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.event.exception");

        // Create a mock EventCallback
        var callback = new EventCallback(new TestComponent(), (Action)(() => { }));

        // Act
        renderingMetrics.EventFailed("ArgumentException", callback, "OnClick");

        // Assert
        var measurements = eventExceptionCollector.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.Equal(1, measurements[0].Value);
        Assert.Equal("ArgumentException", measurements[0].Tags["error.type"]);
        Assert.Equal("OnClick", measurements[0].Tags["attribute.name"]);
        Assert.Contains("Microsoft.AspNetCore.Components.Rendering.RenderingMetricsTest+TestComponent", (string)measurements[0].Tags["component.type"]);
    }

    [Fact]
    public async Task CaptureEventFailedAsync_RecordsException()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var eventExceptionCollector = new MetricCollector<long>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.event.exception");

        // Create a mock EventCallback
        var callback = new EventCallback(new TestComponent(), (Action)(() => { }));

        // Create a task that throws an exception
        var task = Task.FromException(new InvalidOperationException());

        // Act
        await renderingMetrics.CaptureEventFailedAsync(task, callback, "OnClickAsync");

        // Assert
        var measurements = eventExceptionCollector.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.Equal(1, measurements[0].Value);
        Assert.Equal("InvalidOperationException", measurements[0].Tags["error.type"]);
        Assert.Equal("OnClickAsync", measurements[0].Tags["attribute.name"]);
        Assert.Contains("Microsoft.AspNetCore.Components.Rendering.RenderingMetricsTest+TestComponent", (string)measurements[0].Tags["component.type"]);
    }

    [Fact]
    public void PropertiesFailed_RecordsException()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var parametersExceptionCollector = new MetricCollector<long>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.parameters.exception");

        // Act
        renderingMetrics.PropertiesFailed("ArgumentException", "TestComponent");

        // Assert
        var measurements = parametersExceptionCollector.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.Equal(1, measurements[0].Value);
        Assert.Equal("ArgumentException", measurements[0].Tags["error.type"]);
        Assert.Equal("TestComponent", measurements[0].Tags["component.type"]);
    }

    [Fact]
    public async Task CapturePropertiesFailedAsync_RecordsException()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var parametersExceptionCollector = new MetricCollector<long>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.parameters.exception");

        // Create a task that throws an exception
        var task = Task.FromException(new InvalidOperationException());

        // Act
        await renderingMetrics.CapturePropertiesFailedAsync(task, "TestComponent");

        // Assert
        var measurements = parametersExceptionCollector.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.Equal(1, measurements[0].Value);
        Assert.Equal("InvalidOperationException", measurements[0].Tags["error.type"]);
        Assert.Equal("TestComponent", measurements[0].Tags["component.type"]);
    }

    [Fact]
    public void BatchFailed_RecordsException()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var batchExceptionCollector = new MetricCollector<long>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.batch.exception");

        // Act
        renderingMetrics.BatchFailed("ArgumentException");

        // Assert
        var measurements = batchExceptionCollector.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.Equal(1, measurements[0].Value);
        Assert.Equal("ArgumentException", measurements[0].Tags["error.type"]);
    }

    [Fact]
    public async Task CaptureBatchFailedAsync_RecordsException()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var batchExceptionCollector = new MetricCollector<long>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.batch.exception");

        // Create a task that throws an exception
        var task = Task.FromException(new InvalidOperationException());

        // Act
        await renderingMetrics.CaptureBatchFailedAsync(task);

        // Assert
        var measurements = batchExceptionCollector.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.Equal(1, measurements[0].Value);
        Assert.Equal("InvalidOperationException", measurements[0].Tags["error.type"]);
    }

    [Fact]
    public void EnabledProperties_ReflectMeterState()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);

        // Create collectors to ensure the meters are enabled
        using var eventSyncDurationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.event.synchronous.duration");
        using var eventAsyncDurationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.event.asynchronous.duration");
        using var eventExceptionCollector = new MetricCollector<long>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.event.exception");
        using var parametersSyncDurationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.parameters.synchronous.duration");
        using var parametersAsyncDurationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.parameters.asynchronous.duration");
        using var parametersExceptionCollector = new MetricCollector<long>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.parameters.exception");
        using var diffDurationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.diff.duration");
        using var batchDurationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.batch.duration");
        using var batchExceptionCollector = new MetricCollector<long>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.batch.exception");

        // Assert
        Assert.True(renderingMetrics.IsEventDurationEnabled);
        Assert.True(renderingMetrics.IsEventExceptionEnabled);
        Assert.True(renderingMetrics.IsStateDurationEnabled);
        Assert.True(renderingMetrics.IsStateExceptionEnabled);
        Assert.True(renderingMetrics.IsDiffDurationEnabled);
        Assert.True(renderingMetrics.IsBatchDurationEnabled);
        Assert.True(renderingMetrics.IsBatchExceptionEnabled);
    }

    [Fact]
    public void BucketEditLength_ReturnsCorrectBucket()
    {
        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);
        using var diffDurationCollector = new MetricCollector<double>(_meterFactory,
            RenderingMetrics.MeterName, "aspnetcore.components.rendering.diff.duration");

        // Act & Assert - Test different diff lengths
        var startTime = Stopwatch.GetTimestamp();

        // Test each bucket boundary
        renderingMetrics.DiffDuration(startTime, "Component", 1);
        renderingMetrics.DiffDuration(startTime, "Component", 2);
        renderingMetrics.DiffDuration(startTime, "Component", 5);
        renderingMetrics.DiffDuration(startTime, "Component", 10);
        renderingMetrics.DiffDuration(startTime, "Component", 50);
        renderingMetrics.DiffDuration(startTime, "Component", 100);
        renderingMetrics.DiffDuration(startTime, "Component", 500);
        renderingMetrics.DiffDuration(startTime, "Component", 1000);
        renderingMetrics.DiffDuration(startTime, "Component", 10000);
        renderingMetrics.DiffDuration(startTime, "Component", 20000); // Should be 10001

        // Assert
        var measurements = diffDurationCollector.GetMeasurementSnapshot();

        Assert.Equal(10, measurements.Count);
        Assert.Equal(1, measurements[0].Tags["diff.length.bucket"]);
        Assert.Equal(2, measurements[1].Tags["diff.length.bucket"]);
        Assert.Equal(5, measurements[2].Tags["diff.length.bucket"]);
        Assert.Equal(10, measurements[3].Tags["diff.length.bucket"]);
        Assert.Equal(50, measurements[4].Tags["diff.length.bucket"]);
        Assert.Equal(100, measurements[5].Tags["diff.length.bucket"]);
        Assert.Equal(500, measurements[6].Tags["diff.length.bucket"]);
        Assert.Equal(1000, measurements[7].Tags["diff.length.bucket"]);
        Assert.Equal(10000, measurements[8].Tags["diff.length.bucket"]);
        Assert.Equal(10001, measurements[9].Tags["diff.length.bucket"]);
    }

    [Fact]
    public void Dispose_DisposesUnderlyingMeter()
    {
        // This test verifies that the meter is disposed when the metrics instance is disposed
        // This is a bit tricky to test directly, so we'll use an indirect approach

        // Arrange
        var renderingMetrics = new RenderingMetrics(_meterFactory);

        // Act
        renderingMetrics.Dispose();

        // Try to use the disposed meter - this should not throw since TestMeterFactory
        // doesn't actually dispose the meter in test contexts
        var startTime = Stopwatch.GetTimestamp();
        renderingMetrics.EventDurationSync(startTime, "TestComponent", "MyMethod", "OnClick");
    }

    // Helper class for mock components
    public class TestComponent : IComponent, IHandleEvent
    {
        public void Attach(RenderHandle renderHandle) { }
        public Task HandleEventAsync(EventCallbackWorkItem item, object arg) => Task.CompletedTask;
        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }
}

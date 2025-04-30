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
        Assert.Equal(ComponentsMetrics.MeterName, _meterFactory.Meters[0].Name);
    }

    [Fact]
    public async Task CaptureEventDurationAsync_RecordsDuration()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var eventAsyncDurationCollector = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.MeterName, "aspnetcore.components.event.duration");

        // Act
        var startTime = Stopwatch.GetTimestamp();
        var task = Task.Delay(10); // Create a delay task
        await componentsMetrics.CaptureEventDurationAsync(task, startTime, "TestComponent", "MyMethod", "OnClickAsync");

        // Assert
        var measurements = eventAsyncDurationCollector.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.True(measurements[0].Value > 0);
        Assert.Equal("TestComponent", measurements[0].Tags["component.type"]);
        Assert.Equal("OnClickAsync", measurements[0].Tags["attribute.name"]);
        Assert.Equal("MyMethod", measurements[0].Tags["component.method"]);
    }

    [Fact]
    public async Task CaptureParametersDurationAsync_RecordsDuration()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var parametersAsyncDurationCollector = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.update_parameters.duration");

        // Act
        var startTime = Stopwatch.GetTimestamp();
        var task = Task.Delay(10); // Create a delay task
        await componentsMetrics.CaptureParametersDurationAsync(task, startTime, "TestComponent");

        // Assert
        var measurements = parametersAsyncDurationCollector.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.True(measurements[0].Value > 0);
        Assert.Equal("TestComponent", measurements[0].Tags["component.type"]);
    }

    [Fact]
    public void BatchDuration_RecordsDuration()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var batchDurationCollector = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.rendering.batch.duration");

        // Act
        var startTime = Stopwatch.GetTimestamp();
        Thread.Sleep(10); // Add a small delay to ensure a measurable duration
        componentsMetrics.BatchDuration(startTime, 50);

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
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var eventExceptionCollector = new MetricCollector<long>(_meterFactory,
            ComponentsMetrics.MeterName, "aspnetcore.components.event.exceptions");

        // Create a mock EventCallback
        var callback = new EventCallback(new TestComponent(), () => { });

        // Act
        componentsMetrics.EventFailed("ArgumentException", callback, "OnClick");

        // Assert
        var measurements = eventExceptionCollector.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.Equal(1, measurements[0].Value);
        Assert.Equal("ArgumentException", measurements[0].Tags["error.type"]);
        Assert.Equal("OnClick", measurements[0].Tags["attribute.name"]);
        Assert.Contains("Microsoft.AspNetCore.Components.ComponentsMetricsTest+TestComponent", (string)measurements[0].Tags["component.type"]);
    }

    [Fact]
    public async Task CaptureEventFailedAsync_RecordsException()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var eventExceptionCollector = new MetricCollector<long>(_meterFactory,
            ComponentsMetrics.MeterName, "aspnetcore.components.event.exceptions");

        // Create a mock EventCallback
        var callback = new EventCallback(new TestComponent(), () => { });

        // Create a task that throws an exception
        var task = Task.FromException(new InvalidOperationException());

        // Act
        await componentsMetrics.CaptureEventFailedAsync(task, callback, "OnClickAsync");

        // Assert
        var measurements = eventExceptionCollector.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.Equal(1, measurements[0].Value);
        Assert.Equal("InvalidOperationException", measurements[0].Tags["error.type"]);
        Assert.Equal("OnClickAsync", measurements[0].Tags["attribute.name"]);
        Assert.Contains("Microsoft.AspNetCore.Components.ComponentsMetricsTest+TestComponent", (string)measurements[0].Tags["component.type"]);
    }

    [Fact]
    public void PropertiesFailed_RecordsException()
    {
        // Arrange
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var parametersExceptionCollector = new MetricCollector<long>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.update_parameters.exceptions");

        // Act
        componentsMetrics.PropertiesFailed("ArgumentException", "TestComponent");

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
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var parametersExceptionCollector = new MetricCollector<long>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.update_parameters.exceptions");

        // Create a task that throws an exception
        var task = Task.FromException(new InvalidOperationException());

        // Act
        await componentsMetrics.CapturePropertiesFailedAsync(task, "TestComponent");

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
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var batchExceptionCollector = new MetricCollector<long>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.rendering.batch.exceptions");

        // Act
        componentsMetrics.BatchFailed("ArgumentException");

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
        var componentsMetrics = new ComponentsMetrics(_meterFactory);
        using var batchExceptionCollector = new MetricCollector<long>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.rendering.batch.exceptions");

        // Create a task that throws an exception
        var task = Task.FromException(new InvalidOperationException());

        // Act
        await componentsMetrics.CaptureBatchFailedAsync(task);

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
        var componentsMetrics = new ComponentsMetrics(_meterFactory);

        // Create collectors to ensure the meters are enabled
        using var eventAsyncDurationCollector = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.MeterName, "aspnetcore.components.event.duration");
        using var eventExceptionCollector = new MetricCollector<long>(_meterFactory,
            ComponentsMetrics.MeterName, "aspnetcore.components.event.exceptions");
        using var parametersAsyncDurationCollector = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.update_parameters.duration");
        using var parametersExceptionCollector = new MetricCollector<long>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.update_parameters.exceptions");
        using var batchDurationCollector = new MetricCollector<double>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.rendering.batch.duration");
        using var batchExceptionCollector = new MetricCollector<long>(_meterFactory,
            ComponentsMetrics.LifecycleMeterName, "aspnetcore.components.rendering.batch.exceptions");

        // Assert
        Assert.True(componentsMetrics.IsEventDurationEnabled);
        Assert.True(componentsMetrics.IsEventExceptionEnabled);
        Assert.True(componentsMetrics.IsParametersDurationEnabled);
        Assert.True(componentsMetrics.IsParametersExceptionEnabled);
        Assert.True(componentsMetrics.IsBatchDurationEnabled);
        Assert.True(componentsMetrics.IsBatchExceptionEnabled);
    }

    // Helper class for mock components
    public class TestComponent : IComponent, IHandleEvent
    {
        public void Attach(RenderHandle renderHandle) { }
        public Task HandleEventAsync(EventCallbackWorkItem item, object arg) => Task.CompletedTask;
        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }
}

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

public class CircuitMetricsTest
{
    private readonly TestMeterFactory _meterFactory;

    public CircuitMetricsTest()
    {
        _meterFactory = new TestMeterFactory();
    }

    [Fact]
    public void Constructor_CreatesMetersCorrectly()
    {
        // Arrange & Act
        var circuitMetrics = new CircuitMetrics(_meterFactory);

        // Assert
        Assert.Single(_meterFactory.Meters);
        Assert.Equal(CircuitMetrics.MeterName, _meterFactory.Meters[0].Name);
    }

    [Fact]
    public void OnCircuitOpened_IncreasesCounters()
    {
        // Arrange
        var circuitMetrics = new CircuitMetrics(_meterFactory);
        using var activeCircuitCounter = new MetricCollector<long>(_meterFactory,
            CircuitMetrics.MeterName, "aspnetcore.components.circuit.active");

        // Act
        circuitMetrics.OnCircuitOpened();

        // Assert
        var activeMeasurements = activeCircuitCounter.GetMeasurementSnapshot();

        Assert.Single(activeMeasurements);
        Assert.Equal(1, activeMeasurements[0].Value);
    }

    [Fact]
    public void OnConnectionUp_IncreasesConnectedCounter()
    {
        // Arrange
        var circuitMetrics = new CircuitMetrics(_meterFactory);
        using var connectedCircuitCounter = new MetricCollector<long>(_meterFactory,
            CircuitMetrics.MeterName, "aspnetcore.components.circuit.connected");

        // Act
        circuitMetrics.OnConnectionUp();

        // Assert
        var measurements = connectedCircuitCounter.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.Equal(1, measurements[0].Value);
    }

    [Fact]
    public void OnConnectionDown_DecreasesConnectedCounter()
    {
        // Arrange
        var circuitMetrics = new CircuitMetrics(_meterFactory);
        using var connectedCircuitCounter = new MetricCollector<long>(_meterFactory,
            CircuitMetrics.MeterName, "aspnetcore.components.circuit.connected");

        // Act
        circuitMetrics.OnConnectionDown();

        // Assert
        var measurements = connectedCircuitCounter.GetMeasurementSnapshot();

        Assert.Single(measurements);
        Assert.Equal(-1, measurements[0].Value);
    }

    [Fact]
    public async Task OnCircuitDown_UpdatesCountersAndRecordsDuration()
    {
        // Arrange
        var circuitMetrics = new CircuitMetrics(_meterFactory);
        using var activeCircuitCounter = new MetricCollector<long>(_meterFactory,
            CircuitMetrics.MeterName, "aspnetcore.components.circuit.active");
        using var connectedCircuitCounter = new MetricCollector<long>(_meterFactory,
            CircuitMetrics.MeterName, "aspnetcore.components.circuit.connected");
        using var circuitDurationCollector = new MetricCollector<double>(_meterFactory,
            CircuitMetrics.MeterName, "aspnetcore.components.circuit.duration");

        // Act
        var startTime = Stopwatch.GetTimestamp();
        await Task.Delay(10); // Add a small delay to ensure a measurable duration
        var endTime = Stopwatch.GetTimestamp();
        circuitMetrics.OnCircuitDown(startTime, endTime);

        // Assert
        var activeMeasurements = activeCircuitCounter.GetMeasurementSnapshot();
        var connectedMeasurements = connectedCircuitCounter.GetMeasurementSnapshot();
        var durationMeasurements = circuitDurationCollector.GetMeasurementSnapshot();

        Assert.Single(activeMeasurements);
        Assert.Equal(-1, activeMeasurements[0].Value);

        Assert.Single(connectedMeasurements);
        Assert.Equal(-1, connectedMeasurements[0].Value);

        Assert.Single(durationMeasurements);
        Assert.True(durationMeasurements[0].Value > 0);
    }

    [Fact]
    public void IsDurationEnabled_ReturnsMeterEnabledState()
    {
        // Arrange
        var circuitMetrics = new CircuitMetrics(_meterFactory);

        // Create a collector to ensure the meter is enabled
        using var circuitDurationCollector = new MetricCollector<double>(_meterFactory,
            CircuitMetrics.MeterName, "aspnetcore.components.circuit.duration");

        // Act & Assert
        Assert.True(circuitMetrics.IsDurationEnabled());
    }

    [Fact]
    public void FullCircuitLifecycle_RecordsAllMetricsCorrectly()
    {
        // Arrange
        var circuitMetrics = new CircuitMetrics(_meterFactory);
        using var activeCircuitCounter = new MetricCollector<long>(_meterFactory,
            CircuitMetrics.MeterName, "aspnetcore.components.circuit.active");
        using var connectedCircuitCounter = new MetricCollector<long>(_meterFactory,
            CircuitMetrics.MeterName, "aspnetcore.components.circuit.connected");
        using var circuitDurationCollector = new MetricCollector<double>(_meterFactory,
            CircuitMetrics.MeterName, "aspnetcore.components.circuit.duration");

        // Act - Simulating a full circuit lifecycle
        var startTime = Stopwatch.GetTimestamp();

        // 1. Circuit opens
        circuitMetrics.OnCircuitOpened();

        // 2. Connection established
        circuitMetrics.OnConnectionUp();

        // 3. Connection drops
        circuitMetrics.OnConnectionDown();

        // 4. Connection re-established
        circuitMetrics.OnConnectionUp();

        // 5. Circuit closes
        Thread.Sleep(10); // Add a small delay to ensure a measurable duration
        var endTime = Stopwatch.GetTimestamp();
        circuitMetrics.OnCircuitDown(startTime, endTime);

        // Assert
        var activeMeasurements = activeCircuitCounter.GetMeasurementSnapshot();
        var connectedMeasurements = connectedCircuitCounter.GetMeasurementSnapshot();
        var durationMeasurements = circuitDurationCollector.GetMeasurementSnapshot();

        // Active circuit count should have 2 measurements (1 for open, -1 for close)
        Assert.Equal(2, activeMeasurements.Count);
        Assert.Equal(1, activeMeasurements[0].Value);
        Assert.Equal(-1, activeMeasurements[1].Value);

        // Connected circuit count should have 4 measurements (connecting, disconnecting, reconnecting, closing)
        Assert.Equal(4, connectedMeasurements.Count);
        Assert.Equal(1, connectedMeasurements[0].Value);
        Assert.Equal(-1, connectedMeasurements[1].Value);
        Assert.Equal(1, connectedMeasurements[2].Value);
        Assert.Equal(-1, connectedMeasurements[3].Value);

        // Duration should have 1 measurement with a positive value
        Assert.Single(durationMeasurements);
        Assert.True(durationMeasurements[0].Value > 0);
    }
}

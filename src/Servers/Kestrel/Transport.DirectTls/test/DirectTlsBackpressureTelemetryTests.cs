// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Net.Security;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Tests;

public class DirectTlsBackpressureTelemetryTests : LoggedTest
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task ReceiveBackpressure_LogsPauseAndResume()
    {
        using var meter = new Meter($"{nameof(DirectTlsBackpressureTelemetryTests)}.{Guid.NewGuid():N}");
        using var meterListener = new MeterListener();
        var pausedConnectionMeasurements = new ConcurrentQueue<long>();
        var pauseMeasurementRecorded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resumeMeasurementRecorded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == meter.Name &&
                instrument.Name == DirectTlsMetrics.PausedConnectionsInstrumentName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((_, measurement, _, _) =>
        {
            pausedConnectionMeasurements.Enqueue(measurement);
            (measurement == 1 ? pauseMeasurementRecorded : resumeMeasurementRecorded).TrySetResult();
        });
        meterListener.Start();

        using var pump = new TestTlsEventPump();
        var connectionState = new BackpressureConnectionIoState(
            LoggerFactory.CreateLogger<ConnectionIoState>(),
            new DirectTlsMetrics(meter))
        {
            Pump = pump
        };
        var connection = new DirectTlsConnection(
            connectionState,
            pump,
            localEndPoint: null,
            remoteEndPoint: null,
            MemoryPool<byte>.Shared,
            maxReadBufferSize: 1,
            maxWriteBufferSize: 0,
            LoggerFactory.CreateLogger<DirectTlsConnection>());

        connection.Start();

        await connectionState.ReadInterestSuspended.Task.WaitAsync(Timeout);
        await pauseMeasurementRecorded.Task.WaitAsync(Timeout);
        await WaitForLogAsync("ConnectionPause");

        Assert.Contains(TestSink.Writes, write =>
            write.EventId.Name == "ConnectionPause" &&
            write.Message.Contains(connection.ConnectionId, StringComparison.Ordinal));
        Assert.Equal([1L], pausedConnectionMeasurements);

        var readResult = await connection.Transport.Input.ReadAsync().AsTask().WaitAsync(Timeout);
        connection.Transport.Input.AdvanceTo(readResult.Buffer.End);

        await connectionState.ReadInterestResumed.Task.WaitAsync(Timeout);
        await resumeMeasurementRecorded.Task.WaitAsync(Timeout);
        await WaitForLogAsync("ConnectionResume");

        Assert.Contains(TestSink.Writes, write =>
            write.EventId.Name == "ConnectionResume" &&
            write.Message.Contains(connection.ConnectionId, StringComparison.Ordinal));
        Assert.Equal([1L, -1L], pausedConnectionMeasurements);

        connection.Abort(new ConnectionAbortedException("Test complete."));
        await connection.DisposeAsync();
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void CancelWhilePaused_ReleasesPausedConnectionMeasurement()
    {
        using var meter = new Meter($"{nameof(DirectTlsBackpressureTelemetryTests)}.{Guid.NewGuid():N}");
        using var meterListener = new MeterListener();
        var pausedConnectionMeasurements = new ConcurrentQueue<long>();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == meter.Name &&
                instrument.Name == DirectTlsMetrics.PausedConnectionsInstrumentName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((_, measurement, _, _) =>
        {
            pausedConnectionMeasurements.Enqueue(measurement);
        });
        meterListener.Start();

        var connectionState = new BackpressureConnectionIoState(
            LoggerFactory.CreateLogger<ConnectionIoState>(),
            new DirectTlsMetrics(meter));

        connectionState.SuspendReadInterest();
        connectionState.Cancel();

        Assert.Equal([1L, -1L], pausedConnectionMeasurements);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void FailedSuspend_DoesNotRecordPausedConnectionMeasurement()
    {
        using var meter = new Meter($"{nameof(DirectTlsBackpressureTelemetryTests)}.{Guid.NewGuid():N}");
        using var meterListener = new MeterListener();
        var pausedConnectionMeasurements = new ConcurrentQueue<long>();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == meter.Name &&
                instrument.Name == DirectTlsMetrics.PausedConnectionsInstrumentName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((_, measurement, _, _) =>
        {
            pausedConnectionMeasurements.Enqueue(measurement);
        });
        meterListener.Start();

        var connectionState = new BackpressureConnectionIoState(
            LoggerFactory.CreateLogger<ConnectionIoState>(),
            new DirectTlsMetrics(meter))
        {
            ThrowOnApplyEvents = true
        };

        Assert.Throws<InvalidOperationException>(connectionState.SuspendReadInterest);
        connectionState.Cancel();

        Assert.Empty(pausedConnectionMeasurements);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void FailedResume_ReleasesPausedConnectionMeasurementOnCancel()
    {
        using var meter = new Meter($"{nameof(DirectTlsBackpressureTelemetryTests)}.{Guid.NewGuid():N}");
        using var meterListener = new MeterListener();
        var pausedConnectionMeasurements = new ConcurrentQueue<long>();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == meter.Name &&
                instrument.Name == DirectTlsMetrics.PausedConnectionsInstrumentName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((_, measurement, _, _) =>
        {
            pausedConnectionMeasurements.Enqueue(measurement);
        });
        meterListener.Start();

        var connectionState = new BackpressureConnectionIoState(
            LoggerFactory.CreateLogger<ConnectionIoState>(),
            new DirectTlsMetrics(meter));

        connectionState.SuspendReadInterest();
        connectionState.ThrowOnApplyEvents = true;
        Assert.Throws<InvalidOperationException>(connectionState.ResumeReadInterest);
        connectionState.Cancel();

        Assert.Equal([1L, -1L], pausedConnectionMeasurements);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void ListenerEnabledWhilePaused_DoesNotRecordUnmatchedResumeMeasurement()
    {
        using var meter = new Meter($"{nameof(DirectTlsBackpressureTelemetryTests)}.{Guid.NewGuid():N}");
        var connectionState = new BackpressureConnectionIoState(
            LoggerFactory.CreateLogger<ConnectionIoState>(),
            new DirectTlsMetrics(meter));

        connectionState.SuspendReadInterest();

        using var meterListener = new MeterListener();
        var pausedConnectionMeasurements = new ConcurrentQueue<long>();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == meter.Name &&
                instrument.Name == DirectTlsMetrics.PausedConnectionsInstrumentName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((_, measurement, _, _) =>
        {
            pausedConnectionMeasurements.Enqueue(measurement);
        });
        meterListener.Start();

        connectionState.ResumeReadInterest();

        Assert.Empty(pausedConnectionMeasurements);
    }

    private async Task WaitForLogAsync(string eventName)
    {
        var timeoutAt = DateTime.UtcNow + Timeout;
        while (!TestSink.Writes.Any(write => write.EventId.Name == eventName))
        {
            Assert.True(DateTime.UtcNow < timeoutAt, $"Timed out waiting for log event '{eventName}'.");
            await Task.Delay(10);
        }
    }

    private sealed class BackpressureConnectionIoState : ConnectionIoState
    {
        private int _readCount;

        public BackpressureConnectionIoState(ILogger logger, DirectTlsMetrics metrics)
            : base(fd: 7, session: null!, logger, metrics)
        {
            SetHandshakeComplete();
        }

        public TaskCompletionSource ReadInterestSuspended { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReadInterestResumed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool ThrowOnApplyEvents { get; set; }

        internal override TlsOperationStatus RawRead(Span<byte> buffer, out int bytesRead)
        {
            if (Interlocked.Increment(ref _readCount) == 1)
            {
                buffer[0] = 42;
                bytesRead = 1;
                return TlsOperationStatus.Complete;
            }

            bytesRead = 0;
            return TlsOperationStatus.NeedMoreData;
        }

        internal override void ApplyEvents(uint events)
        {
            if (ThrowOnApplyEvents)
            {
                throw new InvalidOperationException("Test event update failure.");
            }

            if ((events & NativeTls.EPOLLIN) == 0)
            {
                ReadInterestSuspended.TrySetResult();
            }
            else
            {
                ReadInterestResumed.TrySetResult();
            }
        }

        internal override void ShutdownSession()
        {
        }

        internal override void DisposeSession()
        {
        }
    }

    private sealed class TestTlsEventPump()
        : TlsEventPump(NullLogger<TlsEventPump>.Instance, id: 0, System.Threading.Timeout.InfiniteTimeSpan)
    {
        internal override void DeregisterFromEpoll(int fd)
        {
        }
    }
}

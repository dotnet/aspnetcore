// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Rendering;

internal sealed class RenderingMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.Components.Rendering";
    private readonly Meter _meter;

    private readonly Histogram<double> _eventSyncDuration;
    private readonly Histogram<double> _eventAsyncDuration;
    private readonly Counter<long> _eventException;

    private readonly Histogram<double> _parametersSyncDuration;
    private readonly Histogram<double> _parametersAsyncDuration;
    private readonly Counter<long> _parametersException;

    private readonly Histogram<double> _diffDuration;

    private readonly Histogram<double> _batchDuration;
    private readonly Counter<long> _batchException;

    [FeatureSwitchDefinition("System.Diagnostics.Metrics.Meter.IsSupported")]
    public static bool IsMetricsSupported { get; } = InitializeIsMetricsSupported();
    private static bool InitializeIsMetricsSupported() => AppContext.TryGetSwitch("System.Diagnostics.Metrics.Meter.IsSupported", out bool isSupported) ? isSupported : true;

    public bool IsEventDurationEnabled => IsMetricsSupported && (_eventSyncDuration.Enabled || _eventAsyncDuration.Enabled);
    public bool IsEventExceptionEnabled => IsMetricsSupported && _eventException.Enabled;

    public bool IsStateDurationEnabled => IsMetricsSupported && (_parametersSyncDuration.Enabled || _parametersAsyncDuration.Enabled);
    public bool IsStateExceptionEnabled => IsMetricsSupported && _parametersException.Enabled;

    public bool IsDiffDurationEnabled => IsMetricsSupported && _diffDuration.Enabled;

    public bool IsBatchDurationEnabled => IsMetricsSupported && _batchDuration.Enabled;
    public bool IsBatchExceptionEnabled => IsMetricsSupported && _batchException.Enabled;

    public RenderingMetrics(IMeterFactory meterFactory)
    {
        if (!IsMetricsSupported)
        {
            // TryAddSingleton prevents trimming constructors, so we trim constructor this way
            throw new NotSupportedException("Metrics are not supported in this environment.");
        }

        Debug.Assert(meterFactory != null);

        _meter = meterFactory.Create(MeterName);

        _eventSyncDuration = _meter.CreateHistogram(
            "aspnetcore.components.rendering.event.synchronous.duration",
            unit: "s",
            description: "Duration of processing browser event synchronously.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.ShortSecondsBucketBoundaries });

        _eventAsyncDuration = _meter.CreateHistogram(
            "aspnetcore.components.rendering.event.asynchronous.duration",
            unit: "s",
            description: "Duration of processing browser event asynchronously.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.ShortSecondsBucketBoundaries });

        _eventException = _meter.CreateCounter<long>(
            "aspnetcore.components.rendering.event.exception",
            unit: "{exceptions}",
            description: "Total number of exceptions during browser event processing.");

        _parametersSyncDuration = _meter.CreateHistogram(
            "aspnetcore.components.rendering.parameters.synchronous.duration",
            unit: "s",
            description: "Duration of processing component parameters synchronously.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.ShortSecondsBucketBoundaries });

        _parametersAsyncDuration = _meter.CreateHistogram(
            "aspnetcore.components.rendering.parameters.asynchronous.duration",
            unit: "s",
            description: "Duration of processing component parameters asynchronously.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.ShortSecondsBucketBoundaries });

        _parametersException = _meter.CreateCounter<long>(
            "aspnetcore.components.rendering.parameters.exception",
            unit: "{exceptions}",
            description: "Total number of exceptions during processing component parameters.");

        _diffDuration = _meter.CreateHistogram(
            "aspnetcore.components.rendering.diff.duration",
            unit: "s",
            description: "Duration of rendering component HTML diff.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.ShortSecondsBucketBoundaries });

        _batchDuration = _meter.CreateHistogram(
            "aspnetcore.components.rendering.batch.duration",
            unit: "s",
            description: "Duration of rendering batch.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.ShortSecondsBucketBoundaries });

        _batchException = _meter.CreateCounter<long>(
            "aspnetcore.components.rendering.batch.exception",
            unit: "{exceptions}",
            description: "Total number of exceptions during batch rendering.");
    }

    public void EventDurationSync(long startTimestamp, string? componentType, string? attributeName)
    {
        var tags = new TagList
        {
            { "component.type", componentType ?? "unknown" },
            { "attribute.name", attributeName ?? "unknown"}
        };

        var duration = Stopwatch.GetElapsedTime(startTimestamp);
        _eventSyncDuration.Record(duration.TotalSeconds, tags);
    }

    public async Task CaptureEventDurationAsync(Task task, long startTimestamp, string? componentType, string? attributeName)
    {
        try
        {
            await task;

            var tags = new TagList
            {
                { "component.type", componentType ?? "unknown" },
                { "attribute.name", attributeName ?? "unknown" }
            };

            var duration = Stopwatch.GetElapsedTime(startTimestamp);
            _eventAsyncDuration.Record(duration.TotalSeconds, tags);
        }
        catch
        {
            // none
        }
    }

    public void ParametersDurationSync(long startTimestamp, string? componentType)
    {
        var tags = new TagList
        {
            { "component.type", componentType ?? "unknown" },
        };

        var duration = Stopwatch.GetElapsedTime(startTimestamp);
        _parametersSyncDuration.Record(duration.TotalSeconds, tags);
    }

    public async Task CaptureParametersDurationAsync(Task task, long startTimestamp, string? componentType)
    {
        try
        {
            await task;

            var tags = new TagList
            {
                { "component.type", componentType ?? "unknown" },
            };

            var duration = Stopwatch.GetElapsedTime(startTimestamp);
            _parametersAsyncDuration.Record(duration.TotalSeconds, tags);
        }
        catch
        {
            // none
        }
    }

    public void DiffDuration(long startTimestamp, string? componentType, int diffLength)
    {
        var tags = new TagList
        {
            { "component.type", componentType ?? "unknown" },
            { "diff.length.bucket", BucketEditLength(diffLength) }
        };

        var duration = Stopwatch.GetElapsedTime(startTimestamp);
        _diffDuration.Record(duration.TotalSeconds, tags);
    }

    public void BatchDuration(long startTimestamp, int diffLength)
    {
        var tags = new TagList
        {
            { "diff.length.bucket", BucketEditLength(diffLength) }
        };

        var duration = Stopwatch.GetElapsedTime(startTimestamp);
        _batchDuration.Record(duration.TotalSeconds, tags);
    }

    public void EventFailed(string? exceptionType, EventCallback callback, string? attributeName)
    {
        var receiverName = (callback.Receiver?.GetType() ?? callback.Delegate?.Target?.GetType())?.FullName;
        var tags = new TagList
        {
            { "component.type", receiverName ?? "unknown" },
            { "attribute.name", attributeName  ?? "unknown"},
            { "error.type", exceptionType ?? "unknown"}
        };
        _eventException.Add(1, tags);
    }

    public async Task CaptureEventFailedAsync(Task task, EventCallback callback, string? attributeName)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            EventFailed(ex.GetType().Name, callback, attributeName);
        }
    }

    public void PropertiesFailed(string? exceptionType, string? componentType)
    {
        var tags = new TagList
        {
            { "component.type", componentType ?? "unknown" },
            { "error.type", exceptionType ?? "unknown"}
        };
        _parametersException.Add(1, tags);
    }

    public async Task CapturePropertiesFailedAsync(Task task, string? componentType)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            PropertiesFailed(ex.GetType().Name, componentType);
        }
    }

    public void BatchFailed(string? exceptionType)
    {
        var tags = new TagList
        {
            { "error.type", exceptionType ?? "unknown"}
        };
        _batchException.Add(1, tags);
    }

    public async Task CaptureBatchFailedAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            BatchFailed(ex.GetType().Name);
        }
    }

    private static int BucketEditLength(int batchLength)
    {
        return batchLength switch
        {
            <= 1 => 1,
            <= 2 => 2,
            <= 5 => 5,
            <= 10 => 10,
            <= 50 => 50,
            <= 100 => 100,
            <= 500 => 500,
            <= 1000 => 1000,
            <= 10000 => 10000,
            _ => 10001,
        };
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}

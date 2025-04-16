// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Rendering;

internal sealed class RenderingMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.Components.Rendering";

    private readonly Meter _meter;
    private readonly Counter<long> _renderCounter;
    private readonly Histogram<double> _renderDuration;

    public RenderingMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _renderCounter = _meter.CreateCounter<long>(
            "aspnetcore.components.rendering.count",
            unit: "{render}",
            description: "Number of component renders performed.");

        _renderDuration = _meter.CreateHistogram<double>(
            "aspnetcore.components.rendering.duration",
            unit: "ms",
            description: "Duration of component rendering operations per component.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.ShortSecondsBucketBoundaries });
    }

    public void RenderStart(string componentType)
    {
        var tags = new TagList();
        tags = InitializeRequestTags(componentType, tags);

        _renderCounter.Add(1, tags);
    }

    public void RenderEnd(string componentType, Exception? exception, long startTimestamp, long currentTimestamp)
    {
        var tags = new TagList();
        tags = InitializeRequestTags(componentType, tags);

        // Tags must match request start.
        if (_renderCounter.Enabled)
        {
            _renderCounter.Add(-1, tags);
        }

        if (_renderDuration.Enabled)
        {
            if (exception != null)
            {
                TryAddTag(ref tags, "error.type", exception.GetType().FullName);
            }

            var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
            _renderDuration.Record(duration.TotalMilliseconds, tags);
        }
    }

    private static TagList InitializeRequestTags(string componentType, TagList tags)
    {
        tags.Add("component.type", componentType);
        return tags;
    }

    public bool IsEnabled() => _renderCounter.Enabled || _renderDuration.Enabled;

    public void Dispose()
    {
        _meter.Dispose();
    }

    private static bool TryAddTag(ref TagList tags, string name, object? value)
    {
        for (var i = 0; i < tags.Count; i++)
        {
            if (tags[i].Key == name)
            {
                return false;
            }
        }

        tags.Add(new KeyValuePair<string, object?>(name, value));
        return true;
    }
}

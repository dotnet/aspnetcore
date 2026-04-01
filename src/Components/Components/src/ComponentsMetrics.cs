// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components;

internal sealed class ComponentsMetrics : IDisposable
{
    public const string MeterName = "Microsoft.AspNetCore.Components";
    public const string LifecycleMeterName = "Microsoft.AspNetCore.Components.Lifecycle";
    private readonly Meter _meter;
    private readonly Meter _lifeCycleMeter;

    private readonly Counter<long> _navigationCount;

    private readonly Histogram<double> _eventDuration;
    private readonly Histogram<double> _parametersDuration;
    private readonly Histogram<double> _batchDuration;
    private readonly Histogram<int> _batchSize;

    public bool IsNavigationEnabled => _navigationCount.Enabled;

    public bool IsEventEnabled => _eventDuration.Enabled;

    public bool IsParametersEnabled => _parametersDuration.Enabled;

    public bool IsBatchEnabled => _batchDuration.Enabled;

    public ComponentsMetrics(IMeterFactory meterFactory)
    {
        Debug.Assert(meterFactory != null);

        _meter = meterFactory.Create(MeterName);
        _lifeCycleMeter = meterFactory.Create(LifecycleMeterName);

        _navigationCount = _meter.CreateCounter<long>(
            "aspnetcore.components.navigate",
            unit: "{route}",
            description: "Total number of route changes.");

        _eventDuration = _meter.CreateHistogram(
            "aspnetcore.components.handle_event.duration",
            unit: "s",
            description: "Duration of processing browser event.  It includes business logic of the component but not affected child components.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.ShortSecondsBucketBoundaries });

        _parametersDuration = _lifeCycleMeter.CreateHistogram(
            "aspnetcore.components.update_parameters.duration",
            unit: "s",
            description: "Duration of processing component parameters. It includes business logic of the component.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.BlazorRenderingSecondsBucketBoundaries });

        _batchDuration = _lifeCycleMeter.CreateHistogram(
            "aspnetcore.components.render_diff.duration",
            unit: "s",
            description: "Duration of rendering component tree and producing HTML diff. It includes business logic of the changed components.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = MetricsConstants.BlazorRenderingSecondsBucketBoundaries });

        _batchSize = _lifeCycleMeter.CreateHistogram(
            "aspnetcore.components.render_diff.size",
            unit: "{elements}",
            description: "Number of HTML elements modified during a rendering batch.",
            advice: new InstrumentAdvice<int> { HistogramBucketBoundaries = MetricsConstants.BlazorRenderingDiffLengthBucketBoundaries });
    }

    public void Navigation(string componentType, string route)
    {
        var tags = new TagList();
        AddComponentTypeTag(ref tags, componentType);
        AddRouteTag(ref tags, route);

        _navigationCount.Add(1, tags);
    }

    public async Task CaptureEventDuration(Task task, long startTimestamp, string? componentType, string? methodName, string? attributeName)
    {
        var tags = new TagList();
        AddComponentTypeTag(ref tags, componentType);
        AddMethodNameTag(ref tags, methodName);
        AddAttributeNameTag(ref tags, attributeName);

        try
        {
            await task;
        }
        catch (Exception ex)
        {
            AddErrorTag(ref tags, ex);
        }
        var duration = Stopwatch.GetElapsedTime(startTimestamp);
        _eventDuration.Record(duration.TotalSeconds, tags);
    }

    public void FailEventSync(Exception ex, long startTimestamp, string? componentType, string? methodName, string? attributeName)
    {
        var tags = new TagList();
        AddComponentTypeTag(ref tags, componentType);
        AddMethodNameTag(ref tags, methodName);
        AddAttributeNameTag(ref tags, attributeName);
        AddErrorTag(ref tags, ex);
        
        var duration = Stopwatch.GetElapsedTime(startTimestamp);
        _eventDuration.Record(duration.TotalSeconds, tags);
    }

    public async Task CaptureParametersDuration(Task task, long startTimestamp, string? componentType)
    {
        var tags = new TagList();
        AddComponentTypeTag(ref tags, componentType);

        try
        {
            await task;
        }
        catch(Exception ex)
        {
            AddErrorTag(ref tags, ex);
        }
        var duration = Stopwatch.GetElapsedTime(startTimestamp);
        _parametersDuration.Record(duration.TotalSeconds, tags);
    }

    public void FailParametersSync(Exception ex, long startTimestamp, string? componentType)
    {
        var duration = Stopwatch.GetElapsedTime(startTimestamp);
        var tags = new TagList();
        AddComponentTypeTag(ref tags, componentType);
        AddErrorTag(ref tags, ex);
        
        _parametersDuration.Record(duration.TotalSeconds, tags);
    }

    public async Task CaptureBatchDuration(Task task, long startTimestamp, int diffLength)
    {
        var tags = new TagList();

        try
        {
            await task;
        }
        catch (Exception ex)
        {
            AddErrorTag(ref tags, ex);
        }
        var duration = Stopwatch.GetElapsedTime(startTimestamp);
        _batchDuration.Record(duration.TotalSeconds, tags);
        _batchSize.Record(diffLength, tags);
    }

    public void FailBatchSync(Exception ex, long startTimestamp)
    {
        var duration = Stopwatch.GetElapsedTime(startTimestamp);
        var tags = new TagList();
        AddErrorTag(ref tags, ex);
        
        _batchDuration.Record(duration.TotalSeconds, tags);
    }

    public void Dispose()
    {
        _meter.Dispose();
        _lifeCycleMeter.Dispose();
    }

    private static void AddComponentTypeTag(ref TagList tags, string? componentType)
    {
        if (componentType != null)
        {
            tags.Add("aspnetcore.components.type", componentType);
        }
    }

    private static void AddRouteTag(ref TagList tags, string? route)
    {
        if (route != null)
        {
            tags.Add("aspnetcore.components.route", route);
        }
    }

    private static void AddMethodNameTag(ref TagList tags, string? methodName)
    {
        if (methodName != null)
        {
            tags.Add("code.function.name", methodName);
        }
    }

    private static void AddAttributeNameTag(ref TagList tags, string? attributeName)
    {
        if (attributeName != null)
        {
            tags.Add("aspnetcore.components.attribute.name", attributeName);
        }
    }

    private static void AddErrorTag(ref TagList tags, Exception? exception)
    {
        var errorType = exception?.GetType().FullName;
        if (errorType is not null)
        {
            tags.Add("error.type", errorType);
        }
    }
}

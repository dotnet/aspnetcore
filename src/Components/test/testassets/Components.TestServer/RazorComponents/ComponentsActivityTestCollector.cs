// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using TestContentPackage;

namespace Components.TestServer.RazorComponents;

internal static class ComponentsActivityTestCollector
{
    private static readonly object _lock = new();
    private static readonly Dictionary<string, List<ActivityLinksTestSpan>> _spans = [];
    private static string? _activeTestId;

    public static bool IsActive => Volatile.Read(ref _activeTestId) is not null;

    public static void Start(string testId)
    {
        lock (_lock)
        {
            _spans.TryAdd(testId, []);
            Volatile.Write(ref _activeTestId, testId);
        }
    }

    public static void AddServer(Activity activity)
    {
        var testId = Volatile.Read(ref _activeTestId);
        if (testId is not null)
        {
            Add(testId, [ActivityLinksTestSpan.FromActivity(activity)]);
        }
    }

    public static void Add(string testId, IEnumerable<ActivityLinksTestSpan> spans)
    {
        lock (_lock)
        {
            if (_spans.TryGetValue(testId, out var collected))
            {
                foreach (var span in spans)
                {
                    if (!collected.Any(existing => existing.TraceId == span.TraceId && existing.SpanId == span.SpanId))
                    {
                        collected.Add(span);
                    }
                }
            }
        }
    }

    public static ActivityLinksTestSpan[] Get(string testId)
    {
        lock (_lock)
        {
            return _spans.TryGetValue(testId, out var spans) ? [.. spans] : [];
        }
    }

    public static void Complete(string testId)
    {
        lock (_lock)
        {
            _spans.Remove(testId);
            if (_activeTestId == testId)
            {
                Volatile.Write(ref _activeTestId, null);
            }
        }
    }
}

internal sealed class ComponentsActivityTestSampler : Sampler
{
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
        => ComponentsActivityTestCollector.IsActive
            ? new SamplingResult(SamplingDecision.RecordAndSample)
            : new SamplingResult(SamplingDecision.Drop);
}

internal sealed class ComponentsActivityTestExporter : BaseExporter<Activity>
{
    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (var activity in batch)
        {
            ComponentsActivityTestCollector.AddServer(activity);
        }

        return ExportResult.Success;
    }
}

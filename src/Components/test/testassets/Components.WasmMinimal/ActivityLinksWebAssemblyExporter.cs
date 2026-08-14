// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using OpenTelemetry;
using TestContentPackage;

namespace Components.WasmMinimal;

internal sealed class ActivityLinksWebAssemblyExporter(HttpClient client, string testId) : BaseExporter<Activity>
{
    private readonly object _lock = new();
    private Task _pendingExport = Task.CompletedTask;

    public override ExportResult Export(in Batch<Activity> batch)
    {
        var spans = new List<ActivityLinksTestSpan>();
        foreach (var activity in batch)
        {
            spans.Add(ActivityLinksTestSpan.FromActivity(activity));
        }
        lock (_lock)
        {
            _pendingExport = ExportAfterAsync(_pendingExport, [.. spans]);
        }
        return ExportResult.Success;
    }

    private async Task ExportAfterAsync(Task previousExport, ActivityLinksTestSpan[] spans)
    {
        try
        {
            await previousExport;
            using var response = await client.PostAsJsonAsync($"activity-links/telemetry/{testId}", spans);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
        }
    }
}

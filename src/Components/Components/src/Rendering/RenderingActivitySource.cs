// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components.Rendering;
internal class RenderingActivitySource
{
    internal const string Name = "Microsoft.AspNetCore.Components.Rendering";
    internal const string OnEventName = $"{Name}.OnEvent";

    public ActivitySource ActivitySource { get; } = new ActivitySource(Name);


    public Activity? StartEventActivity(string? componentType, string? methodName, string? attributeName, Activity? linkedActivity)
    {
        IEnumerable<KeyValuePair<string, object?>> tags =
        [
            new("component.type", componentType ?? "unknown"),
            new("component.method", methodName ?? "unknown"),
            new("attribute.name", attributeName ?? "unknown"),
        ];
        IEnumerable<ActivityLink>? links = (linkedActivity is not null) ? [new ActivityLink(linkedActivity.Context)] : null;

        var activity = ActivitySource.CreateActivity(OnEventName, ActivityKind.Server, parentId: null, tags, links);
        if (activity is not null)
        {
            activity.DisplayName = $"{componentType ?? "unknown"}/{methodName ?? "unknown"}/{attributeName ?? "unknown"}";
            activity.Start();
        }
        return activity;
    }
    public static void FailEventActivity(Activity activity, Exception ex)
    {
        if (!activity.IsStopped)
        {
            activity.SetTag("error.type", ex.GetType().FullName);
            activity.SetStatus(ActivityStatusCode.Error);
            activity.Stop();
        }
    }

    public static async Task CaptureEventStopAsync(Task task, Activity activity)
    {
        try
        {
            await task;
            activity.Stop();
        }
        catch (Exception ex)
        {
            FailEventActivity(activity, ex);
        }
    }
}

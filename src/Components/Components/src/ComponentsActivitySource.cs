// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// This is instance scoped per renderer
/// </summary>
internal class ComponentsActivitySource
{
    internal const string Name = "Microsoft.AspNetCore.Components";
    internal const string OnEventName = $"{Name}.OnEvent";
    internal const string OnNavigationName = $"{Name}.OnNavigation";

    public static ActivitySource ActivitySource { get; } = new ActivitySource(Name);

    private Activity? _routeActivity;

    public void StartRouteActivity(string componentType, string route)
    {
        StopRouteActivity();

        IEnumerable<KeyValuePair<string, object?>> tags =
        [
            new("component.type", componentType ?? "unknown"),
            new("route", route ?? "unknown"),
        ];
        var parentActivity = Activity.Current;
        IEnumerable<ActivityLink>? links = parentActivity is not null ? [new ActivityLink(parentActivity.Context)] : null;

        var activity = ActivitySource.CreateActivity(OnEventName, ActivityKind.Server, parentId: null, tags, links);
        if (activity is not null)
        {
            activity.DisplayName = $"NAVIGATE {route ?? "unknown"} -> {componentType ?? "unknown"}";
            activity.Start();
            _routeActivity = activity;
        }
    }

    public void StopRouteActivity()
    {
        if (_routeActivity != null)
        {
            _routeActivity.Stop();
            _routeActivity = null;
            return;
        }
    }

    public Activity? StartEventActivity(string? componentType, string? methodName, string? attributeName)
    {
        IEnumerable<KeyValuePair<string, object?>> tags =
        [
            new("component.type", componentType ?? "unknown"),
            new("component.method", methodName ?? "unknown"),
            new("attribute.name", attributeName ?? "unknown"),
        ];
        List<ActivityLink>? links = new List<ActivityLink>();
        var parentActivity = Activity.Current;
        if (parentActivity is not null)
        {
            links.Add(new ActivityLink(parentActivity.Context));
        }
        if (_routeActivity is not null)
        {
            links.Add(new ActivityLink(_routeActivity.Context));
        }

        var activity = ActivitySource.CreateActivity(OnEventName, ActivityKind.Server, parentId: null, tags, links);
        if (activity is not null)
        {
            activity.DisplayName = $"EVENT {attributeName ?? "unknown"} -> {componentType ?? "unknown"}.{methodName ?? "unknown"}";
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

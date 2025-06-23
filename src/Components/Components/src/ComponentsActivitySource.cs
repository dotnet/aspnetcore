// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// This is instance scoped per renderer
/// </summary>
internal class ComponentsActivitySource
{
    internal const string Name = "Microsoft.AspNetCore.Components";
    internal const string OnRouteName = $"{Name}.RouteChange";
    internal const string OnEventName = $"{Name}.HandleEvent";

    private static ActivitySource ActivitySource { get; } = new ActivitySource(Name);
    private ComponentsActivityLinkStore? _componentsActivityLinkStore;

    public void Init(ComponentsActivityLinkStore store)
    {
        _componentsActivityLinkStore = store;
    }

    public ComponentsActivityHandle StartRouteActivity(string componentType, string route)
    {
        var activity = ActivitySource.CreateActivity(OnRouteName, ActivityKind.Internal, parentId: null, null, null);
        if (activity is not null)
        {
            var httpActivity = Activity.Current;
            activity.DisplayName = $"Route {route ?? "[unknown path]"} -> {componentType ?? "[unknown component]"}";
            Activity.Current = null; // do not inherit the parent activity
            activity.Start();

            if (activity.IsAllDataRequested)
            {
                if (componentType != null)
                {
                    activity.SetTag("aspnetcore.components.type", componentType);
                }
                if (route != null)
                {
                    activity.SetTag("aspnetcore.components.route", route);

                    // store self link
                    _componentsActivityLinkStore!.SetActivityContext(ComponentsActivityLinkStore.Route, activity.Context,
                        new KeyValuePair<string, object?>("aspnetcore.components.route", route));
                }
            }

            return new ComponentsActivityHandle { Activity = activity, Previous = httpActivity };
        }
        return default;
    }

    public void StopRouteActivity(ComponentsActivityHandle activityHandle, Exception? ex)
    {
        StopComponentActivity(ComponentsActivityLinkStore.Route, activityHandle, ex);
    }

    public static ComponentsActivityHandle StartEventActivity(string? componentType, string? methodName, string? attributeName)
    {
        var activity = ActivitySource.CreateActivity(OnEventName, ActivityKind.Internal, parentId: null, null, null);

        if (activity is not null)
        {
            var previousActivity = Activity.Current;
            activity.DisplayName = $"Event {attributeName ?? "[unknown attribute]"} -> {componentType ?? "[unknown component]"}.{methodName ?? "[unknown method]"}";
            Activity.Current = null; // do not inherit the parent activity
            activity.Start();

            if (activity.IsAllDataRequested)
            {
                if (componentType != null)
                {
                    activity.SetTag("aspnetcore.components.type", componentType);
                }
                if (methodName != null)
                {
                    activity.SetTag("aspnetcore.components.method", methodName);
                }
                if (attributeName != null)
                {
                    activity.SetTag("aspnetcore.components.attribute.name", attributeName);
                }
            }

            return new ComponentsActivityHandle { Activity = activity, Previous = previousActivity };
        }
        return default;
    }

    public void StopEventActivity(ComponentsActivityHandle activityHandle, Exception? ex)
    {
        StopComponentActivity(ComponentsActivityLinkStore.Event, activityHandle, ex);
    }

    public async Task CaptureEventStopAsync(Task task, ComponentsActivityHandle activityHandle)
    {
        try
        {
            await task;
            StopEventActivity(activityHandle, null);
        }
        catch (Exception ex)
        {
            StopEventActivity(activityHandle, ex);
        }
    }

    private void StopComponentActivity(string category, ComponentsActivityHandle activityHandle, Exception? ex)
    {
        var activity = activityHandle.Activity;
        if (activity != null && !activity.IsStopped)
        {
            if (ex != null)
            {
                activity.SetTag("error.type", ex.GetType().FullName);
                activity.SetStatus(ActivityStatusCode.Error);
            }
            if (activity.IsAllDataRequested)
            {
                _componentsActivityLinkStore!.AddActivityContexts(category, activity);
            }
            activity.Stop();

            if (Activity.Current == null && activityHandle.Previous != null && !activityHandle.Previous.IsStopped)
            {
                Activity.Current = activityHandle.Previous;
            }
        }
    }
}

/// <summary>
/// Named tuple for restoring the previous activity after stopping the current one.
/// </summary>
internal struct ComponentsActivityHandle
{
    public Activity? Previous;
    public Activity? Activity;
}

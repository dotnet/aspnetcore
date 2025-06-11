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
    internal const string OnRouteName = $"{Name}.RouteChange";
    internal const string OnEventName = $"{Name}.HandleEvent";

    private readonly IComponentsActivityLinkStore _activityLinkStore;

    private ActivitySource ActivitySource { get; } = new ActivitySource(Name);

    public ComponentsActivitySource(IComponentsActivityLinkStore activityLinkStore)
    {
        _activityLinkStore = activityLinkStore ?? throw new ArgumentNullException(nameof(activityLinkStore));
    }

    public ComponentsActivityHandle StartRouteActivity(string componentType, string route)
    {
        var activity = ActivitySource.CreateActivity(OnRouteName, ActivityKind.Internal, parentId: null, null, null);
        if (activity is not null)
        {
            var previousActivity = Activity.Current;
            if (activity.IsAllDataRequested)
            {
                if (componentType != null)
                {
                    activity.SetTag("aspnetcore.components.type", componentType);
                }
                if (route != null)
                {
                    activity.SetTag("aspnetcore.components.route", route);
                }
                if (previousActivity != null)
                {
                    activity.AddLink(new ActivityLink(previousActivity.Context));
                }

                // store the link
                _activityLinkStore.SetActivityContext(ComponentsActivityCategory.Route, activity.Context,
                    new KeyValuePair<string, object?>("aspnetcore.components.route", route));
            }

            activity.DisplayName = $"Route {route ?? "[unknown path]"} -> {componentType ?? "[unknown component]"}";
            Activity.Current = null; // do not inherit the parent activity
            activity.Start();
            return new ComponentsActivityHandle { Activity = activity, Previous = previousActivity };
        }
        return default;
    }

    public void StopRouteActivity(ComponentsActivityHandle activityHandle, Exception? ex)
    {
        StopComponentActivity(ComponentsActivityCategory.Route, activityHandle, ex);
    }

    public ComponentsActivityHandle StartEventActivity(string? componentType, string? methodName, string? attributeName)
    {
        var activity = ActivitySource.CreateActivity(OnEventName, ActivityKind.Internal, parentId: null, null, null);
        if (activity is not null)
        {
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

            activity.DisplayName = $"Event {attributeName ?? "[unknown attribute]"} -> {componentType ?? "[unknown component]"}.{methodName ?? "[unknown method]"}";
            var previousActivity = Activity.Current;
            Activity.Current = null; // do not inherit the parent activity
            activity.Start();
            return new ComponentsActivityHandle { Activity = activity, Previous = previousActivity };
        }
        return default;
    }

    public void StopEventActivity(ComponentsActivityHandle activityHandle, Exception? ex)
    {
        StopComponentActivity(ComponentsActivityCategory.Event, activityHandle, ex);
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

    private void StopComponentActivity(int category, ComponentsActivityHandle activityHandle, Exception? ex)
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
                _activityLinkStore.AddActivityContexts(category, activity);
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

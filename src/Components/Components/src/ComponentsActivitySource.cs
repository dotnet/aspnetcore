// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Named tuple for restoring the previous activity after stopping the current one.
/// </summary>
internal struct ComponentsActivityHandle
{
    public Activity? Previous;
    public Activity? Activity;
}

/// <summary>
/// This is instance scoped per renderer
/// </summary>
internal class ComponentsActivitySource
{
    internal const string Name = "Microsoft.AspNetCore.Components";
    internal const string OnRouteName = $"{Name}.RouteChange";
    internal const string OnEventName = $"{Name}.HandleEvent";

    internal ActivityContext _httpActivityContext;
    internal ActivityContext _routeContext;
    internal ActivityContext _circuitActivityContext;
    internal string? _circuitId;

    private ActivitySource ActivitySource { get; } = new ActivitySource(Name);

    public ComponentsActivityHandle StartRouteActivity(string componentType, string route)
    {
        var activity = ActivitySource.CreateActivity(OnRouteName, ActivityKind.Internal, parentId: null, null, null);
        if (activity is not null)
        {
            var previousActivity = Activity.Current;
            if (activity.IsAllDataRequested)
            {
                if (_circuitId != null)
                {
                    activity.SetTag("aspnetcore.components.circuit.id", _circuitId);
                }
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
            }

            activity.DisplayName = $"Route {route ?? "[unknown path]"} -> {componentType ?? "[unknown component]"}";
            Activity.Current = null; // do not inherit the parent activity
            activity.Start();
            _routeContext = activity.Context;
            return new ComponentsActivityHandle { Activity = activity, Previous = previousActivity };
        }
        return default;
    }

    public ComponentsActivityHandle StartEventActivity(string? componentType, string? methodName, string? attributeName)
    {
        var activity = ActivitySource.CreateActivity(OnEventName, ActivityKind.Internal, parentId: null, null, null);
        if (activity is not null)
        {
            if (activity.IsAllDataRequested)
            {
                if (_circuitId != null)
                {
                    activity.SetTag("aspnetcore.components.circuit.id", _circuitId);
                }
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

    public void StopComponentActivity(ComponentsActivityHandle activityHandle, Exception? ex)
    {
        var activity = activityHandle.Activity;
        if (activity != null && !activity.IsStopped)
        {
            if (activity.IsAllDataRequested)
            {
                if (_circuitId != null)
                {
                    activity.SetTag("aspnetcore.components.circuit.id", _circuitId);
                }
                if (_httpActivityContext != default)
                {
                    activity.AddLink(new ActivityLink(_httpActivityContext));
                }
                if (_circuitActivityContext != default)
                {
                    activity.AddLink(new ActivityLink(_circuitActivityContext));
                }
                if (_routeContext != default && activity.Context != _routeContext)
                {
                    activity.AddLink(new ActivityLink(_routeContext));
                }
            }
            if (ex != null)
            {
                activity.SetTag("error.type", ex.GetType().FullName);
                activity.SetStatus(ActivityStatusCode.Error);
            }
            activity.Stop();

            if (Activity.Current == null && activityHandle.Previous != null && !activityHandle.Previous.IsStopped)
            {
                Activity.Current = activityHandle.Previous;
            }
        }
    }

    public async Task CaptureEventStopAsync(Task task, ComponentsActivityHandle activityHandle)
    {
        try
        {
            await task;
            StopComponentActivity(activityHandle, null);
        }
        catch (Exception ex)
        {
            StopComponentActivity(activityHandle, ex);
        }
    }
}

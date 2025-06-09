// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components;

internal struct ComponentsActivityWrapper
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

    internal ActivityContext _httpContext;
    internal ActivityContext _circuitContext;
    internal string? _circuitId;

    private ActivityContext _routeContext;

    private ActivitySource ActivitySource { get; } = new ActivitySource(Name);

    public ComponentsActivityWrapper StartRouteActivity(string componentType, string route)
    {
        var activity = ActivitySource.CreateActivity(OnRouteName, ActivityKind.Internal, parentId: null, null, null);
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
                if (route != null)
                {
                    activity.SetTag("aspnetcore.components.route", route);
                }
                if (_httpContext != default)
                {
                    activity.AddLink(new ActivityLink(_httpContext));
                }
                if (_circuitContext != default)
                {
                    activity.AddLink(new ActivityLink(_circuitContext));
                }
            }

            activity.DisplayName = $"Route {route ?? "[unknown path]"} -> {componentType ?? "[unknown component]"}";
            var previousActivity = Activity.Current;
            Activity.Current = null; // do not inherit the parent activity
            activity.Start();
            _routeContext = activity.Context;
            return new ComponentsActivityWrapper { Activity = activity, Previous = previousActivity };
        }
        return default;
    }

    public ComponentsActivityWrapper StartEventActivity(string? componentType, string? methodName, string? attributeName)
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
                if (_httpContext != default)
                {
                    activity.AddLink(new ActivityLink(_httpContext));
                }
                if (_circuitContext != default)
                {
                    activity.AddLink(new ActivityLink(_circuitContext));
                }
                if (_routeContext != default)
                {
                    activity.AddLink(new ActivityLink(_routeContext));
                }
            }

            activity.DisplayName = $"Event {attributeName ?? "[unknown attribute]"} -> {componentType ?? "[unknown component]"}.{methodName ?? "[unknown method]"}";
            var previousActivity = Activity.Current;
            Activity.Current = null; // do not inherit the parent activity
            activity.Start();
            return new ComponentsActivityWrapper { Activity = activity, Previous = previousActivity };
        }
        return default;
    }

    public static void StopComponentActivity(ComponentsActivityWrapper wrapper, Exception? ex)
    {
        if (wrapper.Activity != null && !wrapper.Activity.IsStopped)
        {
            if (ex != null)
            {
                wrapper.Activity.SetTag("error.type", ex.GetType().FullName);
                wrapper.Activity.SetStatus(ActivityStatusCode.Error);
            }
            wrapper.Activity.Stop();

            if (Activity.Current == null && wrapper.Previous != null && !wrapper.Previous.IsStopped)
            {
                Activity.Current = wrapper.Previous;
            }
        }
    }

    public static async Task CaptureEventStopAsync(Task task, ComponentsActivityWrapper wrapper)
    {
        try
        {
            await task;
            StopComponentActivity(wrapper, null);
        }
        catch (Exception ex)
        {
            StopComponentActivity(wrapper, ex);
        }
    }
}

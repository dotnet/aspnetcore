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
    internal const string OnCircuitName = $"{Name}.CircuitStart";
    internal const string OnRouteName = $"{Name}.RouteChange";
    internal const string OnEventName = $"{Name}.Event";

    private ActivityContext _httpContext;
    private ActivityContext _circuitContext;
    private string? _circuitId;
    private ActivityContext _routeContext;

    private ActivitySource ActivitySource { get; } = new ActivitySource(Name);

    public static ActivityContext CaptureHttpContext()
    {
        var parentActivity = Activity.Current;
        if (parentActivity is not null && parentActivity.OperationName == "Microsoft.AspNetCore.Hosting.HttpRequestIn" && parentActivity.Recorded)
        {
            return parentActivity.Context;
        }
        return default;
    }

    public Activity? StartCircuitActivity(string circuitId, ActivityContext httpContext)
    {
        _circuitId = circuitId;

        var activity = ActivitySource.CreateActivity(OnCircuitName, ActivityKind.Internal, parentId: null, null, null);
        if (activity is not null)
        {
            if (activity.IsAllDataRequested)
            {
                if (_circuitId != null)
                {
                    activity.SetTag("circuit.id", _circuitId);
                }
                if (httpContext != default)
                {
                    activity.AddLink(new ActivityLink(httpContext));
                }
            }
            activity.DisplayName = $"Circuit {circuitId ?? ""}";
            activity.Start();
            _circuitContext = activity.Context;
        }
        return activity;
    }

    public void FailCircuitActivity(Activity? activity, Exception ex)
    {
        _circuitContext = default;
        if (activity != null && !activity.IsStopped)
        {
            activity.SetTag("error.type", ex.GetType().FullName);
            activity.SetStatus(ActivityStatusCode.Error);
            activity.Stop();
        }
    }

    public Activity? StartRouteActivity(string componentType, string route)
    {
        if (_httpContext == default)
        {
            _httpContext = CaptureHttpContext();
        }

        var activity = ActivitySource.CreateActivity(OnRouteName, ActivityKind.Internal, parentId: null, null, null);
        if (activity is not null)
        {
            if (activity.IsAllDataRequested)
            {
                if (_circuitId != null)
                {
                    activity.SetTag("circuit.id", _circuitId);
                }
                if (componentType != null)
                {
                    activity.SetTag("component.type", componentType);
                }
                if (route != null)
                {
                    activity.SetTag("route", route);
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
            activity.Start();
            _routeContext = activity.Context;
        }
        return activity;
    }

    public Activity? StartEventActivity(string? componentType, string? methodName, string? attributeName)
    {
        var activity = ActivitySource.CreateActivity(OnEventName, ActivityKind.Internal, parentId: null, null, null);
        if (activity is not null)
        {
            if (activity.IsAllDataRequested)
            {
                if (_circuitId != null)
                {
                    activity.SetTag("circuit.id", _circuitId);
                }
                if (componentType != null)
                {
                    activity.SetTag("component.type", componentType);
                }
                if (methodName != null)
                {
                    activity.SetTag("component.method", methodName);
                }
                if (attributeName != null)
                {
                    activity.SetTag("attribute.name", attributeName);
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
            activity.Start();
        }
        return activity;
    }

    public static void FailEventActivity(Activity? activity, Exception ex)
    {
        if (activity != null && !activity.IsStopped)
        {
            activity.SetTag("error.type", ex.GetType().FullName);
            activity.SetStatus(ActivityStatusCode.Error);
            activity.Stop();
        }
    }

    public static async Task CaptureEventStopAsync(Task task, Activity? activity)
    {
        try
        {
            await task;
            activity?.Stop();
        }
        catch (Exception ex)
        {
            FailEventActivity(activity, ex);
        }
    }
}

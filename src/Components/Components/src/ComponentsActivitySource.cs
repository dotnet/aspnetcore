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
    internal const string OnRouteName = $"{Name}.OnRoute";

    private ActivityContext _httpContext;
    private ActivityContext _circuitContext;
    private string? _circuitId;
    private ActivityContext _routeContext;

    private ActivitySource ActivitySource { get; } = new ActivitySource(Name);

    public static ActivityContext CaptureHttpContext()
    {
        var parentActivity = Activity.Current;
        if (parentActivity is not null && parentActivity.OperationName == "Microsoft.AspNetCore.Hosting.HttpRequestIn")
        {
            return parentActivity.Context;
        }
        return default;
    }

    public Activity? StartCircuitActivity(string circuitId, ActivityContext httpContext)
    {
        _circuitId = circuitId;
        IEnumerable<KeyValuePair<string, object?>> tags =
        [
            new("circuit.id", _circuitId ?? "unknown"),
        ];

        var links = new List<ActivityLink>();
        if (httpContext != default)
        {
            _httpContext = httpContext;
            links.Add(new ActivityLink(httpContext));
        }

        var activity = ActivitySource.CreateActivity(OnRouteName, ActivityKind.Server, parentId:null, tags, links);
        if (activity is not null)
        {
            activity.DisplayName = $"CIRCUIT {circuitId ?? "unknown"}";
            activity.Start();
            _circuitContext = activity.Context;

            Console.WriteLine($"StartCircuitActivity: {circuitId}");
            Console.WriteLine($"circuitContext: {_circuitContext.TraceId} {_circuitContext.SpanId} {_circuitContext.TraceState}");
            Console.WriteLine($"httpContext: {httpContext.TraceId} {httpContext.SpanId} {httpContext.TraceState}");
        }
        return activity;
    }

    public void FailCircuitActivity(Activity activity, Exception ex)
    {
        _circuitContext = default;
        if (!activity.IsStopped)
        {
            activity.SetTag("error.type", ex.GetType().FullName);
            activity.SetStatus(ActivityStatusCode.Error);
            activity.Stop();
        }
    }

    public Activity? StartRouteActivity(string componentType, string route)
    {
        IEnumerable<KeyValuePair<string, object?>> tags =
        [
            new("circuit.id", _circuitId ?? "unknown"),
            new("component.type", componentType ?? "unknown"),
            new("route", route ?? "unknown"),
        ];
        var links = new List<ActivityLink>();
        if (_httpContext == default)
        {
            _httpContext = CaptureHttpContext();
        }
        if (_httpContext != default)
        {
            links.Add(new ActivityLink(_httpContext));
        }
        if (_circuitContext != default)
        {
            links.Add(new ActivityLink(_circuitContext));
        }

        var activity = ActivitySource.CreateActivity(OnRouteName, ActivityKind.Server, parentId: null, tags, links);
        if (activity is not null)
        {
            _routeContext = activity.Context;
            activity.DisplayName = $"ROUTE {route ?? "unknown"} -> {componentType ?? "unknown"}";
            activity.Start();
        }
        return activity;
    }

    public void StopRouteActivity(Activity activity)
    {
        _routeContext = default;
        if (!activity.IsStopped)
        {
            activity.Stop();
        }
    }

    public Activity? StartEventActivity(string? componentType, string? methodName, string? attributeName)
    {
        IEnumerable<KeyValuePair<string, object?>> tags =
        [
            new("circuit.id", _circuitId ?? "unknown"),
            new("component.type", componentType ?? "unknown"),
            new("component.method", methodName ?? "unknown"),
            new("attribute.name", attributeName ?? "unknown"),
        ];
        var links = new List<ActivityLink>();
        if (_httpContext != default)
        {
            links.Add(new ActivityLink(_httpContext));
        }
        if (_circuitContext != default)
        {
            links.Add(new ActivityLink(_circuitContext));
        }
        if (_routeContext != default)
        {
            links.Add(new ActivityLink(_routeContext));
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

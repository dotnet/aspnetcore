// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.RenderTree;

/// <summary>
/// Named tuple for restoring the previous activity after stopping the current one.
/// </summary>
internal struct CircuitActivityHandle
{
    public Activity? Previous;
    public Activity? Activity;
}

internal class CircuitActivitySource
{
    internal const string Name = "Microsoft.AspNetCore.Components.Server.Circuits";
    internal const string OnCircuitName = $"{Name}.CircuitStart";

    private ActivitySource ActivitySource { get; } = new ActivitySource(Name);

    public CircuitActivityHandle StartCircuitActivity(string circuitId, ActivityContext httpActivityContext, Renderer? renderer)
    {
        var activity = ActivitySource.CreateActivity(OnCircuitName, ActivityKind.Internal, parentId:null, null, null);
        if (activity is not null)
        {
            var previousActivity = Activity.Current;

            if (activity.IsAllDataRequested)
            {
                if (circuitId != null)
                {
                    activity.SetTag("aspnetcore.components.circuit.id", circuitId);
                }
                if (httpActivityContext != default)
                {
                    activity.AddLink(new ActivityLink(httpActivityContext));
                }
                if (previousActivity != null)
                {
                    activity.AddLink(new ActivityLink(previousActivity.Context));
                }
            }
            activity.DisplayName = $"Circuit {circuitId ?? ""}";
            Activity.Current = null; // do not inherit the parent activity
            activity.Start();

            if (renderer != null)
            {
                var routeActivityContext = LinkActivityContexts(renderer, httpActivityContext, activity.Context, circuitId);
                if (routeActivityContext != default)
                {
                    activity.AddLink(new ActivityLink(routeActivityContext));
                }
            }
            return new CircuitActivityHandle { Previous = previousActivity, Activity = activity };
        }
        return default;
    }

    public static void StopCircuitActivity(CircuitActivityHandle activityHandle, Exception? ex)
    {
        if (activityHandle.Activity != null && !activityHandle.Activity.IsStopped)
        {
            if (ex != null)
            {
                activityHandle.Activity.SetTag("error.type", ex.GetType().FullName);
                activityHandle.Activity.SetStatus(ActivityStatusCode.Error);
            }
            activityHandle.Activity.Stop();

            if (Activity.Current == null && activityHandle.Previous != null && !activityHandle.Previous.IsStopped)
            {
                Activity.Current = activityHandle.Previous;
            }
        }
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "LinkActivityContexts")]
    static extern ActivityContext LinkActivityContexts(Renderer type, ActivityContext httpContext, ActivityContext circuitContext, string? circuitId);
}

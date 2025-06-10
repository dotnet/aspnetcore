// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.RenderTree;

internal struct CircuitActivityWrapper
{
    public Activity? Previous;
    public Activity? Activity;
}

internal class CircuitActivitySource
{
    internal const string Name = "Microsoft.AspNetCore.Components";
    internal const string OnCircuitName = $"{Name}.CircuitStart";

    private ActivitySource ActivitySource { get; } = new ActivitySource(Name);

    public CircuitActivityWrapper StartCircuitActivity(string circuitId, ActivityContext httpActivityContext, Renderer? renderer)
    {
        var activity = ActivitySource.CreateActivity(OnCircuitName, ActivityKind.Internal, parentId:null, null, null);
        if (activity is not null)
        {
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
            }
            activity.DisplayName = $"Circuit {circuitId ?? ""}";
            var previousActivity = Activity.Current;
            Activity.Current = null; // do not inherit the parent activity
            activity.Start();

            if (renderer != null)
            {
                SetCircuitActivityContext(renderer, httpActivityContext, activity.Context, circuitId);
            }
            return new CircuitActivityWrapper { Previous = previousActivity, Activity = activity };
        }
        return default;
    }

    public static void StopCircuitActivity(CircuitActivityWrapper wrapper, Exception? ex)
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

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "SetCircuitActivityContext")]
    static extern void SetCircuitActivityContext(Renderer type, ActivityContext httpContext, ActivityContext circuitContext, string circuitId);
}

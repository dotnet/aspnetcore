// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Routing;

internal class CircuitActivitySource
{
    internal const string Name = "Microsoft.AspNetCore.Components.Server.Circuits";
    internal const string OnCircuitName = $"{Name}.CircuitStart";

    private readonly IComponentsActivityLinkStore _activityLinkStore;

    private ActivitySource ActivitySource { get; } = new ActivitySource(Name);

    public CircuitActivitySource(IComponentsActivityLinkStore activityLinkStore)
    {
        _activityLinkStore = activityLinkStore ?? throw new ArgumentNullException(nameof(activityLinkStore));
    }

    public CircuitActivityHandle StartCircuitActivity(string circuitId, ActivityContext httpActivityContext, Renderer? renderer)
    {
        var activity = ActivitySource.CreateActivity(OnCircuitName, ActivityKind.Internal, parentId:null, null, null);
        if (activity is not null)
        {
            var signalRActivity = Activity.Current;

            if (activity.IsAllDataRequested)
            {
                if (circuitId != null)
                {
                    activity.SetTag("aspnetcore.components.circuit.id", circuitId);

                    // store the circuit link
                    _activityLinkStore.SetActivityContext(ComponentsActivityCategory.Route, activity.Context,
                        new KeyValuePair<string, object?>("aspnetcore.components.circuit.id", circuitId));
                }
                if (httpActivityContext != default)
                {
                    activity.AddLink(new ActivityLink(httpActivityContext));

                    // store the http link
                    _activityLinkStore.SetActivityContext(ComponentsActivityCategory.Http, httpActivityContext, null);
                }
                if (signalRActivity != null)
                {
                    activity.AddLink(new ActivityLink(signalRActivity.Context));

                    // store the SignalR link
                    _activityLinkStore.SetActivityContext(ComponentsActivityCategory.SignalR, signalRActivity.Context, null);
                }
            }
            activity.DisplayName = $"Circuit {circuitId ?? ""}";
            Activity.Current = null; // do not inherit the parent activity
            activity.Start();
            return new CircuitActivityHandle { Previous = signalRActivity, Activity = activity };
        }
        return default;
    }

    public void StopCircuitActivity(CircuitActivityHandle activityHandle, Exception? ex)
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
                // ComponentsActivityCategory.Circuit = 5;
                _activityLinkStore.AddActivityContexts(5, activity);
            }
            activityHandle.Activity.Stop();

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
internal struct CircuitActivityHandle
{
    public Activity? Previous;
    public Activity? Activity;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server.Circuits;

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Infrastructure.Server;

internal class CircuitActivitySource
{
    internal const string Name = "Microsoft.AspNetCore.Components.Server.Circuits";
    internal const string OnCircuitName = $"{Name}.CircuitStart";

    private ComponentsActivityLinkStore? _activityLinkStore;

    private static ActivitySource ActivitySource { get; } = new ActivitySource(Name);

    public void Init(ComponentsActivityLinkStore store)
    {
        _activityLinkStore = store;
    }

    public CircuitActivityHandle StartCircuitActivity(string circuitId, ActivityContext httpActivityContext)
    {
        var activity = ActivitySource.CreateActivity(OnCircuitName, ActivityKind.Internal, parentId: null, null, null);
        if (activity is not null)
        {
            var signalRActivity = Activity.Current;
            activity.DisplayName = $"Circuit {circuitId ?? ""}";
            Activity.Current = null; // do not inherit the parent activity
            activity.Start();

            if (activity.IsAllDataRequested)
            {
                if (circuitId != null)
                {
                    activity.SetTag("aspnetcore.components.circuit.id", circuitId);

                    // store self link
                    _activityLinkStore.SetActivityContext(ComponentsActivityLinkStore.Circuit, activity.Context,
                        new KeyValuePair<string, object?>("aspnetcore.components.circuit.id", circuitId));
                }
                if (httpActivityContext != default)
                {
                    // store the http link
                    _activityLinkStore.SetActivityContext(ComponentsActivityLinkStore.Http, httpActivityContext, null);
                }
                if (signalRActivity != null && signalRActivity.Source.Name == "Microsoft.AspNetCore.SignalR.Server")
                {
                    // store the SignalR link
                    _activityLinkStore.SetActivityContext(ComponentsActivityLinkStore.SignalR, signalRActivity.Context, null);
                }
            }
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
                _activityLinkStore.AddActivityContexts(ComponentsActivityLinkStore.Circuit, activity);
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

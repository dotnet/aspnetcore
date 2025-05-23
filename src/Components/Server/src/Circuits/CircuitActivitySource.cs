// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>
/// Activity source for circuit-related activities.
/// </summary>
public class CircuitActivitySource
{
    internal const string Name = "Microsoft.AspNetCore.Components.Server";
    internal const string OnCircuitName = $"{Name}.CircuitStart";
    
    private ActivitySource ActivitySource { get; } = new ActivitySource(Name);

    /// <summary>
    /// Creates and starts a new activity for circuit initialization.
    /// </summary>
    /// <param name="circuitId">The ID of the circuit being initialized.</param>
    /// <param name="httpContext">The HTTP context associated with the request that created the circuit.</param>
    /// <returns>The created activity.</returns>
    public Activity? StartCircuitActivity(string circuitId, ActivityContext httpContext)
    {
        var activity = ActivitySource.CreateActivity(OnCircuitName, ActivityKind.Internal, parentId: null, null, null);
        if (activity is not null)
        {
            if (activity.IsAllDataRequested)
            {
                if (circuitId != null)
                {
                    activity.SetTag("aspnetcore.components.circuit.id", circuitId);
                }
                if (httpContext != default)
                {
                    activity.AddLink(new ActivityLink(httpContext));
                }
            }
            activity.DisplayName = $"Circuit {circuitId ?? ""}";
            activity.Start();
        }
        return activity;
    }

    /// <summary>
    /// Stops a circuit activity that was previously started.
    /// </summary>
    /// <param name="activity">The activity to stop.</param>
    public void StopCircuitActivity(Activity? activity)
    {
        if (activity != null && !activity.IsStopped)
        {
            activity.Stop();
        }
    }

    /// <summary>
    /// Marks a circuit activity as failed and stops it.
    /// </summary>
    /// <param name="activity">The activity to mark as failed.</param>
    /// <param name="ex">The exception that caused the failure.</param>
    public void FailCircuitActivity(Activity? activity, Exception ex)
    {
        if (activity != null && !activity.IsStopped)
        {
            activity.SetTag("error.type", ex.GetType().FullName);
            activity.SetStatus(ActivityStatusCode.Error);
            activity.Stop();
        }
    }

    /// <summary>
    /// Captures the current HTTP context activity.
    /// </summary>
    /// <returns>The captured HTTP context activity.</returns>
    public static ActivityContext CaptureHttpContext()
    {
        var parentActivity = Activity.Current;
        if (parentActivity is not null && parentActivity.OperationName == "Microsoft.AspNetCore.Hosting.HttpRequestIn" && parentActivity.Recorded)
        {
            return parentActivity.Context;
        }
        return default;
    }
}
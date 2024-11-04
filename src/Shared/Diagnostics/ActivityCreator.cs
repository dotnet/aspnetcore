// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Shared;

internal static class ActivityCreator
{
    /// <summary>
    /// Create an activity with details received from a remote source.
    /// </summary>
    public static Activity? CreateFromRemote(
        ActivitySource activitySource,
        DistributedContextPropagator propagator,
        object distributedContextCarrier,
        DistributedContextPropagator.PropagatorGetterCallback propagatorGetter,
        string activityName,
        ActivityKind kind,
        IEnumerable<KeyValuePair<string, object?>>? tags,
        IEnumerable<ActivityLink>? links,
        bool diagnosticsOrLoggingEnabled)
    {
        Activity? activity = null;
        string? requestId = null;
        string? traceState = null;

        if (activitySource.HasListeners())
        {
            propagator.ExtractTraceIdAndState(
                distributedContextCarrier,
                propagatorGetter,
                out requestId,
                out traceState);

            if (ActivityContext.TryParse(requestId, traceState, isRemote: true, out ActivityContext context))
            {
                // The requestId used the W3C ID format. Unfortunately, the ActivitySource.CreateActivity overload that
                // takes a string parentId never sets HasRemoteParent to true. We work around that by calling the
                // ActivityContext overload instead which sets HasRemoteParent to parentContext.IsRemote.
                // https://github.com/dotnet/aspnetcore/pull/41568#discussion_r868733305
                activity = activitySource.CreateActivity(activityName, kind, context, tags: tags, links: links);
            }
            else
            {
                // Pass in the ID we got from the headers if there was one.
                activity = activitySource.CreateActivity(activityName, kind, string.IsNullOrEmpty(requestId) ? null : requestId, tags: tags, links: links);
            }
        }

        if (activity is null)
        {
            // CreateActivity didn't create an Activity (this is an optimization for the
            // case when there are no listeners). Let's create it here if needed.
            if (diagnosticsOrLoggingEnabled)
            {
                // Note that there is a very small chance that propagator has already been called.
                // Requires that the activity source had listened, but it didn't create an activity.
                // Can only happen if there is a race between HasListeners and CreateActivity calls,
                // and someone removing the listener.
                //
                // The only negative of calling the propagator twice is a small performance hit.
                // It's small and unlikely so it's not worth trying to optimize.
                propagator.ExtractTraceIdAndState(
                    distributedContextCarrier,
                    propagatorGetter,
                    out requestId,
                    out traceState);

                activity = new Activity(activityName);
                if (!string.IsNullOrEmpty(requestId))
                {
                    activity.SetParentId(requestId);
                }
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        activity.AddTag(tag.Key, tag.Value);
                    }
                }
                if (links != null)
                {
                    foreach (var link in links)
                    {
                        activity.AddLink(link);
                    }
                }
            }
            else
            {
                return null;
            }
        }

        // The trace id was successfully extracted, so we can set the trace state
        // https://www.w3.org/TR/trace-context/#tracestate-header
        if (!string.IsNullOrEmpty(requestId))
        {
            if (!string.IsNullOrEmpty(traceState))
            {
                activity.TraceStateString = traceState;
            }
        }

        // Baggage can be used regardless of whether a distributed trace id was present on the inbound request.
        // https://www.w3.org/TR/baggage/#abstract
        var baggage = propagator.ExtractBaggage(distributedContextCarrier, propagatorGetter);

        // AddBaggage adds items at the beginning  of the list, so we need to add them in reverse to keep the same order as the client
        // By contract, the propagator has already reversed the order of items so we need not reverse it again
        // Order could be important if baggage has two items with the same key (that is allowed by the contract)
        if (baggage is not null)
        {
            foreach (var baggageItem in baggage)
            {
                activity.AddBaggage(baggageItem.Key, baggageItem.Value);
            }
        }

        return activity;
    }
}

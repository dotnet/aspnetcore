// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.RenderTree;

// this internal helper class is used in both Components and Components.Server projects as a different type
// the namespace is different to avoid conflicts with internalVisibleTo in unit tests
#if COMPONENTS 
namespace Microsoft.AspNetCore.Components.Infrastructure;
#else
namespace Microsoft.AspNetCore.Components.Infrastructure.Server;
#endif

using CategoryLink = Tuple<ActivityContext, KeyValuePair<string, object?>?>;

/// <summary>
/// Helper for storing links between diagnostic activities in Blazor components.
/// </summary>
internal class ComponentsActivityLinkStore
{
    public const string Http = "Http";
    public const string SignalR = "SignalR";
    public const string Route = "Route";
    public const string Circuit = "Circuit";
    public const string Event = "Event";

    private readonly Dictionary<string, CategoryLink> _store;

    public ComponentsActivityLinkStore(Renderer? instance)
    {
        _store = instance == null
            ? new Dictionary<string, CategoryLink>(StringComparer.OrdinalIgnoreCase)
            : (Dictionary<string, CategoryLink>)GetActivityLinksStore(instance);
        Debug.Assert(_store != null, "Activity links store should not be null.");
    }

    public void SetActivityContext(string category, ActivityContext activityLink, KeyValuePair<string, object?>? tag)
    {
        _store[category] = new CategoryLink(activityLink, tag);
    }

    public bool TryGetActivityContext(string category, out ActivityContext activityLink, out KeyValuePair<string, object?>? tag)
    {
        if (_store.TryGetValue(category, out var link))
        {
            activityLink = link.Item1;
            tag = link.Item2;
            return true;
        }

        activityLink = default;
        tag = default;
        return false;
    }

    public void RemoveActivityContext(string category)
    {
        _store.Remove(category);
    }

    public bool TryCreatePersistentRouteState(out ComponentsActivityPersistentState? state)
    {
        if (TryGetActivityContext(Route, out var context, out var tag) &&
            context != default &&
            tag is { } routeTag &&
            routeTag.Key == "aspnetcore.components.route" &&
            routeTag.Value is string route)
        {
            state = new ComponentsActivityPersistentState(
                $"00-{context.TraceId}-{context.SpanId}-{(byte)context.TraceFlags:x2}",
                context.TraceState,
                context.IsRemote,
                route);
            return true;
        }

        state = null;
        return false;
    }

    public void RestorePersistentRouteState(ComponentsActivityPersistentState state)
    {
        if (ActivityContext.TryParse(
            state.TraceParent,
            state.TraceState,
            state.IsRemote,
            out var context))
        {
            SetActivityContext(
                Route,
                context,
                new KeyValuePair<string, object?>("aspnetcore.components.route", state.Route));
        }
        else
        {
            RemoveActivityContext(Route);
        }
    }

    public void AddActivityContexts(string exceptCategory, Activity targetActivity)
    {
        foreach (var kvp in _store)
        {
            if (kvp.Key != exceptCategory)
            {
                var link = kvp.Value.Item1;
                var tag = kvp.Value.Item2;
                if (link != default)
                {
                    targetActivity.AddLink(new ActivityLink(link));
                }
                if (tag != null)
                {
                    targetActivity.SetTag(tag.Value.Key, tag.Value.Value);
                }
            }
        }
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_ActivityLinksStore")]
    static extern object GetActivityLinksStore(Renderer instance);
}

internal sealed record ComponentsActivityPersistentState(
    string TraceParent,
    string? TraceState,
    bool IsRemote,
    string Route);

internal sealed record ComponentsActivityPersistentStateUpdate(ComponentsActivityPersistentState? Route);

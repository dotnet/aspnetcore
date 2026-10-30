// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Transfers SSR navigation activity context to interactive renderers through persistent state,
/// including updates to existing interactive roots that do not contain a router.
/// </summary>
internal sealed class ComponentsActivityState(ILogger logger)
{
    private ComponentsActivityLinkStore? _activityLinkStore;
    private ComponentsActivitySource? _activitySource;
    private ComponentsActivityStateUpdate? _pendingState;
    private bool _hasPendingState;

    public ComponentsActivityStateUpdate? Capture()
    {
        if (_activityLinkStore is null)
        {
            return null;
        }

        ComponentsActivityRouteState? routeState = null;
        if (ComponentsActivitySource.HasListeners &&
            _activityLinkStore.TryGetActivityContext(ComponentsActivityLinkStore.Route, out var context, out var tag) &&
            context != default &&
            tag is { Value: string route })
        {
            routeState = new(
                $"00-{context.TraceId}-{context.SpanId}-{(byte)context.TraceFlags:x2}",
                context.TraceState,
                context.IsRemote,
                route);
        }

        // Omitting the payload would leave retained interactive roots with stale correlation when tracing is disabled.
        return new(routeState);
    }

    public void Apply(ComponentsActivityStateUpdate? state)
    {
        _pendingState = state;
        _hasPendingState = true;
        ApplyPendingState();
    }

    public void Initialize(ComponentsActivityLinkStore activityLinkStore, ComponentsActivitySource activitySource)
    {
        _activityLinkStore = activityLinkStore;
        _activitySource = activitySource;
        ApplyPendingState();
    }

    private void ApplyPendingState()
    {
        if (_activityLinkStore is null || !_hasPendingState)
        {
            return;
        }

        // A live interactive router owns its route context, rather than the earlier SSR renderer.
        if (_activitySource!.HasNavigationActivity)
        {
            _pendingState = null;
            _hasPendingState = false;
            return;
        }

        if (_pendingState?.Route is { } routeState)
        {
            if (ActivityContext.TryParse(routeState.TraceParent, routeState.TraceState, routeState.IsRemote, out var context))
            {
                _activityLinkStore.SetActivityContext(
                    ComponentsActivityLinkStore.Route,
                    context,
                    new KeyValuePair<string, object?>("aspnetcore.components.route", routeState.Route));
            }
            else
            {
                logger.LogDebug("The persisted navigation activity context could not be restored.");
                _activityLinkStore.RemoveActivityContext(ComponentsActivityLinkStore.Route);
            }
        }
        else
        {
            _activityLinkStore.RemoveActivityContext(ComponentsActivityLinkStore.Route);
        }

        _pendingState = null;
        _hasPendingState = false;
    }
}

internal sealed class ServerComponentsActivityState(ComponentStatePersistenceManager manager)
{
    [PersistentState(AllowUpdates = true)]
    public ComponentsActivityStateUpdate? ActivityState
    {
        get => manager.ActivityState.Capture();
        set => manager.ActivityState.Apply(value);
    }
}

internal sealed class WebAssemblyComponentsActivityState(ComponentStatePersistenceManager manager)
{
    [PersistentState(AllowUpdates = true)]
    public ComponentsActivityStateUpdate? ActivityState
    {
        get => manager.ActivityState.Capture();
        set => manager.ActivityState.Apply(value);
    }
}

internal sealed record ComponentsActivityRouteState(
    string TraceParent,
    string? TraceState,
    bool IsRemote,
    string Route)
{
    // JSON restoration must not depend on constructor parameter names retained by the trimmer.
    public ComponentsActivityRouteState() : this(string.Empty, null, false, string.Empty)
    {
    }
}

internal sealed record ComponentsActivityStateUpdate(ComponentsActivityRouteState? Route)
{
    public ComponentsActivityStateUpdate() : this(Route: null)
    {
    }
}

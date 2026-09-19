// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components;

internal sealed class ComponentsActivityState
{
    private ComponentsActivityLinkStore? _activityLinkStore;
    private ComponentsActivityPersistentStateUpdate? _pendingState;
    private bool _hasPendingState;

    public ComponentsActivityPersistentStateUpdate Capture()
    {
        ComponentsActivityPersistentState? routeState = null;
        _activityLinkStore?.TryCreatePersistentRouteState(out routeState);
        return new ComponentsActivityPersistentStateUpdate(routeState);
    }

    public void Apply(ComponentsActivityPersistentStateUpdate state)
    {
        _pendingState = state;
        _hasPendingState = true;
        ApplyPendingState();
    }

    public void Initialize(ComponentsActivityLinkStore activityLinkStore)
    {
        _activityLinkStore = activityLinkStore;
        ApplyPendingState();
    }

    private void ApplyPendingState()
    {
        if (_activityLinkStore is null || !_hasPendingState)
        {
            return;
        }

        if (_pendingState?.Route is { } routeState)
        {
            _activityLinkStore.RestorePersistentRouteState(routeState);
        }
        else
        {
            _activityLinkStore.RemoveActivityContext(ComponentsActivityLinkStore.Route);
        }

        _pendingState = null;
        _hasPendingState = false;
    }
}

internal sealed class ServerComponentsActivityState(ComponentsActivityState activityState)
{
    [PersistentState(AllowUpdates = true)]
    public ComponentsActivityPersistentStateUpdate ActivityState
    {
        get => activityState.Capture();
        set => activityState.Apply(value);
    }
}

internal sealed class WebAssemblyComponentsActivityState(ComponentsActivityState activityState)
{
    [PersistentState(AllowUpdates = true)]
    public ComponentsActivityPersistentStateUpdate ActivityState
    {
        get => activityState.Capture();
        set => activityState.Apply(value);
    }
}

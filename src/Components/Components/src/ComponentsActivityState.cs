// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components;

internal sealed class ComponentsActivityState
{
    private ComponentsActivityLinkStore? _activityLinkStore;
    private ComponentsActivityPersistentStateUpdate? _pendingState;
    private bool _hasPendingState;

    [PersistentState(AllowUpdates = true)]
    public ComponentsActivityPersistentStateUpdate ActivityState
    {
        get
        {
            ComponentsActivityPersistentState? routeState = null;
            _activityLinkStore?.TryCreatePersistentRouteState(out routeState);
            return new ComponentsActivityPersistentStateUpdate(routeState);
        }
        set
        {
            _pendingState = value;
            _hasPendingState = true;
            ApplyPendingState();
        }
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

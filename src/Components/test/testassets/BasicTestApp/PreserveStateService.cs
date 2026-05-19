// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BasicTestApp;

public class PreserveStateService : IDisposable
{
    private readonly PersistentComponentState _componentApplicationState;
    private PersistingComponentStateSubscription _persistingSubscription;

    private ServiceState _state = new();

    public PreserveStateService(PersistentComponentState componentApplicationState)
    {
        _componentApplicationState = componentApplicationState;
        _persistingSubscription = _componentApplicationState.RegisterOnPersisting(PersistState, RenderMode.InteractiveAuto);
        TryRestoreState();
    }

    public Guid Guid => _state.TheState;

    private void TryRestoreState()
    {
        if (_componentApplicationState.TryTakeFromJson<ServiceState>("Service", out var state))
        {
            _state = state;
        }
        else
        {
            _state = new ServiceState { TheState = Guid.NewGuid() };
        }
    }

    public void NewState() => _state = new ServiceState { TheState = Guid.NewGuid() };

    private Task PersistState()
    {
        _componentApplicationState.PersistAsJson("Service", _state);
        return Task.CompletedTask;
    }

    public void Dispose() => _persistingSubscription.Dispose();

    private class ServiceState
    {
        public Guid TheState { get; set; }
    }
}

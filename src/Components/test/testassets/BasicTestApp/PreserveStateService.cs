// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace BasicTestApp
{
    public class PreserveStateService : IDisposable
    {
        private readonly ComponentApplicationState _componentApplicationState;

        private ServiceState _state = new();

        public PreserveStateService(ComponentApplicationState componentApplicationState)
        {
            _componentApplicationState = componentApplicationState;
            _componentApplicationState.OnPersisting += PersistState;
            TryRestoreState();
        }

        public Guid Guid => _state.TheState;

        private void TryRestoreState()
        {
            if (_componentApplicationState.TryTakeAsJson<ServiceState>("Service", out var state))
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

        public void Dispose()
        {
            _componentApplicationState.OnPersisting -= PersistState;
        }

        private class ServiceState
        {
            public Guid TheState { get; set; }
        }
    }
}

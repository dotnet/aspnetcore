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

        public PreserveStateService(ComponentApplicationState componentApplicationState)
        {
            _componentApplicationState = componentApplicationState;
            _componentApplicationState.OnPersisting += PersistState;
            TryRestoreState();
        }

        public Guid Guid { get; private set; }

        private void TryRestoreState()
        {
            if (_componentApplicationState.TryRedeemFromJson<Guid>("Service", out var guid))
            {
                Guid = guid;
            }
            else
            {
                Guid = Guid.NewGuid();
            }
        }

        public void NewState() => Guid = Guid.NewGuid();

        private Task PersistState()
        {
            _componentApplicationState.PersistAsJson("Service", Guid);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _componentApplicationState.OnPersisting -= PersistState;
        }
    }
}

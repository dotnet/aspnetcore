// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.Services;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class RemoteComponentContext : IComponentContext
    {
        private CircuitClientProxy _clientProxy;

        public bool IsConnected => _clientProxy != null && _clientProxy.Connected;

        internal void Initialize(CircuitClientProxy clientProxy)
        {
            _clientProxy = clientProxy ?? throw new ArgumentNullException(nameof(clientProxy));
        }
    }
}

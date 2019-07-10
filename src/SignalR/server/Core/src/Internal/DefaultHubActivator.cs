// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal class DefaultHubActivator<THub> : IHubActivator<THub> where THub: Hub
    {
        // Object factory for THub instances
        private readonly ObjectFactory _objectFactory = ActivatorUtilities.CreateFactory(typeof(THub), Type.EmptyTypes);
        private readonly IServiceProvider _serviceProvider;

        public DefaultHubActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public virtual HubHandle<THub> Create()
        {
            var created = false;
            var hub = _serviceProvider.GetService<THub>();
            if (hub == null)
            {
                hub = (THub)_objectFactory(_serviceProvider, Array.Empty<object>());
                created = true;
            }

            return new HubHandle<THub>(hub, created);
        }

        public virtual void Release(HubHandle<THub> hub)
        {
            if (hub.Created)
            {
                hub.Hub.Dispose();
            }
        }
    }
}

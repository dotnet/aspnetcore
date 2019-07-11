// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal class DefaultHubActivator<THub> : IHubActivator<THub> where THub : Hub
    {
        // Object factory for THub instances
        private readonly ObjectFactory _objectFactory = ActivatorUtilities.CreateFactory(typeof(THub), Type.EmptyTypes);

        public virtual HubHandle<THub> Create(IServiceProvider serviceProvider)
        {
            var created = false;
            var hub = serviceProvider.GetService<THub>();
            if (hub == null)
            {
                hub = (THub)_objectFactory(serviceProvider, Array.Empty<object>());
                created = true;
            }

            return new HubHandle<THub>(hub, created, state: null);
        }

        public virtual void Release(in HubHandle<THub> handle)
        {
            if (handle.Created)
            {
                handle.Hub.Dispose();
            }
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// A <see cref="Hub"/> activator abstraction.
    /// </summary>
    /// <typeparam name="THub">The hub type.</typeparam>
    public interface IHubActivator<THub> where THub : Hub
    {
        /// <summary>
        /// Creates a hub.
        /// </summary>
        /// <param name="serviceProvider">A <see cref="IServiceProvider"/> that can be used to resolve dependencies.</param>
        /// <returns>The created hub.</returns>
        HubHandle<THub> Create(IServiceProvider serviceProvider);

        /// <summary>
        /// Releases the specified hub.
        /// </summary>
        /// <param name="hub">The hub to release.</param>
        void Release(in HubHandle<THub> hub);
    }
}

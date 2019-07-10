// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// A handle to the hub instance.
    /// </summary>
    /// <typeparam name="THub"></typeparam>
    public readonly struct HubHandle<THub> where THub : Hub
    {
        public HubHandle(THub hub, bool created)
        {
            Hub = hub;
            Created = created;
        }

        /// <summary>
        /// The <typeparamref name="THub"/> that was created
        /// </summary>
        public THub Hub { get; }

        /// <summary>
        /// Determines if the hub was created by this the activator
        /// </summary>
        public bool Created { get; }
    }
}

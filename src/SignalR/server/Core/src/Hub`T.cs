// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR.Internal;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// A base class for a strongly typed SignalR hub.
    /// </summary>
    /// <typeparam name="T">The type of client.</typeparam>
    public abstract class Hub<T> : Hub where T : class
    {
        private IHubCallerClients<T> _clients;

        /// <summary>
        /// Gets or sets a <typeparamref name="T"/> that can be used to invoke methods on the clients connected to this hub.
        /// </summary>
        public new IHubCallerClients<T> Clients
        {
            get
            {
                if (_clients == null)
                {
                    _clients = new TypedHubClients<T>(base.Clients);
                }
                return _clients;
            }
            set => _clients = value;
        }
    }
}

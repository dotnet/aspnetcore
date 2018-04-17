// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR.Internal;

namespace Microsoft.AspNetCore.SignalR
{
    public class Hub<T> : Hub where T : class
    {
        private IHubCallerClients<T> _clients;

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

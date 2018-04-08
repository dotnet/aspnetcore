// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR
{
    public class DynamicHub : Hub
    {
        private DynamicHubClients _clients;

        public new DynamicHubClients Clients
        {
            get
            {
                if (_clients == null)
                {
                    _clients = new DynamicHubClients(base.Clients);
                }

                return _clients;
            }
            set => _clients = value;
        }
    }
}

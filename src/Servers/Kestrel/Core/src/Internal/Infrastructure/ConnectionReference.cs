// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class ConnectionReference
    {
        private readonly WeakReference<KestrelConnection> _weakReference;

        public ConnectionReference(KestrelConnection connection)
        {
            _weakReference = new WeakReference<KestrelConnection>(connection);
            ConnectionId = connection.TransportConnection.ConnectionId;
        }

        public string ConnectionId { get; }

        public bool TryGetConnection(out KestrelConnection connection)
        {
            return _weakReference.TryGetTarget(out connection);
        }
    }
}

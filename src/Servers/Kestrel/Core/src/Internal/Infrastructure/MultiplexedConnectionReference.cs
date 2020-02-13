// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class MultiplexedConnectionReference
    {
        private readonly WeakReference<MultiplexedKestrelConnection> _weakReference;

        public MultiplexedConnectionReference(MultiplexedKestrelConnection connection)
        {
            _weakReference = new WeakReference<MultiplexedKestrelConnection>(connection);
            ConnectionId = connection.TransportConnection.ConnectionId;
        }

        public string ConnectionId { get; }

        public bool TryGetConnection(out MultiplexedKestrelConnection connection)
        {
            return _weakReference.TryGetTarget(out connection);
        }
    }
}

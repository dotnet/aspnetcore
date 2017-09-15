// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public class HttpConnectionReference
    {
        private readonly WeakReference<HttpConnection> _weakReference;

        public HttpConnectionReference(HttpConnection connection)
        {
            _weakReference = new WeakReference<HttpConnection>(connection);
            ConnectionId = connection.ConnectionId;
        }

        public string ConnectionId { get; }

        public bool TryGetConnection(out HttpConnection connection)
        {
            return _weakReference.TryGetTarget(out connection);
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public class FrameConnectionReference
    {
        private readonly WeakReference<FrameConnection> _weakReference;

        public FrameConnectionReference(FrameConnection connection)
        {
            _weakReference = new WeakReference<FrameConnection>(connection);
            ConnectionId = connection.ConnectionId;
        }

        public string ConnectionId { get; }

        public bool TryGetConnection(out FrameConnection connection)
        {
            return _weakReference.TryGetTarget(out connection);
        }
    }
}

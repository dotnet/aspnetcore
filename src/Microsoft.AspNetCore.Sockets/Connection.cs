// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Sockets
{
    public abstract class Connection : IDisposable
    {
        public abstract ConnectionMode Mode { get; }
        public string ConnectionId { get; }

        public ClaimsPrincipal User { get; set; }
        public ConnectionMetadata Metadata { get; } = new ConnectionMetadata();

        protected Connection(string id)
        {
            ConnectionId = id;
        }

        public virtual void Dispose()
        {
        }
    }
}

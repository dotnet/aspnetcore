// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class KestrelConnection
    {
        public KestrelConnection(ConnectionContext connectionContext)
        {
            TransportConnection = connectionContext;
        }

        public ConnectionContext TransportConnection { get; set; }

        public Task CompleteAsync()
        {
            // TODO
            return Task.CompletedTask;
        }

        public void TickHeartbeat()
        {
            
        }
    }
}

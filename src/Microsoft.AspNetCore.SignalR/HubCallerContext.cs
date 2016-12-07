// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubCallerContext
    {
        public HubCallerContext(StreamingConnection connection)
        {
            Connection = connection;
        }

        public StreamingConnection Connection { get; }

        public ClaimsPrincipal User => Connection.User;

        public string ConnectionId => Connection.ConnectionId;
    }
}

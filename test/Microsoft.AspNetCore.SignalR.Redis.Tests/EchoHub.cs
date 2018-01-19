// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Redis.Tests
{
    public class EchoHub : Hub
    {
        public string Echo(string message)
        {
            return message;
        }

        public Task EchoGroup(string group, string message)
        {
            return Clients.Group(group).SendAsync("Echo", message);
        }

        public Task AddSelfToGroup(string group)
        {
            return Groups.AddAsync(Context.ConnectionId, group);
        }
    }
}

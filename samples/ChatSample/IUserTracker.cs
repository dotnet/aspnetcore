// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;

namespace ChatSample
{
    public interface IUserTracker<out THub>
    {
        Task<IEnumerable<UserDetails>> UsersOnline();
        Task AddUser(Connection connection, UserDetails userDetails);
        Task RemoveUser(Connection connection);

        event Action<UserDetails> UserJoined;
        event Action<UserDetails> UserLeft;
    }
}

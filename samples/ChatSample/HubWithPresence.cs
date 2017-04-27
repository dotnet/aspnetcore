// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ChatSample
{
    public class HubWithPresence : HubWithPresence<IClientProxy>
    {
        public HubWithPresence(IUserTracker<HubWithPresence> userTracker)
            : base(userTracker)
        { }
    }

    public class HubWithPresence<TClient> : Hub<TClient>
    {
        private IUserTracker<HubWithPresence<TClient>> _userTracker;

        public HubWithPresence(IUserTracker<HubWithPresence<TClient>> userTracker)
        {
            _userTracker = userTracker;
        }

        public Task<IEnumerable<UserDetails>> GetUsersOnline()
        {
            return _userTracker.UsersOnline();
        }

        public virtual Task OnUserJoined(UserDetails user)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnUserLeft(UserDetails user)
        {
            return Task.CompletedTask;
        }
    }
}

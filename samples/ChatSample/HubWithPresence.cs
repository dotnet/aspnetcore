// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ChatSample
{
    public class HubWithPresence : HubWithPresence<IClientProxy>
    {
        public HubWithPresence(IPresenceManager presenceManager)
            : base(presenceManager)
        {
        }
    }

    public class HubWithPresence<TClient> : Hub<TClient>
    {
        private IPresenceManager _presenceManager;

        public HubWithPresence(IPresenceManager presenceManager)
        {
            _presenceManager = presenceManager;
        }

        public Task<IEnumerable<UserDetails>> UsersOnline
        {
            get
            {
                return _presenceManager.UsersOnline();
            }
        }

        public override Task OnConnectedAsync()
        {
            _presenceManager.UserJoined(Context.Connection);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _presenceManager.UserLeft(Context.Connection);
            return base.OnDisconnectedAsync(exception);
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

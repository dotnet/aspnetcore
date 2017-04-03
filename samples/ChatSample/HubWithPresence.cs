using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

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

        public IEnumerable<UserDetails> UsersOnline
        {
            get
            {
                return _presenceManager.UsersOnline;
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

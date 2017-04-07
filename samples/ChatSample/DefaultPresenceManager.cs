// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatSample
{
    public class DefaultPresenceManager<THub> : IPresenceManager where THub : HubWithPresence
    {
        private IHubContext<THub> _hubContext;
        private HubLifetimeManager<THub> _lifetimeManager;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger _logger;

        public DefaultPresenceManager(IHubContext<THub> hubContext, HubLifetimeManager<THub> lifetimeManager,
            IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory)
        {
            _hubContext = hubContext;
            _lifetimeManager = lifetimeManager;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = loggerFactory.CreateLogger<DefaultPresenceManager<THub>>();
        }

        private readonly ConcurrentDictionary<Connection, UserDetails> _usersOnline
            = new ConcurrentDictionary<Connection, UserDetails>();

        public Task<IEnumerable<UserDetails>> UsersOnline()
            => Task.FromResult(_usersOnline.Values.AsEnumerable());

        public async Task UserJoined(Connection connection)
        {
            var user = new UserDetails(connection.ConnectionId, connection.User.Identity.Name);

            await Notify(hub => hub.OnUserJoined(user));

            _usersOnline.TryAdd(connection, user);
        }

        public async Task UserLeft(Connection connection)
        {
            if (_usersOnline.TryRemove(connection, out var userDetails))
            {
                await Notify(hub => hub.OnUserLeft(userDetails));
            }
        }

        private async Task Notify(Func<THub, Task> invocation)
        {
            foreach (var connection in _usersOnline.Keys)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub, IClientProxy>>();
                    var hub = hubActivator.Create();

                    hub.Clients = _hubContext.Clients;
                    hub.Context = new HubCallerContext(connection);
                    hub.Groups = new GroupManager<THub>(connection, _lifetimeManager);

                    try
                    {
                        await invocation(hub);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Presence notification failed.");
                    }
                    finally
                    {
                        hubActivator.Release(hub);
                    }
                }
            }
        }
    }
}

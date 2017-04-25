
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatSample
{
    public class PresenceHubLifetimeManager<THub> : DefaultHubLifetimeManager<THub>, IDisposable
        where THub : HubWithPresence
    {
        private readonly ConnectionList _connections = new ConnectionList();
        private readonly IUserTracker<THub> _userTracker;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private IHubContext<THub> _hubContext;

        public PresenceHubLifetimeManager(InvocationAdapterRegistry registry, IUserTracker<THub> userTracker,
            IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
            : base(registry)
        {
            _userTracker = userTracker;
            _userTracker.UserJoined += OnUserJoined;
            _userTracker.UserLeft += OnUserLeft;

            _serviceScopeFactory = serviceScopeFactory;
            _serviceProvider = serviceProvider;
            _logger = loggerFactory.CreateLogger<PresenceHubLifetimeManager<THub>>();
        }

        public override async Task OnConnectedAsync(Connection connection)
        {
            await base.OnConnectedAsync(connection);
            _connections.Add(connection);
            await _userTracker.AddUser(connection, new UserDetails(connection.ConnectionId, connection.User.Identity.Name));
        }

        public override async Task OnDisconnectedAsync(Connection connection)
        {
            await base.OnDisconnectedAsync(connection);
            _connections.Remove(connection);
            await _userTracker.RemoveUser(connection);
        }

        private async void OnUserJoined(UserDetails userDetails)
        {
            await Notify(hub => hub.OnUserJoined(userDetails));
        }

        private async void OnUserLeft(UserDetails userDetails)
        {
            await Notify(hub => hub.OnUserLeft(userDetails));
        }

        private async Task Notify(Func<THub, Task> invocation)
        {
            foreach (var connection in _connections)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub, IClientProxy>>();
                    var hub = hubActivator.Create();

                    if (_hubContext == null)
                    {
                        // Cannot be injected due to circular dependency
                        _hubContext = _serviceProvider.GetRequiredService<IHubContext<THub>>();
                    }

                    hub.Clients = _hubContext.Clients;
                    hub.Context = new HubCallerContext(connection);
                    hub.Groups = new GroupManager<THub>(connection, this);

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

        public void Dispose()
        {
            _userTracker.UserJoined -= OnUserJoined;
            _userTracker.UserLeft -= OnUserLeft;
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocketsSample.EndPoints.Hubs;
using SocketsSample.Hubs;

namespace SocketsSample
{
    public class HubEndPoint<THub> : RpcEndpoint<THub>, IHubConnectionContext where THub : Hub
    {
        private readonly AllClientProxy<THub> _all;
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public HubEndPoint(HubLifetimeManager<THub> lifetimeManager,
                           InvocationAdapterRegistry registry,
                           ILoggerFactory loggerFactory,
                           IServiceScopeFactory serviceScopeFactory)
            : base(registry, loggerFactory, serviceScopeFactory)
        {
            _lifetimeManager = lifetimeManager;
            _all = new AllClientProxy<THub>(_lifetimeManager);
        }

        public virtual IClientProxy All => _all;

        public virtual IClientProxy Client(string connectionId)
        {
            return new SingleClientProxy<THub>(_lifetimeManager, connectionId);
        }

        public virtual IClientProxy Group(string groupName)
        {
            return new GroupProxy<THub>(_lifetimeManager, groupName);
        }

        public virtual IClientProxy User(string userId)
        {
            return new UserProxy<THub>(_lifetimeManager, userId);
        }

        public override async Task OnConnected(Connection connection)
        {
            try
            {
                await _lifetimeManager.OnConnectedAsync(connection);

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var hub = scope.ServiceProvider.GetService<THub>() ?? Activator.CreateInstance<THub>();
                    Initialize(connection, hub);
                    await hub.OnConnectedAsync();
                }

                await base.OnConnected(connection);
            }
            finally
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var hub = scope.ServiceProvider.GetService<THub>() ?? Activator.CreateInstance<THub>();
                    Initialize(connection, hub);
                    await hub.OnDisconnectedAsync();
                }

                await _lifetimeManager.OnDisconnectedAsync(connection);
            }
        }

        protected override void BeforeInvoke(Connection connection, THub endpoint)
        {
            Initialize(connection, endpoint);
        }

        private void Initialize(Connection connection, THub endpoint)
        {
            var hub = endpoint;
            hub.Clients = this;
            hub.Context = new HubCallerContext(connection);
            hub.Groups = new GroupManager<THub>(connection, _lifetimeManager);
        }

        protected override void AfterInvoke(Connection connection, THub endpoint)
        {
            // Poison the hub make sure it can't be used after invocation
        }
    }
}

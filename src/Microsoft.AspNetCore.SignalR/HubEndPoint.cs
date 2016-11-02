using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubEndPoint<THub> : RpcEndpoint<THub> where THub : Hub
    {
        private readonly AllClientProxy<THub> _all;
        private readonly HubLifetimeManager<THub> _lifetimeManager;
        private readonly IHubContext<THub> _hubContext;

        public HubEndPoint(HubLifetimeManager<THub> lifetimeManager,
                           IHubContext<THub> hubContext,
                           InvocationAdapterRegistry registry,
                           ILoggerFactory loggerFactory,
                           IServiceScopeFactory serviceScopeFactory)
            : base(registry, loggerFactory, serviceScopeFactory)
        {
            _lifetimeManager = lifetimeManager;
            _hubContext = hubContext;
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

        protected override void BeforeInvoke(Connection connection, THub hub)
        {
            Initialize(connection, hub);
        }

        private void Initialize(Connection connection, THub hub)
        {
            hub.Clients = _hubContext.Clients;
            hub.Context = new HubCallerContext(connection);
            hub.Groups = new GroupManager<THub>(connection, _lifetimeManager);
        }

        protected override void AfterInvoke(Connection connection, THub endpoint)
        {
            // Poison the hub make sure it can't be used after invocation
        }
    }
}

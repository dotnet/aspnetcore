using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubRouteBuilder
    {
        private readonly SocketRouteBuilder _routes;

        public HubRouteBuilder(SocketRouteBuilder routes)
        {
            _routes = routes;
        }

        public void MapHub<THub>(string path) where THub : Hub<IClientProxy>
        {
            _routes.MapSocket(path, builder =>
            {
                builder.UseHub<THub>();
            });
        }
    }
}

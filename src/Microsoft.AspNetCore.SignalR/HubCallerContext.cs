using System.Security.Claims;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubCallerContext
    {
        public HubCallerContext(Connection connection)
        {
            Connection = connection;
        }

        public Connection Connection { get; }

        public ClaimsPrincipal User => Connection.User;

        public string ConnectionId => Connection.ConnectionId;
    }
}

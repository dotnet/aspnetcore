using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR
{
    public abstract class HubLifetimeManager<THub>
    {
        public abstract Task OnConnectedAsync(Connection connection);

        public abstract Task OnDisconnectedAsync(Connection connection);

        public abstract Task InvokeAll(string methodName, params object[] args);

        public abstract Task InvokeConnection(string connectionId, string methodName, params object[] args);

        public abstract Task InvokeGroup(string groupName, string methodName, params object[] args);

        public abstract Task InvokeUser(string userId, string methodName, params object[] args);

        public abstract Task AddGroup(Connection connection, string groupName);

        public abstract Task RemoveGroup(Connection connection, string groupName);
    }

}

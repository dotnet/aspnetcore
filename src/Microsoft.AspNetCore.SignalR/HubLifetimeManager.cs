using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR
{
    public abstract class HubLifetimeManager<THub>
    {
        public abstract Task OnConnectedAsync(Connection connection);

        public abstract Task OnDisconnectedAsync(Connection connection);

        public abstract Task InvokeAllAsync(string methodName, object[] args);

        public abstract Task InvokeConnectionAsync(string connectionId, string methodName, object[] args);

        public abstract Task InvokeGroupAsync(string groupName, string methodName, object[] args);

        public abstract Task InvokeUserAsync(string userId, string methodName, object[] args);

        public abstract Task AddGroupAsync(Connection connection, string groupName);

        public abstract Task RemoveGroupAsync(Connection connection, string groupName);
    }

}

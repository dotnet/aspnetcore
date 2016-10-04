using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Sockets
{
    /// <summary>
    /// Represents an end point that multiple connections connect to. For HTTP, endpoints are URLs, for non HTTP it can be a TCP listener (or similar)
    /// </summary>
    public abstract class EndPoint
    {
        /// <summary>
        /// Live list of connections for this <see cref="EndPoint"/>
        /// </summary>
        public ConnectionList Connections { get; } = new ConnectionList();

        /// <summary>
        /// Called when a new connection is accepted to the endpoint
        /// </summary>
        /// <param name="connection">The new <see cref="Connection"/></param>
        /// <returns>A <see cref="Task"/> that represents the connection lifetime. When the task completes, the connection is complete.</returns>
        public abstract Task OnConnected(Connection connection);
    }
}

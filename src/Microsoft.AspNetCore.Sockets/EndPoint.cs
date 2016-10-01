using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Sockets
{
    /// <summary>
    /// Represents an end point that multiple connections connect to. For HTTP, endpoints are URLs, for non HTTP it can be a TCP listener (or similar)
    /// </summary>
    public class EndPoint
    {
        // This is a stream based API, we might just want to change to a message based API or invent framing
        // over this stream based API to do a message based API
        public virtual Task OnConnected(Connection connection)
        {
            return Task.CompletedTask;
        }
    }
}

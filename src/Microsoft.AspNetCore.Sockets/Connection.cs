using System.Security.Claims;
using Channels;

namespace Microsoft.AspNetCore.Sockets
{
    public class Connection
    {
        public string ConnectionId { get; set; }
        public ClaimsPrincipal User { get; set; }
        public IChannel Channel { get; set; }
        public ConnectionMetadata Metadata { get; } = new ConnectionMetadata();
    }
}

#if NET45
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Microsoft.AspNet.HttpFeature
{
    public interface IHttpWebSocketAccept
    {
        bool IsWebSocketRequest { get; set; }
        Task<WebSocket> AcceptAsync();
    }
}
#endif
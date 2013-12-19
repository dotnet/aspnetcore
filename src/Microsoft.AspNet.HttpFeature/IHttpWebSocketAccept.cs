using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Interfaces
{
    public interface IHttpWebSocketAccept
    {
        bool IsWebSocketRequest { get; set; }
        Task<WebSocket> AcceptAsync();
    }
}

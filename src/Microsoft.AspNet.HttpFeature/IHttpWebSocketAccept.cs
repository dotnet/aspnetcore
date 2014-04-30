#if NET45
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.HttpFeature
{
    [AssemblyNeutral]
    public interface IHttpWebSocketAccept
    {
        bool IsWebSocketRequest { get; set; }
        Task<WebSocket> AcceptAsync();
    }
}
#endif
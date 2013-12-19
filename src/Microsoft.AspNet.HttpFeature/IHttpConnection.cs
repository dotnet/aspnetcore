using System.Net;

namespace Microsoft.AspNet.Interfaces
{
    public interface IHttpConnection
    {
        IPAddress RemoteIpAddress { get; set; }
        int RemotePort { get; set; }
        IPAddress LocalIpAddress { get; set; }
        int LocalPort { get; set; }
        bool IsLocal { get; set; }
    }
}
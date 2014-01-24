using System.Net;

namespace Microsoft.AspNet.HttpFeature
{
    public interface IHttpConnection
    {
#if NET45
        IPAddress RemoteIpAddress { get; set; }
        IPAddress LocalIpAddress { get; set; }
#endif
        int RemotePort { get; set; }
        int LocalPort { get; set; }
        bool IsLocal { get; set; }
    }
}
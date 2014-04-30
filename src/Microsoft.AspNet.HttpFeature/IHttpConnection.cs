using System.Net;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.HttpFeature
{
    [AssemblyNeutral]
    public interface IHttpConnection
    {
        IPAddress RemoteIpAddress { get; set; }
        IPAddress LocalIpAddress { get; set; }
        int RemotePort { get; set; }
        int LocalPort { get; set; }
        bool IsLocal { get; set; }
    }
}
using System.Net;

namespace Microsoft.AspNetCore.Connections.Features
{
    public interface IConnectionEndPointFeature
    {
        EndPoint LocalEndpoint { get; set; }
        EndPoint RemoteEndpoint { get; set; }
    }
}

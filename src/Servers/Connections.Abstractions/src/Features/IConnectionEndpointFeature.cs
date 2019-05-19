using System.Net;

namespace Microsoft.AspNetCore.Connections.Features
{
    public interface IConnectionEndPointFeature
    {
        EndPoint LocalEndPoint { get; set; }
        EndPoint RemoteEndPoint { get; set; }
    }
}

using System.Net;
using ClientSample;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static class TcpHubConnectionBuilderExtensions
    {
        public static IHubConnectionBuilder WithEndPoint(this IHubConnectionBuilder builder, IPEndPoint endPoint)
        {
            builder.ConfigureConnectionFactory(() => new TcpConnection(endPoint));

            return builder;
        }
    }
}

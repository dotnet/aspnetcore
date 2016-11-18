using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR.Test.Server
{
    public class EchoEndPoint : EndPoint
    {
        public async override Task OnConnectedAsync(Connection connection)
        {
            var buffer = new byte[256];
            var bytesRead = 0;
            var stream = connection.Channel.GetStream();
            while((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await stream.WriteAsync(buffer, 0, bytesRead);
            }
        }
    }
}

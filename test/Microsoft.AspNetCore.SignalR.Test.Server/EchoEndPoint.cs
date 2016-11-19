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
            await connection.Channel.Input.CopyToAsync(connection.Channel.Output);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR
{
    public static class SignalRSocketBuilderExtensions
    {
        public static ISocketBuilder UseHub<THub>(this ISocketBuilder socketBuilder) where THub : Hub<IClientProxy>
        {
            return socketBuilder.Run(connection =>
            {
                var endpoint = socketBuilder.ApplicationServices.GetRequiredService<HubEndPoint<THub>>();
                return endpoint.OnConnectedAsync(connection);
            });
        }
    }
}

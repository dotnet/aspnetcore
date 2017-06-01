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
            var endpoint = socketBuilder.ApplicationServices.GetRequiredService<HubEndPoint<THub>>();
            return socketBuilder.Run(connection =>
            {
                return endpoint.OnConnectedAsync(connection);
            });
        }
    }
}

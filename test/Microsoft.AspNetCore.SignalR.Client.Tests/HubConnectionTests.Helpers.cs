using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HubConnectionTests
    {
        private static HubConnection CreateHubConnection(TestConnection connection, IHubProtocol protocol = null)
        {
            var builder = new HubConnectionBuilder();
            builder.WithConnectionFactory(() => connection);
            if (protocol != null)
            {
                builder.WithHubProtocol(protocol);
            }

            return builder.Build();
        }
    }
}

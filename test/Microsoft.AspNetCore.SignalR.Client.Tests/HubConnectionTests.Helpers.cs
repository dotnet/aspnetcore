// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HubConnectionTests
    {
        private static HubConnection CreateHubConnection(TestConnection connection, IHubProtocol protocol = null)
        {
            var builder = new HubConnectionBuilder();
            builder.WithConnectionFactory(async format =>
            {
                await connection.StartAsync(format);
                return connection;
            },
            connecton => ((TestConnection)connection).DisposeAsync());
            
            if (protocol != null)
            {
                builder.WithHubProtocol(protocol);
            }

            return builder.Build();
        }
    }
}

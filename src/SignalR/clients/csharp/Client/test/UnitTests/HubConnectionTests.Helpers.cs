// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HubConnectionTests
    {
        private static HubConnection CreateHubConnection(TestConnection connection, IHubProtocol protocol = null, ILoggerFactory loggerFactory = null)
        {
            var builder = new HubConnectionBuilder().WithUrl("http://example.com");

            var delegateConnectionFactory = new DelegateConnectionFactory(
                endPoint => connection.StartAsync());

            builder.Services.AddSingleton<IConnectionFactory>(delegateConnectionFactory);

            if (loggerFactory != null)
            {
                builder.WithLoggerFactory(loggerFactory);
            }

            if (protocol != null)
            {
                builder.Services.AddSingleton(protocol);
            }

            return builder.Build();
        }
    }
}

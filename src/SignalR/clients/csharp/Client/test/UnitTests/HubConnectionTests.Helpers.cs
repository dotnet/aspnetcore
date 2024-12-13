// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public partial class HubConnectionTests
{
    private static HubConnection CreateHubConnection(
        TestConnection connection,
        IHubProtocol protocol = null,
        ILoggerFactory loggerFactory = null,
        SignalRClientActivitySource clientActivitySource = null)
    {
        var builder = new HubConnectionBuilder().WithUrl("http://example.com");

        var delegateConnectionFactory = new DelegateConnectionFactory(
            async endPoint =>
            {
                connection.RemoteEndPoint = endPoint;
                return await connection.StartAsync();
            });

        builder.Services.AddSingleton<IConnectionFactory>(delegateConnectionFactory);

        if (loggerFactory != null)
        {
            builder.WithLoggerFactory(loggerFactory);
        }

        if (protocol != null)
        {
            builder.Services.AddSingleton(protocol);
        }

        if (clientActivitySource != null)
        {
            builder.Services.AddSingleton(clientActivitySource);
        }

        return builder.Build();
    }
}

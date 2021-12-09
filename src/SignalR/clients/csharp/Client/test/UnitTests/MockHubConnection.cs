// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Moq;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public static class MockHubConnection
{
    public static Mock<HubConnection> Get()
    {
        IConnectionFactory connectionFactory = new Mock<IConnectionFactory>().Object;
        IHubProtocol protocol = new Mock<IHubProtocol>().Object;
        EndPoint endPoint = new Mock<EndPoint>().Object;
        IServiceProvider serviceProvider = new Mock<IServiceProvider>().Object;
        ILoggerFactory loggerFactory = null;
        return new Mock<HubConnection>(MockBehavior.Strict,
            connectionFactory, protocol, endPoint, serviceProvider, loggerFactory);
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Moq;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
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
}

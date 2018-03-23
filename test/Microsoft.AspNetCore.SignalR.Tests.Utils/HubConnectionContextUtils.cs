// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public static class HubConnectionContextUtils
    {
        public static HubConnectionContext Create(ConnectionContext connection)
        {
            return new HubConnectionContext(connection, TimeSpan.FromSeconds(15), NullLoggerFactory.Instance)
            {
                Protocol = new JsonHubProtocol()
            };
        }

        public static Mock<HubConnectionContext> CreateMock(ConnectionContext connection)
        {
            var mock = new Mock<HubConnectionContext>(connection, TimeSpan.FromSeconds(15), NullLoggerFactory.Instance) { CallBase = true };
            var protocol = new JsonHubProtocol();
            mock.SetupGet(m => m.Protocol).Returns(protocol);
            return mock;

        }
    }
}

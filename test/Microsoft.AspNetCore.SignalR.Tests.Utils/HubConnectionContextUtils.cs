// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Encoders;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public static class HubConnectionContextUtils
    {
        public static HubConnectionContext Create(DefaultConnectionContext connection)
        {
            return new HubConnectionContext(connection, TimeSpan.FromSeconds(15), NullLoggerFactory.Instance)
            {
                ProtocolReaderWriter = new HubProtocolReaderWriter(new JsonHubProtocol(), new PassThroughEncoder())
            };
        }

        public static Mock<HubConnectionContext> CreateMock(DefaultConnectionContext connection)
        {
            var mock = new Mock<HubConnectionContext>(connection, TimeSpan.FromSeconds(15), NullLoggerFactory.Instance) { CallBase = true };
            var readerWriter = new HubProtocolReaderWriter(new JsonHubProtocol(), new PassThroughEncoder());
            mock.SetupGet(m => m.ProtocolReaderWriter).Returns(readerWriter);
            return mock;

        }
    }
}

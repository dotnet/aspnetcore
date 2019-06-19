// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Tests
{
#if TESTUTILS
    public
#else
    internal
#endif
    static class HubConnectionContextUtils
    {
        public static HubConnectionContext Create(ConnectionContext connection, IHubProtocol protocol = null, string userIdentifier = null)
        {
            var options = new HubOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(15),
            };

            return new HubConnectionContext(connection, options, NullLoggerFactory.Instance)
            {
                Protocol = protocol ?? new JsonHubProtocol(),
                UserIdentifier = userIdentifier,
            };
        }

        public static MockHubConnectionContext CreateMock(ConnectionContext connection)
        {
            var options = new HubOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(15),
                ClientTimeoutInterval = TimeSpan.FromSeconds(15),
                StreamBufferCapacity = 10,
            };
            return new MockHubConnectionContext(connection, options, NullLoggerFactory.Instance);
        }

        public class MockHubConnectionContext : HubConnectionContext
        {
            public MockHubConnectionContext(ConnectionContext connectionContext, HubOptions options, ILoggerFactory loggerFactory)
                : base(connectionContext, options, loggerFactory)
            {
            }

            public override ValueTask WriteAsync(HubMessage message, CancellationToken cancellationToken = default)
            {
                throw new Exception();
            }
        }
    }

}

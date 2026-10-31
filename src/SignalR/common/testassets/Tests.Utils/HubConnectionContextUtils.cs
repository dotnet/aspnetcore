// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Tests;
#if TESTUTILS
public
#else
internal
#endif
    static class HubConnectionContextUtils
{
    public static HubConnectionContext Create(ConnectionContext connection, IHubProtocol protocol = null, string userIdentifier = null)
    {
        var contextOptions = new HubConnectionContextOptions()
        {
            KeepAliveInterval = TimeSpan.FromSeconds(15),
        };

        return new HubConnectionContext(connection, contextOptions, NullLoggerFactory.Instance)
        {
            Protocol = protocol ?? new JsonHubProtocol(),
            UserIdentifier = userIdentifier,
        };
    }

    public static MockHubConnectionContext CreateMock(ConnectionContext connection)
    {
        var contextOptions = new HubConnectionContextOptions()
        {
            KeepAliveInterval = TimeSpan.FromSeconds(15),
            ClientTimeoutInterval = TimeSpan.FromSeconds(15),
            StreamBufferCapacity = 10,
        };
        return new MockHubConnectionContext(connection, contextOptions, NullLoggerFactory.Instance);
    }

    public class MockHubConnectionContext : HubConnectionContext
    {
        public MockHubConnectionContext(ConnectionContext connectionContext, HubConnectionContextOptions contextOptions, ILoggerFactory loggerFactory)
            : base(connectionContext, contextOptions, loggerFactory)
        {
        }

        public override ValueTask WriteAsync(HubMessage message, CancellationToken cancellationToken = default)
        {
            throw new Exception();
        }
    }
}

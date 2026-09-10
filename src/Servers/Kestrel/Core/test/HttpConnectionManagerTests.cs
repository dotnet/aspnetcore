// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class HttpConnectionManagerTests : LoggedTest
{
    [Fact]
    public void UnrootedConnectionsGetRemovedFromHeartbeat()
    {
        var trace = new KestrelTrace(LoggerFactory);
        var connectionId = "0";
        var httpConnectionManager = new ConnectionManager(trace, ResourceCounter.Unlimited);

        // Create HttpConnection in inner scope so it doesn't get rooted by the current frame.
        UnrootedConnectionsGetRemovedFromHeartbeatInnerScope(connectionId, httpConnectionManager, trace);

        GC.Collect();
        GC.WaitForPendingFinalizers();

        var connectionCount = 0;
        httpConnectionManager.Walk(_ => connectionCount++);

        Assert.Equal(0, connectionCount);

        Assert.Single(TestSink.Writes.Where(c => c.EventId.Name == "ApplicationNeverCompleted"));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void UnrootedConnectionsGetRemovedFromHeartbeatInnerScope(
        string connectionId,
        ConnectionManager httpConnectionManager,
        KestrelTrace trace)
    {
        var serviceContext = new TestServiceContext();
        var mock = new Mock<DefaultConnectionContext>() { CallBase = true };
        mock.Setup(m => m.ConnectionId).Returns(connectionId);
        var transportConnectionManager = new TransportConnectionManager(httpConnectionManager);
        var httpConnection = new KestrelConnection<ConnectionContext>(0, serviceContext, transportConnectionManager, _ => Task.CompletedTask, mock.Object, trace, TestContextFactory.CreateMetricsContext(mock.Object));
        transportConnectionManager.AddConnection(0, httpConnection);

        var connectionCount = 0;
        httpConnectionManager.Walk(_ => connectionCount++);

        Assert.Equal(1, connectionCount);
        Assert.DoesNotContain(TestSink.Writes, c => c.EventId.Name == "ApplicationNeverCompleted");

        // Ensure httpConnection doesn't get GC'd before this point.
        GC.KeepAlive(httpConnection);
    }
}

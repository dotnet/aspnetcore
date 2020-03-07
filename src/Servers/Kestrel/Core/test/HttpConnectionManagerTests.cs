// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class HttpConnectionManagerTests
    {
        [Fact]
        public void UnrootedConnectionsGetRemovedFromHeartbeat()
        {
            var connectionId = "0";
            var trace = new Mock<IKestrelTrace>();
            var httpConnectionManager = new ConnectionManager(trace.Object, ResourceCounter.Unlimited);

            // Create HttpConnection in inner scope so it doesn't get rooted by the current frame.
            UnrootedConnectionsGetRemovedFromHeartbeatInnerScope(connectionId, httpConnectionManager, trace);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            var connectionCount = 0;
            httpConnectionManager.Walk(_ => connectionCount++);

            Assert.Equal(0, connectionCount);
            trace.Verify(t => t.ApplicationNeverCompleted(connectionId), Times.Once());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UnrootedConnectionsGetRemovedFromHeartbeatInnerScope(
            string connectionId,
            ConnectionManager httpConnectionManager,
            Mock<IKestrelTrace> trace)
        {
            var serviceContext = new TestServiceContext();
            var mock = new Mock<DefaultConnectionContext>() { CallBase = true };
            mock.Setup(m => m.ConnectionId).Returns(connectionId);
            var httpConnection = new KestrelConnection<ConnectionContext>(0, serviceContext, _ => Task.CompletedTask, mock.Object, Mock.Of<IKestrelTrace>());

            httpConnectionManager.AddConnection(0, httpConnection);

            var connectionCount = 0;
            httpConnectionManager.Walk(_ => connectionCount++);

            Assert.Equal(1, connectionCount);
            trace.Verify(t => t.ApplicationNeverCompleted(connectionId), Times.Never());

            // Ensure httpConnection doesn't get GC'd before this point.
            GC.KeepAlive(httpConnection);
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
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
            var httpConnectionManager = new HttpConnectionManager(trace.Object, ResourceCounter.Unlimited);

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
            HttpConnectionManager httpConnectionManager,
            Mock<IKestrelTrace> trace)
        {
            var httpConnection = new HttpConnection(new HttpConnectionContext
            {
                ServiceContext = new TestServiceContext(),
                ConnectionId = connectionId
            });

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

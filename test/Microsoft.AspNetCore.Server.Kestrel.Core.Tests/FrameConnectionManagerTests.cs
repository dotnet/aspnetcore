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
    public class FrameConnectionManagerTests
    {
        [Fact]
        public void UnrootedConnectionsGetRemovedFromHeartbeat()
        {
            var connectionId = "0";
            var trace = new Mock<IKestrelTrace>();
            var frameConnectionManager = new FrameConnectionManager(trace.Object);

            // Create FrameConnection in inner scope so it doesn't get rooted by the current frame.
            UnrootedConnectionsGetRemovedFromHeartbeatInnerScope(connectionId, frameConnectionManager, trace);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            var connectionCount = 0;
            frameConnectionManager.Walk(_ => connectionCount++);

            Assert.Equal(0, connectionCount);
            trace.Verify(t => t.ApplicationNeverCompleted(connectionId), Times.Once());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UnrootedConnectionsGetRemovedFromHeartbeatInnerScope(
            string connectionId,
            FrameConnectionManager frameConnectionManager,
            Mock<IKestrelTrace> trace)
        {
            var serviceContext = new TestServiceContext
            {
                ConnectionManager = frameConnectionManager
            };

            // The FrameConnection constructor adds itself to the connection manager.
            var ignore = new FrameConnection(new FrameConnectionContext
            {
                ServiceContext = serviceContext,
                ConnectionId = connectionId
            });

            var connectionCount = 0;
            frameConnectionManager.Walk(_ => connectionCount++);

            Assert.Equal(1, connectionCount);
            trace.Verify(t => t.ApplicationNeverCompleted(connectionId), Times.Never());
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNet.Server.Kestrel;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.AspNet.Server.Kestrel.Networking;
using Microsoft.AspNet.Server.KestrelTests.TestHelpers;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class SocketOutputTests
    {
        [Fact]
        public void CanWrite1MB()
        {
            // This test was added because when initially implementing write-behind buffering in
            // SocketOutput, the write callback would never be invoked for writes larger than
            // _maxBytesPreCompleted even after the write actually completed.

            // Arrange
            var mockLibuv = new MockLibuv
            {
                OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    triggerCompleted(0);
                    return 0;
                }
            };

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var socketOutput = new SocketOutput(kestrelThread, socket, 0, trace);

                // I doubt _maxBytesPreCompleted will ever be over a MB. If it is, we should change this test.
                var bufferSize = 1048576;
                var buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
                var completedWh = new ManualResetEventSlim();
                Action<Exception, object, bool> onCompleted = (ex, state, calledInline) =>
                {
                    Assert.Null(ex);
                    Assert.Null(state);
                    completedWh.Set();
                };

                // Act
                socketOutput.Write(buffer, onCompleted, null);

                // Assert
                Assert.True(completedWh.Wait(1000));
            }
        }

        [Fact]
        public void WritesDontCompleteImmediatelyWhenTooManyBytesAreAlreadyPreCompleted()
        {
            // This should match _maxBytesPreCompleted in SocketOutput
            var maxBytesPreCompleted = 65536;
            var completeQueue = new Queue<Action<int>>();

            // Arrange
            var mockLibuv = new MockLibuv
            {
                OnWrite = (socket, buffers, triggerCompleted) =>
                {
                    completeQueue.Enqueue(triggerCompleted);
                    return 0;
                }
            };

            using (var kestrelEngine = new KestrelEngine(mockLibuv, new TestServiceContext()))
            {
                kestrelEngine.Start(count: 1);

                var kestrelThread = kestrelEngine.Threads[0];
                var socket = new MockSocket(kestrelThread.Loop.ThreadId, new TestKestrelTrace());
                var trace = new KestrelTrace(new TestKestrelTrace());
                var socketOutput = new SocketOutput(kestrelThread, socket, 0, trace);

                var bufferSize = maxBytesPreCompleted;
                var buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
                var completedWh = new ManualResetEventSlim();
                Action<Exception, object, bool> onCompleted = (ex, state, calledInline) =>
                {
                    Assert.Null(ex);
                    Assert.Null(state);
                    completedWh.Set();
                };

                // Act 
                socketOutput.Write(buffer, onCompleted, null);
                // Assert
                // The first write should pre-complete since it is <= _maxBytesPreCompleted.
                Assert.True(completedWh.Wait(1000));
                // Arrange
                completedWh.Reset();
                // Act
                socketOutput.Write(buffer, onCompleted, null);
                // Assert 
                // Too many bytes are already pre-completed for the second write to pre-complete.
                Assert.False(completedWh.Wait(1000));
                // Act
                completeQueue.Dequeue()(0);
                // Assert
                // Finishing the first write should allow the second write to pre-complete.
                Assert.True(completedWh.Wait(1000));
            }
        }

        private class MockSocket : UvStreamHandle
        {
            public MockSocket(int threadId, IKestrelTrace logger) : base(logger)
            {
                // Set the handle to something other than IntPtr.Zero
                // so handle.Validate doesn't fail in Libuv.write
                handle = (IntPtr)1;
                _threadId = threadId;
            }

            protected override bool ReleaseHandle()
            {
                // No-op
                return true;
            }
        }
    }
}

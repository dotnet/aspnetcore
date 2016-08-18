// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class UvTimerHandleTests
    {
        [Fact]
        public void TestTimeout()
        {
            var trace = new TestKestrelTrace();

            var loop = new UvLoopHandle(trace);
            loop.Init(new Libuv());

            var timer = new UvTimerHandle(trace);
            timer.Init(loop, (a, b) => { });

            var callbackInvoked = false;
            timer.Start(_ =>
            {
                callbackInvoked = true;
            }, 1, 0);
            loop.Run();

            timer.Dispose();
            loop.Run();

            loop.Dispose();

            Assert.True(callbackInvoked);
        }

        [Fact]
        public void TestRepeat()
        {
            var trace = new TestKestrelTrace();

            var loop = new UvLoopHandle(trace);
            loop.Init(new Libuv());

            var timer = new UvTimerHandle(trace);
            timer.Init(loop, (callback, handle) => { });

            var callbackCount = 0;
            timer.Start(_ =>
            {
                if (callbackCount < 2)
                {
                    callbackCount++;
                }
                else
                {
                    timer.Stop();
                }
            }, 1, 1);

            loop.Run();

            timer.Dispose();
            loop.Run();

            loop.Dispose();

            Assert.Equal(2, callbackCount);
        }
    }
}

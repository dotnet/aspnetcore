// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests
{
    public class UvTimerHandleTests
    {
        private readonly LibuvTrace _trace = new LibuvTrace(new TestApplicationErrorLogger());

        [Fact]
        public void TestTimeout()
        {
            var loop = new UvLoopHandle(_trace);
            loop.Init(new LibuvFunctions());

            var timer = new UvTimerHandle(_trace);
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
            var loop = new UvLoopHandle(_trace);
            loop.Init(new LibuvFunctions());

            var timer = new UvTimerHandle(_trace);
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

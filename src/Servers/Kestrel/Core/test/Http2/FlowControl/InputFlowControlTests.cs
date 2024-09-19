// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests.Http2.FlowControl;

public class InputFlowControlTests
{
    private const int IterationCount = 1;

    [Fact]
    public async Task Threads1_Advance()
    {
        for (var i = 0; i < IterationCount; i++)
        {
            var sut = new InputFlowControl(5000, 1);
            await Task.Run(() => Advance(sut)); ;
            Assert.Throws<Http2ConnectionErrorException>(() => sut.TryAdvance(1));
        }

        static void Advance(InputFlowControl sut)
        {
            for (var j = 0; j < 5000; j++)
            {
                Assert.True(sut.TryAdvance(1));
            }
        }
    }

    [Fact]
    public async Task Threads2_Advance()
    {
        for (var i = 0; i < IterationCount; i++)
        {
            var sut = new InputFlowControl(10000, 1);
            var t1 = Task.Run(() => Advance(sut));
            var t2 = Task.Run(() => Advance(sut));
            await Task.WhenAll(t1, t2);
            Assert.Throws<Http2ConnectionErrorException>(() => sut.TryAdvance(1));
        }

        static void Advance(InputFlowControl sut)
        {
            for (var j = 0; j < 5000; j++)
            {
                Assert.True(sut.TryAdvance(1));
            }
        }
    }

    [Fact]
    public async Task Threads3_Advance()
    {
        for (var i = 0; i < IterationCount; i++)
        {
            var sut = new InputFlowControl(15000, 1);
            var t1 = Task.Run(() => Advance(sut));
            var t2 = Task.Run(() => Advance(sut));
            var t3 = Task.Run(() => Advance(sut));
            await Task.WhenAll(t1, t2, t3);
            Assert.Throws<Http2ConnectionErrorException>(() => sut.TryAdvance(1));
        }

        static void Advance(InputFlowControl sut)
        {
            for (var j = 0; j < 5000; j++)
            {
                Assert.True(sut.TryAdvance(1));
            }
        }
    }

    [Fact]
    public async Task Threads3_WindowUpdates()
    {
        for (var i = 0; i < IterationCount; i++)
        {
            var sut = new InputFlowControl(15000, 0);
            var t1 = Task.Run(() => WindowUpdate(sut));
            var t2 = Task.Run(() => WindowUpdate(sut));
            var t3 = Task.Run(() => WindowUpdate(sut));
            await Task.WhenAll(t1, t2, t3);
            Assert.Equal((uint)15000 + 5000 * 3, sut.Available);
        }

        static void WindowUpdate(InputFlowControl sut)
        {
            for (var j = 0; j < 5000; j++)
            {
                Assert.True(sut.TryUpdateWindow(1, out var size));
                Assert.Equal(1, size);
            }
        }
    }

    [Fact]
    public async Task Threads4_Advance()
    {
        for (var i = 0; i < IterationCount; i++)
        {
            var sut = new InputFlowControl(20000, 1);
            var t1 = Task.Run(() => Advance(sut));
            var t2 = Task.Run(() => Advance(sut));
            var t3 = Task.Run(() => Advance(sut));
            var t4 = Task.Run(() => Advance(sut));
            await Task.WhenAll(t1, t2, t3, t4);
        }

        static void Advance(InputFlowControl sut)
        {
            for (var j = 0; j < 5000; j++)
            {
                Assert.True(sut.TryAdvance(1));
            }
        }
    }

    [Fact]
    public async Task Threads3_Abort()
    {
        for (var i = 0; i < IterationCount; i++)
        {
            var sut = new InputFlowControl(20000, 1);
            var t1 = Task.Run(() => Advance(sut));
            var t2 = Task.Run(() => Advance(sut));
            var t3 = Task.Run(() => Advance(sut));
            var t4 = Task.Run(() => Abort(i, sut));
            await Task.WhenAll(t1, t2, t3, t4);
        }

        static void Abort(int i, InputFlowControl sut)
        {
            while (i > 0)
            {
                i--;
            }

            sut.Abort();
        }

        static void Advance(InputFlowControl sut)
        {
            var isAborted = false;
            for (var j = 0; j < 5000; j++)
            {
                var state = sut.TryAdvance(1);
                if (!isAborted && !state)
                {
                    isAborted = true;
                }

                Assert.True(state || isAborted);
            }
        }
    }

    [Fact]
    public void Abort_Abort()
    {
        var sut = new InputFlowControl(15000, 1);
        sut.Abort();
        Assert.Equal(0, sut.Abort());
    }

    [Fact]
    public async Task Threads3Abort_AssertAfter()
    {
        for (var i = 0; i < IterationCount; i++)
        {
            var isAborted = false;
            var sut = new InputFlowControl(90000, 1);
            var t1 = Task.Run(() => Advance(ref isAborted, sut));
            var t2 = Task.Run(() => Advance(ref isAborted, sut));
            var t3 = Task.Run(() => Advance(ref isAborted, sut));
            Abort(i, sut);
            Interlocked.Exchange(ref isAborted, true);
            await Task.WhenAll(t1, t2, t3);
        }

        static void Abort(int i, InputFlowControl sut)
        {
            while (i > 0)
            {
                i--;
            }

            sut.Abort();
        }

        static void Advance(ref bool isAborted, InputFlowControl sut)
        {
            for (var j = 0; j < 30000; j++)
            {
                var localAborted = isAborted;
                var state = sut.TryAdvance(1);
                if (localAborted)
                {
                    Assert.False(state);
                }
            }
        }
    }

    [Fact]
    public async Task Threads3Abort_AssertBefore()
    {
        for (var i = 0; i < IterationCount; i++)
        {
            var isAborting = false;
            var sut = new InputFlowControl(30000, 1);
            var t1 = Task.Run(() => Advance(ref isAborting, sut));
            var t2 = Task.Run(() => Advance(ref isAborting, sut));
            var t3 = Task.Run(() => Advance(ref isAborting, sut));
            Interlocked.Exchange(ref isAborting, true);
            Abort(i, sut);
            await Task.WhenAll(t1, t2, t3);
        }

        static void Abort(int i, InputFlowControl sut)
        {
            while (i > 0)
            {
                i--;
            }

            sut.Abort();
        }

        static void Advance(ref bool isAborting, InputFlowControl sut)
        {
            for (var j = 0; j < 10000; j++)
            {
                var state = sut.TryAdvance(1);
                var localAborting = isAborting;
                if (!localAborting)
                {
                    Assert.True(state);
                }
            }
        }
    }

    [Fact]
    public async Task Threads3WindowUpdates_AssertAfter()
    {
        for (var i = 0; i < IterationCount; i++)
        {
            var isAborted = false;
            var sut = new InputFlowControl(90000, 1);
            var t1 = Task.Run(() => WindowUpdate(ref isAborted, sut));
            var t2 = Task.Run(() => WindowUpdate(ref isAborted, sut));
            var t3 = Task.Run(() => WindowUpdate(ref isAborted, sut));
            Abort(i, sut);
            Interlocked.Exchange(ref isAborted, true);
            await Task.WhenAll(t1, t2, t3);
        }

        static void Abort(int i, InputFlowControl sut)
        {
            while (i > 0)
            {
                i--;
            }

            sut.Abort();
        }

        static void WindowUpdate(ref bool isAborted, InputFlowControl sut)
        {
            for (var j = 0; j < 30000; j++)
            {
                var localAborted = isAborted;
                var state = sut.TryUpdateWindow(1, out _);
                if (localAborted)
                {
                    Assert.False(state);
                }
            }
        }
    }

    [Fact]
    public async Task Threads3WindowUpdates_AssertBefore()
    {
        for (var i = 0; i < IterationCount; i++)
        {
            var isAborting = false;
            var sut = new InputFlowControl(30000, 1);
            var t1 = Task.Run(() => WindowUpdate(ref isAborting, sut));
            var t2 = Task.Run(() => WindowUpdate(ref isAborting, sut));
            var t3 = Task.Run(() => WindowUpdate(ref isAborting, sut));
            Interlocked.Exchange(ref isAborting, true);
            Abort(i, sut);
            await Task.WhenAll(t1, t2, t3);
        }

        static void Abort(int i, InputFlowControl sut)
        {
            while (i > 0)
            {
                i--;
            }

            sut.Abort();
        }

        static void WindowUpdate(ref bool isAborting, InputFlowControl sut)
        {
            for (var j = 0; j < 10000; j++)
            {
                var state = sut.TryUpdateWindow(1, out _);
                var localAborting = isAborting;
                if (!localAborting)
                {
                    Assert.True(state);
                }
            }
        }
    }

    [Fact]
    public async Task Threads3Advance_WindowUpdates()
    {
        for (var i = 0; i < IterationCount; i++)
        {
            var sut = new InputFlowControl(30000, 0);
            var t0 = Task.Run(() => WindowUpdate(sut));
            var t1 = Task.Run(() => Advance(sut));
            var t2 = Task.Run(() => Advance(sut));
            var t3 = Task.Run(() => Advance(sut));
            await Task.WhenAll(t0, t1, t2, t3);
            Assert.Equal((uint)10000, sut.Available);
        }

        static void WindowUpdate(InputFlowControl sut)
        {
            for (var j = 0; j < 10000; j++)
            {
                Assert.True(sut.TryUpdateWindow(1, out var size));
                Assert.Equal(1, size);
            }

        }

        static void Advance(InputFlowControl sut)
        {
            for (var j = 0; j < 10000; j++)
            {
                Assert.True(sut.TryAdvance(1));
            }
        }
    }

    [Fact]
    public async Task Threads3WindowUpdates_AssertSize()
    {
        for (var i = 0; i < IterationCount; i++)
        {
            var sizeSum = 0;
            var sut = new InputFlowControl(15000, 10);
            var t1 = Task.Run(() => WindowUpdate(ref sizeSum, sut));
            var t2 = Task.Run(() => WindowUpdate(ref sizeSum, sut));
            var t3 = Task.Run(() => WindowUpdate(ref sizeSum, sut));
            await Task.WhenAll(t1, t2, t3);
            Assert.Equal((uint)15000 + 5000 * 3, sut.Available);
            sut.TryUpdateWindow(11, out var size); // To trigger anything less then 10 included
            sizeSum += size;
            Assert.Equal(5000 * 3 + 11, sizeSum);
        }

        static void WindowUpdate(ref int sizeSum, InputFlowControl sut)
        {
            for (var j = 0; j < 5000; j++)
            {
                Assert.True(sut.TryUpdateWindow(1, out var size));
                Assert.True(size > 10 || size == 0);
                Interlocked.Add(ref sizeSum, size);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests.Http2;

public class QueueWithMovableHeadTests
{
    [Fact]
    public void FIFOWorks()
    {
        var queue = new QueueWithMovableHead<int>();
        queue.Enqueue(1);
        queue.Enqueue(2);

        Assert.True(queue.TryDequeue(out var val1));
        Assert.True(queue.TryDequeue(out var val2));
        Assert.Equal(1, val1);
        Assert.Equal(2, val2);
    }

    [Fact]
    public void SettingTheHeadOfTheQueueWorks()
    {
        var queue = new QueueWithMovableHead<int>();
        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);
        queue.Enqueue(4);

        queue.SetHead(2);

        Assert.True(queue.TryDequeue(out var val1));
        Assert.True(queue.TryDequeue(out var val2));
        Assert.Equal(3, val1);
        Assert.Equal(4, val2);
    }

    [Fact]
    public void SettingTheHeadOfTheQueueAndDrainingItWorks()
    {
        var queue = new QueueWithMovableHead<int>();
        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);
        queue.Enqueue(4);

        queue.SetHead(2);

        Assert.True(queue.TryDequeue(out var val1));
        Assert.True(queue.TryDequeue(out var val2));
        Assert.True(queue.TryDequeue(out var val3));
        Assert.True(queue.TryDequeue(out var val4));
        Assert.False(queue.TryDequeue(out var val5));

        Assert.Equal(3, val1);
        Assert.Equal(4, val2);
        Assert.Equal(1, val3);
        Assert.Equal(2, val4);
    }

    [Fact]
    public void GrowingTheCapacitySettingTheHeadOfTheQueueAndDrainingItWorks()
    {
        var queue = new QueueWithMovableHead<int>();

        for (int i = 0; i < 2; i++)
        {
            for (var j = 0; j < 10; j++)
            {
                queue.Enqueue(j);
            }

            queue.SetHead(5);

            var expected = 5;
            for (var j = 0; j < 10; j++)
            {
                Assert.True(queue.TryDequeue(out var val));
                Assert.Equal(expected, val);
                expected = (expected + 1) % 10;
            }

            Assert.Equal(5, expected);
        }
    }
}

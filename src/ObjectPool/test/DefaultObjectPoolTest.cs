// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.ObjectPool;

public class DefaultObjectPoolTest
{
    [Fact]
    public void DefaultObjectPoolWithDefaultPolicy_GetAnd_ReturnObject_SameInstance()
    {
        // Arrange
        var pool = new DefaultObjectPool<object>(new DefaultPooledObjectPolicy<object>());

        var obj1 = pool.Get();
        pool.Return(obj1);

        // Act
        var obj2 = pool.Get();

        // Assert
        Assert.Same(obj1, obj2);
    }

    [Fact]
    public void DefaultObjectPool_GetAndReturnObject_SameInstance()
    {
        // Arrange
        var pool = new DefaultObjectPool<List<int>>(new ListPolicy());

        var list1 = pool.Get();
        pool.Return(list1);

        // Act
        var list2 = pool.Get();

        // Assert
        Assert.Same(list1, list2);
    }

    [Fact]
    public void DefaultObjectPool_CreatedByPolicy()
    {
        // Arrange
        var pool = new DefaultObjectPool<List<int>>(new ListPolicy());

        // Act
        var list = pool.Get();

        // Assert
        Assert.Equal(17, list.Capacity);
    }

    [Fact]
    public void DefaultObjectPool_Return_RejectedByPolicy()
    {
        // Arrange
        var pool = new DefaultObjectPool<List<int>>(new ListPolicy());
        var list1 = pool.Get();
        list1.Capacity = 20;

        // Act
        pool.Return(list1);
        var list2 = pool.Get();

        // Assert
        Assert.NotSame(list1, list2);
    }

    [Fact]
    public static void DefaultObjectPool_Honors_IResettable()
    {
        var p = new DefaultObjectPool<Resettable>(new DefaultPooledObjectPolicy<Resettable>());
        var r = new Resettable();

        r.ResetReturnValue = false;
        p.Return(r);
        Assert.Equal(1, r.ResetCallCount);
        Assert.NotSame(r, p.Get());

        r.ResetReturnValue = true;
        p.Return(r);
        Assert.Equal(2, r.ResetCallCount);
        Assert.Same(r, p.Get());
    }

    private sealed class Resettable : IResettable
    {
        public int ResetCallCount { get; set; }
        public bool ResetReturnValue { get; set; }
        public bool TryReset()
        {
            ResetCallCount++;
            return ResetReturnValue;
        }
    }

    private class ListPolicy : IPooledObjectPolicy<List<int>>
    {
        public List<int> Create()
        {
            return new List<int>(17);
        }

        public bool Return(List<int> obj)
        {
            return obj.Capacity == 17;
        }
    }
}

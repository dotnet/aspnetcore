// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.RenderTree;

public class StackObjectPoolTest
{
    [Fact]
    public void CanGetInstances()
    {
        // Arrange
        var stackObjectPool = new StackObjectPool<object>(10, () => new object());

        // Act
        var instance1 = stackObjectPool.Get();
        var instance2 = stackObjectPool.Get();

        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.NotSame(instance1, instance2);
    }

    [Fact]
    public void CanReturnInstances()
    {
        // Arrange
        var stackObjectPool = new StackObjectPool<object>(10, () => new object());
        var instance1 = stackObjectPool.Get();
        var instance2 = stackObjectPool.Get();

        // Act/Assert
        // No exception means success
        stackObjectPool.Return(instance2);
        stackObjectPool.Return(instance1);
    }

    [Fact]
    public void ReusesInstancesInPoolUpToCapacity()
    {
        // Arrange
        var stackObjectPool = new StackObjectPool<object>(10, () => new object());
        var instance1 = stackObjectPool.Get();
        var instance2 = stackObjectPool.Get();
        stackObjectPool.Return(instance2);
        stackObjectPool.Return(instance1);

        // Act
        var instance1b = stackObjectPool.Get();
        var instance2b = stackObjectPool.Get();
        var instance3 = stackObjectPool.Get();

        // Assert
        Assert.Same(instance1, instance1b);
        Assert.Same(instance2, instance2b);
        Assert.NotNull(instance3);
        Assert.NotSame(instance1, instance3);
        Assert.NotSame(instance2, instance3);
    }

    [Fact]
    public void SuppliesTransientInstancesWhenExceedingCapacity()
    {
        // Arrange
        var stackObjectPool = new StackObjectPool<object>(1, () => new object());

        // Act 1: Returns distinct instances beyond capacity
        var instance1 = stackObjectPool.Get();
        var instance2 = stackObjectPool.Get();
        var instance3 = stackObjectPool.Get();
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.NotNull(instance3);
        Assert.Equal(3, new[] { instance1, instance2, instance3 }.Distinct().Count());

        // Act 2: Can return all instances, including transient ones
        stackObjectPool.Return(instance3);
        stackObjectPool.Return(instance2);
        stackObjectPool.Return(instance1);

        // Act 3: Reuses only the non-transient instances
        var instance1b = stackObjectPool.Get();
        var instance2b = stackObjectPool.Get();
        Assert.Same(instance1, instance1b);
        Assert.NotSame(instance2b, instance2);
        Assert.Equal(4, new[] { instance1, instance2, instance3, instance2b }.Distinct().Count());
    }

    [Fact]
    public void CannotReturnWhenEmpty()
    {
        // Arrange
        var stackObjectPool = new StackObjectPool<object>(10, () => new object());

        // Act/Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            stackObjectPool.Return(new object());
        });
        Assert.Equal("There are no outstanding instances to return.", ex.Message);
    }

    [Fact]
    public void CannotReturnMismatchingTrackedItem()
    {
        // Arrange
        var stackObjectPool = new StackObjectPool<object>(10, () => new object());
        var instance1 = stackObjectPool.Get();
        var instance2 = stackObjectPool.Get();

        // Act/Assert
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            stackObjectPool.Return(instance1);
        });
        Assert.Equal("Attempting to return wrong pooled instance. Get/Return calls must form a stack.", ex.Message);
    }
}

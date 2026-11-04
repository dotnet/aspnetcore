// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;

namespace Microsoft.AspNetCore.Components.Testing.Tests;

public class TestLockProviderTests
{
    [Fact]
    public async Task WaitOn_CompletesWhenReleased()
    {
        // Arrange
        var provider = new TestLockProvider();
        var key = "test-key";

        // Act
        var waitTask = provider.WaitOn(key);
        Assert.False(waitTask.IsCompleted);

        provider.Release(key);

        // Assert
        await waitTask;
        Assert.True(waitTask.IsCompleted);
    }

    [Fact]
    public async Task WaitOn_SameKey_ReturnsSameTask()
    {
        // Arrange
        var provider = new TestLockProvider();
        var key = "same-key";

        // Act
        var task1 = provider.WaitOn(key);
        var task2 = provider.WaitOn(key);

        // Assert
        Assert.Same(task1, task2);

        provider.Release(key);
        await task1;
    }

    [Fact]
    public async Task Release_BeforeWaitOn_CompletesImmediately()
    {
        // Arrange
        var provider = new TestLockProvider();
        var key = "pre-released";

        // Act — release first, then wait
        var released = provider.Release(key);
        var waitTask = provider.WaitOn(key);

        // Assert
        Assert.True(released);
        Assert.True(waitTask.IsCompleted);
        await waitTask;
    }

    [Fact]
    public void Release_ReturnsTrueOnFirstCall()
    {
        // Arrange
        var provider = new TestLockProvider();
        var key = "release-once";

        // Act & Assert
        Assert.True(provider.Release(key));
    }

    [Fact]
    public void Release_ReturnsFalseOnSubsequentCalls()
    {
        // Arrange
        var provider = new TestLockProvider();
        var key = "release-twice";

        // Act
        provider.Release(key);
        var secondRelease = provider.Release(key);

        // Assert
        Assert.False(secondRelease);
    }

    [Fact]
    public async Task MultipleKeys_AreIndependent()
    {
        // Arrange
        var provider = new TestLockProvider();

        // Act
        var task1 = provider.WaitOn("key-1");
        var task2 = provider.WaitOn("key-2");

        provider.Release("key-1");
        await task1;

        // Assert
        Assert.True(task1.IsCompleted);
        Assert.False(task2.IsCompleted);

        provider.Release("key-2");
        await task2;
        Assert.True(task2.IsCompleted);
    }

    [Fact]
    public async Task ConcurrentWaitAndRelease_WorksCorrectly()
    {
        // Arrange
        var provider = new TestLockProvider();
        var keys = Enumerable.Range(0, 10).Select(i => $"concurrent-{i}").ToArray();

        // Act — start all waits
        var tasks = keys.Select(k => provider.WaitOn(k)).ToArray();

        // Release all concurrently
        await Task.WhenAll(keys.Select(k => Task.Run(() => provider.Release(k))));

        // Assert — all should complete
        await Task.WhenAll(tasks);
        Assert.All(tasks, t => Assert.True(t.IsCompleted));
    }
}

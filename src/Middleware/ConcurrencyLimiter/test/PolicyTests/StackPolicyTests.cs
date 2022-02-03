// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ConcurrencyLimiter.Tests.PolicyTests;

public class StackPolicyTests
{
    [Fact]
    public async Task BaseFunctionality()
    {
        var stack = new StackPolicy(Options.Create(new QueuePolicyOptions
        {
            MaxConcurrentRequests = 1,
            RequestQueueLimit = 2,
        }));

        var task1 = stack.TryEnterAsync();

        Assert.True(task1.IsCompleted);
        Assert.True(await task1);

        var task2 = stack.TryEnterAsync();

        Assert.False(task2.IsCompleted);

        stack.OnExit();

        Assert.True(await task2);

        stack.OnExit();
    }

    [Fact]
    public async Task OldestRequestOverwritten()
    {
        var stack = new StackPolicy(Options.Create(new QueuePolicyOptions
        {
            MaxConcurrentRequests = 1,
            RequestQueueLimit = 3,
        }));

        var task1 = stack.TryEnterAsync();
        Assert.True(task1.IsCompleted);

        var task2 = stack.TryEnterAsync();
        Assert.False(task2.IsCompleted);
        var task3 = stack.TryEnterAsync();
        Assert.False(task3.IsCompleted);
        var task4 = stack.TryEnterAsync();
        Assert.False(task4.IsCompleted);

        var task5 = stack.TryEnterAsync();
        Assert.False(task5.IsCompleted);

        // Should have been pushed out of the stack
        Assert.False(await task2);

        Assert.False(task3.IsCompleted);
        Assert.False(task4.IsCompleted);
    }

    [Fact]
    public void RespectsMaxConcurrency()
    {
        var stack = new StackPolicy(Options.Create(new QueuePolicyOptions
        {
            MaxConcurrentRequests = 2,
            RequestQueueLimit = 2,
        }));

        var task1 = stack.TryEnterAsync();
        Assert.True(task1.IsCompleted);

        var task2 = stack.TryEnterAsync();
        Assert.True(task2.IsCompleted);

        var task3 = stack.TryEnterAsync();
        Assert.False(task3.IsCompleted);
    }

    [Fact]
    public async Task ExitRequestsPreserveSemaphoreState()
    {
        var stack = new StackPolicy(Options.Create(new QueuePolicyOptions
        {
            MaxConcurrentRequests = 1,
            RequestQueueLimit = 2,
        }));

        var task1 = stack.TryEnterAsync();
        Assert.True(task1.IsCompleted && await task1);

        var task2 = stack.TryEnterAsync();
        Assert.False(task2.IsCompleted);

        stack.OnExit();  // t1 exits, should free t2 to return
        Assert.True(await task2);

        stack.OnExit();  // t2 exists, there's now a free spot in server

        var task3 = stack.TryEnterAsync();
        Assert.True(task3.IsCompleted && await task3);
    }

    [Fact]
    public async Task StaleRequestsAreProperlyOverwritten()
    {
        var stack = new StackPolicy(Options.Create(new QueuePolicyOptions
        {
            MaxConcurrentRequests = 1,
            RequestQueueLimit = 4,
        }));

        var task0 = stack.TryEnterAsync();
        Assert.True(task0.IsCompleted && await task0);

        var task1 = stack.TryEnterAsync();
        Assert.False(task1.IsCompleted);

        stack.OnExit();
        Assert.True(await task1);

        var task2 = stack.TryEnterAsync();
        Assert.False(task2.IsCompleted);

        stack.OnExit();
        Assert.True(await task2);

        stack.OnExit();
    }

    [Fact]
    public async Task OneTryEnterAsyncOneOnExit()
    {
        var stack = new StackPolicy(Options.Create(new QueuePolicyOptions
        {
            MaxConcurrentRequests = 1,
            RequestQueueLimit = 4,
        }));

        Assert.Throws<InvalidOperationException>(() => stack.OnExit());

        await stack.TryEnterAsync();

        stack.OnExit();

        Assert.Throws<InvalidOperationException>(() => stack.OnExit());
    }
}

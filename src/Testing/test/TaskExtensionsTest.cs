// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

public class TaskExtensionsTest
{
    [Fact]
    public async Task TimeoutAfterTest()
    {
        var cts = new CancellationTokenSource();
        await Assert.ThrowsAsync<TimeoutException>(async () => await Task.Delay(30000, cts.Token).TimeoutAfter(TimeSpan.FromMilliseconds(50)));
        cts.Cancel();
    }

    [Fact]
    public async Task TimeoutAfter_DoesNotThrowWhenCompleted()
    {
        await Task.FromResult(true).TimeoutAfter(TimeSpan.FromMilliseconds(30000));
    }

    [Fact]
    public async Task TimeoutAfter_DoesNotThrow_WithinTimeoutPeriod()
    {
        await Task.Delay(10).TimeoutAfter(TimeSpan.FromMilliseconds(30000));
    }

    [Fact]
    public async Task DefaultTimeout_WithTimespan()
    {
        var cts = new CancellationTokenSource();
        await Assert.ThrowsAsync<TimeoutException>(async () => await Task.Delay(30000, cts.Token).DefaultTimeout(TimeSpan.FromMilliseconds(50)));
        cts.Cancel();
    }

    [Fact]
    public async Task DefaultTimeout_WithMilliseconds()
    {
        var cts = new CancellationTokenSource();
        await Assert.ThrowsAsync<TimeoutException>(async () => await Task.Delay(30000, cts.Token).DefaultTimeout(50));
        cts.Cancel();
    }

    [Fact]
    public async Task DefaultTimeout_Message_ContainsLineNumber()
    {
        var cts = new CancellationTokenSource();
        await Assert.ThrowsAsync<TimeoutException>(async () => await Task.Delay(30000, cts.Token).DefaultTimeout(50));
        cts.Cancel();
    }

    [Fact]
    public async Task DefaultTimeout_DoesNotThrowWhenCompleted()
    {
        await Task.FromResult(true).DefaultTimeout();
    }

    [Theory]
    [InlineData("This is my custom timeout exception message.")]
    public async Task Task_TimeoutAfter_DoesNotRethrow_NonWaitAsyncTimeouts(string message)
    {
        async Task ExpectedTimeout()
        {
            await Task.Delay(10);
            throw new TimeoutException(message);
        }
        var exception = await Assert.ThrowsAsync<TimeoutException>(() => ExpectedTimeout().TimeoutAfter(TimeSpan.FromMilliseconds(30000)));
        Assert.Equal(message, exception.Message);
    }

    [Theory]
    [InlineData("This is my custom timeout exception message.")]
    public async Task TaskT_TimeoutAfter_DoesNotRethrow_NonWaitAsyncTimeouts(string message)
    {
        async Task<bool> ExpectedTimeout()
        {
            await Task.Delay(10);
            throw new TimeoutException(message);
        }
        var exception = await Assert.ThrowsAsync<TimeoutException>(() => ExpectedTimeout().TimeoutAfter(TimeSpan.FromMilliseconds(30000)));
        Assert.Equal(message, exception.Message);
    }

}

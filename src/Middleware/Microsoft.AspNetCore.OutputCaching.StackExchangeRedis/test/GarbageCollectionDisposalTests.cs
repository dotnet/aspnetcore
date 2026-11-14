// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.OutputCaching.StackExchangeRedis.Tests;

public class GarbageCollectionDisposalTests
{
    [Fact]
    public async Task Dispose_InterruptsPendingGarbageCollectionDelay()
    {
        var logger = new Mock<ILogger>();
        logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        using var store = new RedisOutputCacheStore(Options.Create(new RedisOutputCacheOptions()), logger.Object);
        var method = typeof(RedisOutputCacheStore).GetMethod("RunGarbageCollectionLoopAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var loop = (Task)method.Invoke(store, null)!;
        Assert.False(loop.IsCompleted);

        store.Dispose();

        await loop.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.DoesNotContain(logger.Invocations, invocation =>
            invocation.Method.Name == nameof(ILogger.Log) && (LogLevel)invocation.Arguments[0] >= LogLevel.Error);
    }
}

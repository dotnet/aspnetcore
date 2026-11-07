// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.OutputCaching.StackExchangeRedis.Tests;

public class RedisOutputCacheStoreTests
{
    [Fact]
    public async Task Dispose_EndsGarbageCollectionLoop()
    {
        var logger = new Mock<ILogger>();
        logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var store = new RedisOutputCacheStore(Options.Create(new RedisOutputCacheOptions()), logger.Object);

        // the loop is waiting for its next sweep, which is minutes away
        Assert.False(store.GarbageCollectionLoop.IsCompleted);

        store.Dispose();

        // without observing disposal, the loop (and everything it references) lives on until the delay elapses
        await store.GarbageCollectionLoop.WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Equal(TaskStatus.RanToCompletion, store.GarbageCollectionLoop.Status);
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }
}

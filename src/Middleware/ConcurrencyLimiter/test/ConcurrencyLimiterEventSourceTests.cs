// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Diagnostics.Tracing;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.ConcurrencyLimiter.Tests;

public class ConcurrencyLimiterEventSourceTests
{
    [Fact]
    public void MatchesNameAndGuid()
    {
        var eventSource = new ConcurrencyLimiterEventSource();

        Assert.Equal("Microsoft.AspNetCore.ConcurrencyLimiter", eventSource.Name);
        Assert.Equal(Guid.Parse("a605548a-6963-55cf-f000-99a6013deb01", CultureInfo.InvariantCulture), eventSource.Guid);
    }

    [Fact]
    public void RecordsRequestsRejected()
    {
        // Arrange
        var expectedId = 1;
        var eventListener = new TestEventListener(expectedId);
        var eventSource = GetConcurrencyLimiterEventSource();
        eventListener.EnableEvents(eventSource, EventLevel.Informational);

        // Act
        eventSource.RequestRejected();

        // Assert
        var eventData = eventListener.EventData;
        Assert.NotNull(eventData);
        Assert.Equal(expectedId, eventData.EventId);
        Assert.Equal(EventLevel.Warning, eventData.Level);
        Assert.Same(eventSource, eventData.EventSource);
        Assert.Null(eventData.Message);
        Assert.Empty(eventData.Payload);
    }

    [Fact]
    public async Task TracksQueueLength()
    {
        // Arrange
        using var eventListener = new TestCounterListener(new[] {
                "queue-length",
                "queue-duration",
                "requests-rejected",
            });

        using var eventSource = GetConcurrencyLimiterEventSource();

        using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var lengthValues = eventListener.GetCounterValues("queue-length", timeoutTokenSource.Token).GetAsyncEnumerator();

        eventListener.EnableEvents(eventSource, EventLevel.Informational, EventKeywords.None,
            new Dictionary<string, string>
            {
                    {"EventCounterIntervalSec", ".1" }
            });

        // Act
        eventSource.RequestRejected();

        Assert.True(await UntilValueMatches(lengthValues, 0));
        using (eventSource.QueueTimer())
        {
            Assert.True(await UntilValueMatches(lengthValues, 1));

            using (eventSource.QueueTimer())
            {
                Assert.True(await UntilValueMatches(lengthValues, 2));
            }

            Assert.True(await UntilValueMatches(lengthValues, 1));
        }

        Assert.True(await UntilValueMatches(lengthValues, 0));
    }

    [Fact]
    public async Task TracksDurationSpentInQueue()
    {
        // Arrange
        using var eventListener = new TestCounterListener(new[] {
                "queue-length",
                "queue-duration",
                "requests-rejected",
            });

        using var eventSource = GetConcurrencyLimiterEventSource();

        using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var durationValues = eventListener.GetCounterValues("queue-duration", timeoutTokenSource.Token).GetAsyncEnumerator();

        eventListener.EnableEvents(eventSource, EventLevel.Informational, EventKeywords.None,
            new Dictionary<string, string>
            {
                    {"EventCounterIntervalSec", ".1" }
            });

        // Act
        Assert.True(await UntilValueMatches(durationValues, 0));

        using (eventSource.QueueTimer())
        {
            Assert.True(await UntilValueMatches(durationValues, 0));
        }

        // check that something (anything!) has been written
        while (await durationValues.MoveNextAsync())
        {
            if (durationValues.Current > 0)
            {
                return;
            }
        }

        throw new TimeoutException();
    }

    private async Task<bool> UntilValueMatches(IAsyncEnumerator<double> enumerator, int value)
    {
        while (await enumerator.MoveNextAsync())
        {
            if (enumerator.Current == value)
            {
                return true;
            }
        }

        return false;
    }

    private static ConcurrencyLimiterEventSource GetConcurrencyLimiterEventSource()
    {
        return new ConcurrencyLimiterEventSource(Guid.NewGuid().ToString());
    }
}

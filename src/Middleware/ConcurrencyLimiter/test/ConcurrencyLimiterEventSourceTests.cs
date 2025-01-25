// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Diagnostics.Tracing;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.ConcurrencyLimiter.Tests;

public class ConcurrencyLimiterEventSourceTests : LoggedTest
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
        using var eventSource = GetConcurrencyLimiterEventSource();

        using var eventListener = new TestCounterListener(LoggerFactory, eventSource.Name, [
            "queue-length",
            "queue-duration",
            "requests-rejected",
        ]);

        using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var lengthValues = eventListener.GetCounterValues("queue-length", timeoutTokenSource.Token);

        eventListener.EnableEvents(eventSource, EventLevel.Informational, EventKeywords.None,
            new Dictionary<string, string>
            {
                {"EventCounterIntervalSec", ".1" }
            });

        // Act
        eventSource.RequestRejected();

        await WaitForCounterValue(lengthValues, expectedValue: 0, Logger);
        using (eventSource.QueueTimer())
        {
            await WaitForCounterValue(lengthValues, expectedValue: 1, Logger);

            using (eventSource.QueueTimer())
            {
                await WaitForCounterValue(lengthValues, expectedValue: 2, Logger);
            }

            await WaitForCounterValue(lengthValues, expectedValue: 1, Logger);
        }

        await WaitForCounterValue(lengthValues, expectedValue: 0, Logger);
    }

    [Fact]
    public async Task TracksDurationSpentInQueue()
    {
        // Arrange
        using var eventSource = GetConcurrencyLimiterEventSource();

        using var eventListener = new TestCounterListener(LoggerFactory, eventSource.Name, [
            "queue-length",
            "queue-duration",
            "requests-rejected",
        ]);

        using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var durationValues = eventListener.GetCounterValues("queue-duration", timeoutTokenSource.Token);

        eventListener.EnableEvents(eventSource, EventLevel.Informational, EventKeywords.None,
            new Dictionary<string, string>
            {
                {"EventCounterIntervalSec", ".1" }
            });

        // Act
        await WaitForCounterValue(durationValues, expectedValue: 0, Logger);

        using (eventSource.QueueTimer())
        {
            await WaitForCounterValue(durationValues, expectedValue: 0, Logger);
        }

        // check that something (anything!) has been written
        while (await durationValues.Values.MoveNextAsync())
        {
            if (durationValues.Values.Current > 0)
            {
                return;
            }
        }

        throw new TimeoutException();
    }

    private static async Task WaitForCounterValue(CounterValues values, double expectedValue, ILogger logger)
    {
        await values.Values.WaitForValueAsync(expectedValue, values.CounterName, logger);
    }

    private static ConcurrencyLimiterEventSource GetConcurrencyLimiterEventSource()
    {
        return new ConcurrencyLimiterEventSource(Guid.NewGuid().ToString());
    }
}

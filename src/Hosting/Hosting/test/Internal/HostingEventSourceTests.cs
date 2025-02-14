// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting;

public class HostingEventSourceTests : LoggedTest
{
    [Fact]
    public void MatchesNameAndGuid()
    {
        // Arrange & Act
        var eventSource = new HostingEventSource();

        // Assert
        Assert.Equal("Microsoft.AspNetCore.Hosting", eventSource.Name);
        Assert.Equal(Guid.Parse("9ded64a4-414c-5251-dcf7-1e4e20c15e70", CultureInfo.InvariantCulture), eventSource.Guid);
    }

    [Fact]
    public void HostStart()
    {
        // Arrange
        var expectedEventId = 1;
        var eventListener = new TestEventListener(expectedEventId);
        var hostingEventSource = GetHostingEventSource();
        eventListener.EnableEvents(hostingEventSource, EventLevel.Informational);

        // Act
        hostingEventSource.HostStart();

        // Assert
        var eventData = eventListener.EventData;
        Assert.NotNull(eventData);
        Assert.Equal(expectedEventId, eventData.EventId);
        Assert.Equal("HostStart", eventData.EventName);
        Assert.Equal(EventLevel.Informational, eventData.Level);
        Assert.Same(hostingEventSource, eventData.EventSource);
        Assert.Null(eventData.Message);
        Assert.Empty(eventData.Payload);
    }

    [Fact]
    public void HostStop()
    {
        // Arrange
        var expectedEventId = 2;
        var eventListener = new TestEventListener(expectedEventId);
        var hostingEventSource = GetHostingEventSource();
        eventListener.EnableEvents(hostingEventSource, EventLevel.Informational);

        // Act
        hostingEventSource.HostStop();

        // Assert
        var eventData = eventListener.EventData;
        Assert.NotNull(eventData);
        Assert.Equal(expectedEventId, eventData.EventId);
        Assert.Equal("HostStop", eventData.EventName);
        Assert.Equal(EventLevel.Informational, eventData.Level);
        Assert.Same(hostingEventSource, eventData.EventSource);
        Assert.Null(eventData.Message);
        Assert.Empty(eventData.Payload);
    }

    public static TheoryData<DefaultHttpContext, string[]> RequestStartData
    {
        get
        {
            var variations = new TheoryData<DefaultHttpContext, string[]>();

            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/Home/Index";
            variations.Add(
                context,
                new string[]
                {
                    "GET",
                    "/Home/Index"
                });

            context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = "/";
            variations.Add(
                context,
                new string[]
                {
                    "POST",
                    "/"
                });

            return variations;
        }
    }

    [Theory]
    [MemberData(nameof(RequestStartData))]
    public void RequestStart(DefaultHttpContext httpContext, string[] expected)
    {
        // Arrange
        var expectedEventId = 3;
        var eventListener = new TestEventListener(expectedEventId);
        var hostingEventSource = GetHostingEventSource();
        eventListener.EnableEvents(hostingEventSource, EventLevel.Informational);

        // Act
        hostingEventSource.RequestStart(httpContext.Request.Method, httpContext.Request.Path);

        // Assert
        var eventData = eventListener.EventData;
        Assert.NotNull(eventData);
        Assert.Equal(expectedEventId, eventData.EventId);
        Assert.Equal("RequestStart", eventData.EventName);
        Assert.Equal(EventLevel.Informational, eventData.Level);
        Assert.Same(hostingEventSource, eventData.EventSource);
        Assert.Null(eventData.Message);

        var payloadList = eventData.Payload;
        Assert.Equal(expected.Length, payloadList.Count);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], payloadList[i]);
        }
    }

    [Fact]
    public void RequestStop()
    {
        // Arrange
        var expectedEventId = 4;
        var eventListener = new TestEventListener(expectedEventId);
        var hostingEventSource = GetHostingEventSource();
        eventListener.EnableEvents(hostingEventSource, EventLevel.Informational);

        // Act
        hostingEventSource.RequestStop();

        // Assert
        var eventData = eventListener.EventData;
        Assert.Equal(expectedEventId, eventData.EventId);
        Assert.Equal("RequestStop", eventData.EventName);
        Assert.Equal(EventLevel.Informational, eventData.Level);
        Assert.Same(hostingEventSource, eventData.EventSource);
        Assert.Null(eventData.Message);
        Assert.Empty(eventData.Payload);
    }

    [Fact]
    public void UnhandledException()
    {
        // Arrange
        var expectedEventId = 5;
        var eventListener = new TestEventListener(expectedEventId);
        var hostingEventSource = GetHostingEventSource();
        eventListener.EnableEvents(hostingEventSource, EventLevel.Informational);

        // Act
        hostingEventSource.UnhandledException();

        // Assert
        var eventData = eventListener.EventData;
        Assert.Equal(expectedEventId, eventData.EventId);
        Assert.Equal("UnhandledException", eventData.EventName);
        Assert.Equal(EventLevel.Error, eventData.Level);
        Assert.Same(hostingEventSource, eventData.EventSource);
        Assert.Null(eventData.Message);
        Assert.Empty(eventData.Payload);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/57259")]
    public async Task VerifyCountersFireWithCorrectValues()
    {
        // Arrange
        var hostingEventSource = GetHostingEventSource();

        // requests-per-second isn't tested because the value can't be reliably tested because of time
        using var eventListener = new TestCounterListener(LoggerFactory, hostingEventSource.Name,
        [
            "total-requests",
            "current-requests",
            "failed-requests"
        ]);

        using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        timeoutTokenSource.Token.Register(() => Logger.LogError("Timeout while waiting for counter value."));

        var totalRequestValues = eventListener.GetCounterValues("total-requests", timeoutTokenSource.Token);
        var currentRequestValues = eventListener.GetCounterValues("current-requests", timeoutTokenSource.Token);
        var failedRequestValues = eventListener.GetCounterValues("failed-requests", timeoutTokenSource.Token);

        eventListener.EnableEvents(hostingEventSource, EventLevel.Informational, EventKeywords.None,
            new Dictionary<string, string>
            {
                { "EventCounterIntervalSec", "1" }
            });

        // Act & Assert
        Logger.LogInformation(nameof(HostingEventSource.RequestStart));
        hostingEventSource.RequestStart("GET", "/");

        await WaitForCounterValue(totalRequestValues, expectedValue: 1, Logger);
        await WaitForCounterValue(currentRequestValues, expectedValue: 1, Logger);
        await WaitForCounterValue(failedRequestValues, expectedValue: 0, Logger);

        Logger.LogInformation(nameof(HostingEventSource.RequestStop));
        hostingEventSource.RequestStop();

        await WaitForCounterValue(totalRequestValues, expectedValue: 1, Logger);
        await WaitForCounterValue(currentRequestValues, expectedValue: 0, Logger);
        await WaitForCounterValue(failedRequestValues, expectedValue: 0, Logger);

        Logger.LogInformation(nameof(HostingEventSource.RequestStart));
        hostingEventSource.RequestStart("POST", "/");

        await WaitForCounterValue(totalRequestValues, expectedValue: 2, Logger);
        await WaitForCounterValue(currentRequestValues, expectedValue: 1, Logger);
        await WaitForCounterValue(failedRequestValues, expectedValue: 0, Logger);

        Logger.LogInformation(nameof(HostingEventSource.RequestFailed));
        hostingEventSource.RequestFailed();
        Logger.LogInformation(nameof(HostingEventSource.RequestStop));
        hostingEventSource.RequestStop();

        await WaitForCounterValue(totalRequestValues, expectedValue: 2, Logger);
        await WaitForCounterValue(currentRequestValues, expectedValue: 0, Logger);
        await WaitForCounterValue(failedRequestValues, expectedValue: 1, Logger);
    }

    private static async Task WaitForCounterValue(CounterValues values, double expectedValue, ILogger logger)
    {
        await values.Values.WaitForValueAsync(expectedValue, values.CounterName, logger);
    }

    private static HostingEventSource GetHostingEventSource()
    {
        return new HostingEventSource(Guid.NewGuid().ToString());
    }
}

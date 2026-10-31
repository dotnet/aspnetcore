// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class DateHeaderValueManagerTests
{
    /// <summary>
    /// DateTime format string for RFC1123.
    /// </summary>
    /// <remarks>
    /// See https://msdn.microsoft.com/en-us/library/az4se3k1(v=vs.110).aspx#RFC1123 for info on the format.
    /// </remarks>
    private const string Rfc1123DateFormat = "r";

    [Fact]
    public void GetDateHeaderValue_ReturnsDateValueInRFC1123Format()
    {
        var timeProvider = new FakeTimeProvider();
        var now = timeProvider.GetUtcNow();

        var dateHeaderValueManager = new DateHeaderValueManager(timeProvider);
        dateHeaderValueManager.OnHeartbeat();

        Assert.Equal(now.ToString(Rfc1123DateFormat), dateHeaderValueManager.GetDateHeaderValues().String);
    }

    [Fact]
    public void GetDateHeaderValue_ReturnsCachedValueBetweenTimerTicks()
    {
        var timeProvider = new FakeTimeProvider();
        var now = timeProvider.GetUtcNow();

        var dateHeaderValueManager = new DateHeaderValueManager(timeProvider);
        dateHeaderValueManager.OnHeartbeat();

        var testKestrelTrace = new KestrelTrace(NullLoggerFactory.Instance);

        using (var heartbeat = new Heartbeat(new IHeartbeatHandler[] { dateHeaderValueManager }, timeProvider, DebuggerWrapper.Singleton, testKestrelTrace, Heartbeat.Interval))
        {
            Assert.Equal(now.ToString(Rfc1123DateFormat), dateHeaderValueManager.GetDateHeaderValues().String);
            timeProvider.Advance(TimeSpan.FromSeconds(10));
            Assert.Equal(now.ToString(Rfc1123DateFormat), dateHeaderValueManager.GetDateHeaderValues().String);
        }
    }

    [Fact]
    public void GetDateHeaderValue_ReturnsUpdatedValueAfterHeartbeat()
    {
        var timeProvider = new FakeTimeProvider();
        var now = timeProvider.GetUtcNow();
        var future = now.AddSeconds(10);

        var dateHeaderValueManager = new DateHeaderValueManager(timeProvider);
        dateHeaderValueManager.OnHeartbeat();

        var testKestrelTrace = new KestrelTrace(NullLoggerFactory.Instance);

        var mockHeartbeatHandler = new Mock<IHeartbeatHandler>();

        using (var heartbeat = new Heartbeat(new[] { dateHeaderValueManager, mockHeartbeatHandler.Object }, timeProvider, DebuggerWrapper.Singleton, testKestrelTrace, Heartbeat.Interval))
        {
            heartbeat.OnHeartbeat();

            Assert.Equal(now.ToString(Rfc1123DateFormat), dateHeaderValueManager.GetDateHeaderValues().String);

            // Wait for the next heartbeat before verifying GetDateHeaderValues picks up new time.
            timeProvider.SetUtcNow(future);

            heartbeat.OnHeartbeat();

            Assert.Equal(future.ToString(Rfc1123DateFormat), dateHeaderValueManager.GetDateHeaderValues().String);
        }
    }

    [Fact]
    public void GetDateHeaderValue_ReturnsLastDateValueAfterHeartbeatDisposed()
    {
        var timeProvider = new FakeTimeProvider();
        var now = timeProvider.GetUtcNow();

        var dateHeaderValueManager = new DateHeaderValueManager(timeProvider);
        dateHeaderValueManager.OnHeartbeat();

        var testKestrelTrace = new KestrelTrace(NullLoggerFactory.Instance);

        using (var heartbeat = new Heartbeat(new IHeartbeatHandler[] { dateHeaderValueManager }, timeProvider, DebuggerWrapper.Singleton, testKestrelTrace, Heartbeat.Interval))
        {
            heartbeat.OnHeartbeat();
            Assert.Equal(now.ToString(Rfc1123DateFormat), dateHeaderValueManager.GetDateHeaderValues().String);
        }

        timeProvider.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(now.ToString(Rfc1123DateFormat), dateHeaderValueManager.GetDateHeaderValues().String);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

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
        var now = DateTimeOffset.UtcNow;

        var dateHeaderValueManager = new DateHeaderValueManager();
        dateHeaderValueManager.OnHeartbeat(now);

        Assert.Equal(now.ToString(Rfc1123DateFormat), dateHeaderValueManager.GetDateHeaderValues().String);
    }

    [Fact]
    public void GetDateHeaderValue_ReturnsCachedValueBetweenTimerTicks()
    {
        var now = DateTimeOffset.UtcNow;
        var future = now.AddSeconds(10);
        var systemClock = new MockSystemClock
        {
            UtcNow = now
        };

        var dateHeaderValueManager = new DateHeaderValueManager();
        dateHeaderValueManager.OnHeartbeat(now);

        var testKestrelTrace = new KestrelTrace(NullLoggerFactory.Instance);

        using (var heartbeat = new Heartbeat(new IHeartbeatHandler[] { dateHeaderValueManager }, systemClock, DebuggerWrapper.Singleton, testKestrelTrace))
        {
            Assert.Equal(now.ToString(Rfc1123DateFormat), dateHeaderValueManager.GetDateHeaderValues().String);
            systemClock.UtcNow = future;
            Assert.Equal(now.ToString(Rfc1123DateFormat), dateHeaderValueManager.GetDateHeaderValues().String);
        }

        Assert.Equal(0, systemClock.UtcNowCalled);
    }

    [Fact]
    public void GetDateHeaderValue_ReturnsUpdatedValueAfterHeartbeat()
    {
        var now = DateTimeOffset.UtcNow;
        var future = now.AddSeconds(10);
        var systemClock = new MockSystemClock
        {
            UtcNow = now
        };

        var dateHeaderValueManager = new DateHeaderValueManager();
        dateHeaderValueManager.OnHeartbeat(now);

        var testKestrelTrace = new KestrelTrace(NullLoggerFactory.Instance);

        var mockHeartbeatHandler = new Mock<IHeartbeatHandler>();

        using (var heartbeat = new Heartbeat(new[] { dateHeaderValueManager, mockHeartbeatHandler.Object }, systemClock, DebuggerWrapper.Singleton, testKestrelTrace))
        {
            heartbeat.OnHeartbeat();

            Assert.Equal(now.ToString(Rfc1123DateFormat), dateHeaderValueManager.GetDateHeaderValues().String);

            // Wait for the next heartbeat before verifying GetDateHeaderValues picks up new time.
            systemClock.UtcNow = future;

            heartbeat.OnHeartbeat();

            Assert.Equal(future.ToString(Rfc1123DateFormat), dateHeaderValueManager.GetDateHeaderValues().String);
            Assert.Equal(4, systemClock.UtcNowCalled);
        }
    }

    [Fact]
    public void GetDateHeaderValue_ReturnsLastDateValueAfterHeartbeatDisposed()
    {
        var now = DateTimeOffset.UtcNow;
        var future = now.AddSeconds(10);
        var systemClock = new MockSystemClock
        {
            UtcNow = now
        };

        var dateHeaderValueManager = new DateHeaderValueManager();
        dateHeaderValueManager.OnHeartbeat(now);

        var testKestrelTrace = new KestrelTrace(NullLoggerFactory.Instance);

        using (var heartbeat = new Heartbeat(new IHeartbeatHandler[] { dateHeaderValueManager }, systemClock, DebuggerWrapper.Singleton, testKestrelTrace))
        {
            heartbeat.OnHeartbeat();
            Assert.Equal(now.ToString(Rfc1123DateFormat), dateHeaderValueManager.GetDateHeaderValues().String);
        }

        systemClock.UtcNow = future;
        Assert.Equal(now.ToString(Rfc1123DateFormat), dateHeaderValueManager.GetDateHeaderValues().String);
    }
}

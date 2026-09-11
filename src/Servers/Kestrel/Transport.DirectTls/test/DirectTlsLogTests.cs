// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Tests;

[Microsoft.Extensions.Logging.Testing.LogLevel(LogLevel.Trace)]
public class DirectTlsLogTests : LoggedTest
{
    [Fact]
    public void ConnectionLifecycleEventsMatchSocketsTransport()
    {
        var logger = LoggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls");
        const string connectionId = "connection-id";

        DirectTlsLog.ConnectionPause(logger, connectionId);
        DirectTlsLog.ConnectionResume(logger, connectionId);
        DirectTlsLog.ConnectionReadFin(logger, connectionId);
        DirectTlsLog.ConnectionWriteFin(logger, connectionId, "fin");
        DirectTlsLog.ConnectionWriteRst(logger, connectionId, "rst");
        DirectTlsLog.ConnectionError(logger, connectionId, new IOException("error"));
        DirectTlsLog.ConnectionReset(logger, connectionId);

        Assert.Collection(
            TestSink.Writes,
            write => AssertEvent(write.EventId, 4, "ConnectionPause"),
            write => AssertEvent(write.EventId, 5, "ConnectionResume"),
            write => AssertEvent(write.EventId, 6, "ConnectionReadFin"),
            write => AssertEvent(write.EventId, 7, "ConnectionWriteFin"),
            write => AssertEvent(write.EventId, 8, "ConnectionWriteRst"),
            write => AssertEvent(write.EventId, 14, "ConnectionError"),
            write => AssertEvent(write.EventId, 19, "ConnectionReset"));
    }

    [Fact]
    public void EpollEventsHaveExpectedIds()
    {
        var logger = LoggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls");
        const string connectionId = "connection-id";

        DirectTlsLog.EpollConnectionRegistered(logger, connectionId, pumpId: 3, fileDescriptor: 42, events: 1);
        DirectTlsLog.EpollInterestChanged(logger, connectionId, pumpId: 3, fileDescriptor: 42, previousEvents: 1, events: 4);
        DirectTlsLog.EpollConnectionUnregistered(logger, connectionId, pumpId: 3, fileDescriptor: 42, events: 4);

        Assert.Collection(
            TestSink.Writes,
            write => AssertEvent(write.EventId, 20, "EpollConnectionRegistered"),
            write => AssertEvent(write.EventId, 21, "EpollInterestChanged"),
            write => AssertEvent(write.EventId, 22, "EpollConnectionUnregistered"));
    }

    private static void AssertEvent(EventId eventId, int expectedId, string expectedName)
    {
        Assert.Equal(expectedId, eventId.Id);
        Assert.Equal(expectedName, eventId.Name);
    }
}

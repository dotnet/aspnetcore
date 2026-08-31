// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Tests;

[Microsoft.Extensions.Logging.Testing.LogLevel(LogLevel.Trace)]
public class DirectTlsEpollTelemetryTests : LoggedTest
{
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void EstablishedConnection_LogsEpollStateTransitions()
    {
        using var pump = new RecordingPump(LoggerFactory.CreateLogger<TlsEventPump>(), id: 3);
        var connection = new ConnectionIoState(
            fd: 42,
            session: null!,
            NullLogger<ConnectionIoState>.Instance);
        connection.SetConnectionId("connection-id");

        pump.TrackConnectionForTest(connection.Fd, connection);
        Assert.True(pump.ModifyEvents(connection.Fd, NativeTls.EPOLLOUT));
        pump.Unregister(connection.Fd);

        Assert.Collection(
            TestSink.Writes,
            write => Assert.Equal("EpollConnectionRegistered", write.EventId.Name),
            write => Assert.Equal("EpollInterestChanged", write.EventId.Name),
            write => Assert.Equal("EpollConnectionUnregistered", write.EventId.Name));
    }

    private sealed class RecordingPump : TlsEventPump
    {
        public RecordingPump(ILogger<TlsEventPump> logger, int id)
            : base(logger, id, Timeout.InfiniteTimeSpan)
        {
        }

        private protected override bool TryModifyEstablishedInterest(int fd, uint events)
            => true;

        internal override void DeregisterFromEpoll(int fd)
        {
        }
    }
}

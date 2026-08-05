// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Unit tests for the handshake-timeout sweep on <see cref="TlsEventPump"/>. These exercise the deadline
/// math (<see cref="TlsEventPump.ComputeHandshakeDeadline"/>) and the sweep
/// (<see cref="TlsEventPump.SweepExpiredHandshakes"/>) without a live socket, epoll registration, a real TLS
/// session, or the pump thread, by seeding synthetic handshaking entries and stubbing the native teardown.
/// The pump constructor creates a real epoll fd (Linux), matching the rest of the DirectTls suite.
/// </summary>
public class TlsEventPumpHandshakeTimeoutTests
{
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void ComputeHandshakeDeadline_FiniteTimeout_ReturnsNowPlusTimeout()
    {
        using var pump = new RecordingDropPump(TimeSpan.FromSeconds(10));

        Assert.Equal(1_000 + 10_000, pump.ComputeHandshakeDeadline(1_000));
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void ComputeHandshakeDeadline_InfiniteTimeout_ReturnsMaxValue()
    {
        using var pump = new RecordingDropPump(Timeout.InfiniteTimeSpan);

        Assert.Equal(long.MaxValue, pump.ComputeHandshakeDeadline(1_000));
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void ComputeHandshakeDeadline_MaxValueTimeout_TreatedAsInfinite()
    {
        // The options setter stores Timeout.InfiniteTimeSpan as TimeSpan.MaxValue; the pump must treat that
        // the same as infinite (never expire), not overflow into a real deadline.
        using var pump = new RecordingDropPump(TimeSpan.MaxValue);

        Assert.Equal(long.MaxValue, pump.ComputeHandshakeDeadline(1_000));
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void SweepExpiredHandshakes_DropsExpired_KeepsPending()
    {
        using var pump = new RecordingDropPump(TimeSpan.FromSeconds(10));

        pump.Seed(fd: 101, deadline: 900);   // expired (deadline < now)
        pump.Seed(fd: 102, deadline: 1_000); // expired (deadline == now)
        pump.Seed(fd: 103, deadline: 1_500); // still pending

        var dropped = pump.SweepExpiredHandshakes(nowTimestamp: 1_000);

        Assert.Equal(2, dropped);
        Assert.Equal(new[] { 101, 102 }, pump.DroppedFds.Order());
        Assert.False(pump.IsHandshaking(101));
        Assert.False(pump.IsHandshaking(102));
        Assert.True(pump.IsHandshaking(103));
        Assert.Single(pump.Handshakes);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void SweepExpiredHandshakes_NoneExpired_DropsNothing()
    {
        using var pump = new RecordingDropPump(TimeSpan.FromSeconds(10));

        pump.Seed(fd: 201, deadline: 2_000);
        pump.Seed(fd: 202, deadline: 3_000);

        var dropped = pump.SweepExpiredHandshakes(nowTimestamp: 1_000);

        Assert.Equal(0, dropped);
        Assert.Empty(pump.DroppedFds);
        Assert.Equal(2, pump.Handshakes.Count);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void SweepExpiredHandshakes_InfiniteDeadline_NeverDropped_EvenAtMaxNow()
    {
        using var pump = new RecordingDropPump(TimeSpan.FromSeconds(10));

        // A long.MaxValue deadline means "timeout disabled for this connection" and must never be swept,
        // even when the clock (nowTimestamp) is itself long.MaxValue.
        pump.Seed(fd: 301, deadline: long.MaxValue);

        var dropped = pump.SweepExpiredHandshakes(nowTimestamp: long.MaxValue);

        Assert.Equal(0, dropped);
        Assert.True(pump.IsHandshaking(301));
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void SweepExpiredHandshakes_EmptySet_ReturnsZero()
    {
        using var pump = new RecordingDropPump(TimeSpan.FromSeconds(10));

        Assert.Equal(0, pump.SweepExpiredHandshakes(nowTimestamp: long.MaxValue));
    }

    /// <summary>
    /// A <see cref="TlsEventPump"/> that overrides only the native teardown (<c>ReleaseHandshakeResources</c>)
    /// to record dropped fds instead of touching a real TLS session or socket. The base
    /// <c>DropHandshake</c> still runs, so the dictionary removal and epoll de-registration paths are
    /// exercised for real.
    /// </summary>
    private sealed class RecordingDropPump : TlsEventPump
    {
        public List<int> DroppedFds { get; } = new();

        public RecordingDropPump(TimeSpan handshakeTimeout)
            : base(tlsPumpLogger: NullLogger<TlsEventPump>.Instance, id: 0, handshakeTimeout: handshakeTimeout)
        {
        }

        private protected override void ReleaseHandshakeResources(in HandshakingConnection conn)
        {
            DroppedFds.Add(conn.Fd);
        }

        public void Seed(int fd, long deadline)
            => Handshakes[fd] = new HandshakingConnection { Fd = fd, HandshakeDeadlineTimestamp = deadline };
    }
}

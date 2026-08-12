// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Net.Security;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Regression tests for the pump's in-progress handshake epoll interest transitions
/// (<see cref="TlsEventPump.ApplyInProgressHandshakeInterest"/>). Handshaking sockets are registered
/// level-triggered, so EPOLLOUT may only stay armed while there is pending handshake output the socket send
/// buffer could not accept (<see cref="TlsOperationStatus.DestinationTooSmall"/>). Once the handshake goes
/// back to waiting on the peer (<see cref="TlsOperationStatus.NeedMoreData"/>) the writable interest must be
/// cleared, otherwise <c>epoll_wait</c> keeps returning the writable socket continuously and tight-spins the
/// pump thread at 100% CPU for the rest of that handshake. These tests drive the transition directly and
/// assert the exact interest masks issued, without a live epoll instance or TLS session.
/// Linux-only: constructing a <see cref="TlsEventPump"/> calls <c>epoll_create1</c>.
/// </summary>
public class TlsEventPumpHandshakeInterestTests
{
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void DestinationTooSmall_ArmsWritableInterest()
    {
        using var pump = new RecordingInterestPump();
        const int fd = 10;
        pump.Seed(fd);
        var conn = pump.Handshakes[fd];

        pump.ApplyInProgressHandshakeInterest(fd, ref conn, TlsOperationStatus.DestinationTooSmall);

        var mask = Assert.Single(pump.InterestMasks);
        Assert.NotEqual(0u, mask & NativeTls.EPOLLOUT);
        Assert.NotEqual(0u, mask & NativeTls.EPOLLIN);
        var expected = NativeTls.EPOLLIN | NativeTls.EPOLLOUT | NativeTls.EPOLLRDHUP;
        Assert.Equal(expected, conn.CurrentEpollInterest);
        Assert.Equal(expected, pump.Handshakes[fd].CurrentEpollInterest);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void NeedMoreData_AfterDestinationTooSmall_ClearsWritableInterest()
    {
        // The core busy-spin regression: a step flushed output under EPOLLOUT (DestinationTooSmall) and the
        // next step went back to waiting on the peer (NeedMoreData). EPOLLOUT must be dropped so the
        // level-triggered writable socket stops waking the pump.
        using var pump = new RecordingInterestPump();
        const int fd = 11;
        pump.Seed(fd);
        var conn = pump.Handshakes[fd];

        pump.ApplyInProgressHandshakeInterest(fd, ref conn, TlsOperationStatus.DestinationTooSmall);
        pump.ApplyInProgressHandshakeInterest(fd, ref conn, TlsOperationStatus.NeedMoreData);

        Assert.Equal(2, pump.InterestMasks.Count);
        var clearMask = pump.InterestMasks[1];
        Assert.Equal(0u, clearMask & NativeTls.EPOLLOUT);
        Assert.NotEqual(0u, clearMask & NativeTls.EPOLLIN);
        var expected = NativeTls.EPOLLIN | NativeTls.EPOLLRDHUP;
        Assert.Equal(expected, conn.CurrentEpollInterest);
        Assert.Equal(expected, pump.Handshakes[fd].CurrentEpollInterest);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void NeedMoreData_WithoutPriorFlush_IssuesNoSyscall()
    {
        // A fresh handshake is registered EPOLLIN-only, so the common NeedMoreData step (waiting for the peer)
        // must not touch epoll at all - re-arming or clearing here would be a redundant syscall per round trip.
        using var pump = new RecordingInterestPump();
        const int fd = 12;
        pump.Seed(fd);
        var conn = pump.Handshakes[fd];

        pump.ApplyInProgressHandshakeInterest(fd, ref conn, TlsOperationStatus.NeedMoreData);

        Assert.Empty(pump.InterestMasks);
        Assert.Equal(NativeTls.EPOLLIN | NativeTls.EPOLLRDHUP, conn.CurrentEpollInterest);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void DestinationTooSmall_Repeated_ArmsWritableInterestOnlyOnce()
    {
        // Repeated DestinationTooSmall steps (the send buffer accepts a little output at a time) already have
        // EPOLLOUT armed, so only the first step should issue an epoll_ctl.
        using var pump = new RecordingInterestPump();
        const int fd = 13;
        pump.Seed(fd);
        var conn = pump.Handshakes[fd];

        pump.ApplyInProgressHandshakeInterest(fd, ref conn, TlsOperationStatus.DestinationTooSmall);
        pump.ApplyInProgressHandshakeInterest(fd, ref conn, TlsOperationStatus.DestinationTooSmall);

        Assert.Single(pump.InterestMasks);
        Assert.Equal(NativeTls.EPOLLIN | NativeTls.EPOLLOUT | NativeTls.EPOLLRDHUP, conn.CurrentEpollInterest);
    }

    /// <summary>
    /// A pump whose only real state is a live epoll fd; the handshake interest updates are recorded, not issued.
    /// </summary>
    private sealed class RecordingInterestPump : TlsEventPump
    {
        public List<uint> InterestMasks { get; } = new();

        public RecordingInterestPump()
            : base(tlsPumpLogger: NullLogger<TlsEventPump>.Instance, id: 0, handshakeTimeout: Timeout.InfiniteTimeSpan)
        {
        }

        internal override void UpdateHandshakeInterest(int fd, uint events) => InterestMasks.Add(events);

        // Mirrors real registration: a handshaking fd is added EPOLLIN | EPOLLRDHUP.
        public void Seed(int fd)
            => Handshakes[fd] = new HandshakingConnection
            {
                Fd = fd,
                Session = null!,
                CurrentEpollInterest = NativeTls.EPOLLIN | NativeTls.EPOLLRDHUP,
            };
    }
}

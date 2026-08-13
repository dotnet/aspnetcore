// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Net.Security;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Regression tests for the pump's established-connection drop paths. Established-connection events are
/// registered level-triggered, so when a connection fails (native I/O throws, or the peer half-closes with
/// EPOLLRDHUP), the pump must remove the fd from BOTH the connection table and its epoll interest set. If
/// only the table entry is dropped, the still-registered fd keeps re-firing on every <c>epoll_wait</c> and is
/// dropped again at the <c>_connections</c> lookup, tight-spinning the pump thread at 100% CPU and starving
/// every other connection it owns until the connection is finally disposed. These tests drive
/// <see cref="TlsEventPump.HandleConnectionEvent"/> directly and assert the fd is de-registered from epoll.
/// Linux-only: constructing a <see cref="TlsEventPump"/> calls <c>epoll_create1</c>.
/// </summary>
public class TlsEventPumpConnectionDropTests
{
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void HandleConnectionEvent_PeerClosedConnection_DeregistersFdFromEpoll()
    {
        using var pump = new RecordingDeregisterPump();
        const int fd = 42;
        var conn = new ConnectionIoState(fd, session: null!, logger: NullLogger<ConnectionIoState>.Instance);
        pump.TrackConnectionForTest(fd, conn);

        // EPOLLRDHUP with no EPOLLIN: the peer closed their write side and there is nothing to read.
        pump.HandleConnectionEvent(fd, NativeTls.EPOLLRDHUP);

        Assert.Contains(fd, pump.DeregisteredFds);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void HandleConnectionEvent_ConnectionIoThrows_DeregistersFdFromEpoll()
    {
        using var pump = new RecordingDeregisterPump();
        const int fd = 43;
        var conn = new ReadFailsOnCompletionConnection(fd);
        conn.ReadAsync(new byte[16]); // parks waiting for readable
        pump.TrackConnectionForTest(fd, conn);

        // Socket readable, but completing the read throws (simulating SSL_read on a reset peer).
        pump.HandleConnectionEvent(fd, NativeTls.EPOLLIN);

        Assert.Contains(fd, pump.DeregisteredFds);
    }

    [ConditionalTheory]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    [InlineData(NativeTls.EPOLLERR)]
    [InlineData(NativeTls.EPOLLHUP)]
    [InlineData(NativeTls.EPOLLERR | NativeTls.EPOLLHUP)]
    public void HandleConnectionEvent_ErrorOrHangupOnIdleConnection_DeregistersFdFromEpoll(uint mask)
    {
        using var pump = new RecordingDeregisterPump();
        const int fd = 44;
        // Idle established connection: no read or write awaitable is active, so OnReadable/OnWritable are
        // no-ops and never throw. EPOLLERR/EPOLLHUP is level-triggered, so without an unconditional drop the
        // event would re-fire on every epoll_wait and tight-spin the pump.
        var conn = new ConnectionIoState(fd, session: null!, logger: NullLogger<ConnectionIoState>.Instance);
        pump.TrackConnectionForTest(fd, conn);

        pump.HandleConnectionEvent(fd, mask);

        Assert.Contains(fd, pump.DeregisteredFds);
    }

    /// <summary>A pump whose only real state is a live epoll fd; the epoll de-registration is recorded, not issued.</summary>
    private sealed class RecordingDeregisterPump : TlsEventPump
    {
        public List<int> DeregisteredFds { get; } = new();

        public RecordingDeregisterPump()
            : base(tlsPumpLogger: NullLogger<TlsEventPump>.Instance, id: 0, handshakeTimeout: Timeout.InfiniteTimeSpan)
        {
        }

        internal override void DeregisterFromEpoll(int fd) => DeregisteredFds.Add(fd);
    }

    /// <summary>
    /// A connection whose first read parks (NeedMoreData) and whose completion attempt throws, so that
    /// <see cref="ConnectionIoState.OnReadable"/> surfaces an I/O exception through the pump's catch path.
    /// </summary>
    private sealed class ReadFailsOnCompletionConnection : ConnectionIoState
    {
        private bool _parked;

        public ReadFailsOnCompletionConnection(int fd)
            : base(fd, session: null!, logger: NullLogger<ConnectionIoState>.Instance)
        {
            SetHandshakeComplete();
        }

        internal override TlsOperationStatus RawRead(Span<byte> buffer, out int bytesRead)
        {
            bytesRead = 0;
            if (!_parked)
            {
                _parked = true;
                return TlsOperationStatus.NeedMoreData;
            }

            throw new IOException("SSL_read failed on a reset peer");
        }
    }
}

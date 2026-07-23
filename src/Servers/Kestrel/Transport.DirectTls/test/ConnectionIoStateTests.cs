// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Authentication;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Tests;

/// <summary>
/// Unit tests for the <see cref="ConnectionIoState"/> read/write + epoll-interest state machine.
/// The real class is driven by a native <see cref="TlsSocketSession"/> whose
/// <see cref="TlsOperationStatus"/> can't be forced on demand, and its epoll interest disappears into a
/// syscall. <see cref="ScriptedConnectionIoState"/> subclasses it to script those statuses and capture the
/// computed mask, so every transition - including the renegotiation cases that never occur under TLS 1.3 -
/// can be exercised deterministically and single-threaded.
/// </summary>
public class ConnectionIoStateTests
{
    private const uint In = NativeTls.EPOLLIN;
    private const uint InOut = NativeTls.EPOLLIN | NativeTls.EPOLLOUT;

    // ── Read ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Read_Complete_ReturnsResult_WithoutTouchingEpoll()
    {
        var io = new ScriptedConnectionIoState();
        io.ScriptRead(TlsOperationStatus.Complete, bytesRead: 7);

        var read = io.ReadAsync(new byte[16]);

        Assert.True(read.IsCompleted);
        Assert.Equal(7, await read);
        Assert.Equal(0, io.ApplyEventsCallCount);
    }

    [Fact]
    public void Read_NeedMoreData_Parks_WithoutTouchingEpoll()
    {
        // The WANT_READ hot path must issue no epoll_ctl (EPOLLIN is already the baseline interest).
        var io = new ScriptedConnectionIoState();
        io.ScriptRead(TlsOperationStatus.NeedMoreData);

        var read = io.ReadAsync(new byte[16]);

        Assert.False(read.IsCompleted);
        Assert.Equal(0, io.ApplyEventsCallCount);
    }

    [Fact]
    public async Task Read_Renegotiation_RequestsEpollOut_ThenCompletesOnWritable()
    {
        var io = new ScriptedConnectionIoState();

        // Read enters renegotiation: it must flush handshake output, so it waits for writable.
        io.ScriptRead(TlsOperationStatus.DestinationTooSmall);
        var read = io.ReadAsync(new byte[16]);
        Assert.False(read.IsCompleted);
        Assert.Equal(InOut, io.LastEvents);

        // Socket becomes writable; the flush finishes and the read produces data.
        io.ScriptRead(TlsOperationStatus.Complete, bytesRead: 3);
        io.OnWritable();

        Assert.True(read.IsCompleted);
        Assert.Equal(3, await read);
        Assert.Equal(In, io.LastEvents); // EPOLLOUT dropped once the read no longer needs it.
    }

    [Fact]
    public async Task Read_Eof_CompletesWithZero()
    {
        var io = new ScriptedConnectionIoState();
        io.ScriptRead(TlsOperationStatus.Closed);

        var read = io.ReadAsync(new byte[16]);

        Assert.True(read.IsCompleted);
        Assert.Equal(0, await read);
    }

    [Fact]
    public async Task Read_AbruptClose_MapsAuthenticationExceptionToEof()
    {
        // The runtime surfaces an abrupt peer close (ECONNRESET / no close_notify) as
        // AuthenticationException; TlsRead must translate it to a clean EOF.
        var io = new ScriptedConnectionIoState();
        io.ScriptReadAbruptClose();

        var read = io.ReadAsync(new byte[16]);

        Assert.True(read.IsCompleted);
        Assert.Equal(0, await read);
    }

    [Fact]
    public void Read_UnexpectedStatus_ThrowsTlsException()
    {
        // A handshake-phase status must never appear on the application read path; TlsRead rejects it.
        var io = new ScriptedConnectionIoState();
        io.ScriptRead(TlsOperationStatus.NeedsTlsContext);

        Assert.Throws<TlsException>(() => io.ReadAsync(new byte[16]));
    }

    // ── Write ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Write_Complete_ReturnsTotal()
    {
        var io = new ScriptedConnectionIoState();
        io.ScriptWrite(TlsOperationStatus.Complete);

        var write = io.WriteAsync(new byte[10]);

        Assert.True(write.IsCompleted);
        Assert.Equal(10, await write); // reports the original request length, not the chunk count
        Assert.Equal(0, io.ApplyEventsCallCount);
    }

    [Fact]
    public async Task Write_WouldBlock_RequestsEpollOut_ThenCompletesOnWritable()
    {
        var io = new ScriptedConnectionIoState();

        io.ScriptWrite(TlsOperationStatus.DestinationTooSmall);
        var write = io.WriteAsync(new byte[10]);
        Assert.False(write.IsCompleted);
        Assert.Equal(InOut, io.LastEvents);

        io.ScriptWrite(TlsOperationStatus.Complete);
        io.OnWritable();

        Assert.True(write.IsCompleted);
        Assert.Equal(10, await write);
        Assert.Equal(In, io.LastEvents);
    }

    [Fact]
    public void Write_Renegotiation_WaitsForReadable_WithoutTouchingEpoll()
    {
        // Write needs to read peer ciphertext first; EPOLLIN is already registered, so no epoll_ctl.
        var io = new ScriptedConnectionIoState();
        io.ScriptWrite(TlsOperationStatus.NeedMoreData);

        var write = io.WriteAsync(new byte[10]);

        Assert.False(write.IsCompleted);
        Assert.Equal(0, io.ApplyEventsCallCount);
    }

    [Fact]
    public async Task Write_AbruptClose_MapsAuthenticationExceptionToEof()
    {
        var io = new ScriptedConnectionIoState();
        io.ScriptWriteAbruptClose();

        var write = io.WriteAsync(new byte[10]);

        Assert.True(write.IsCompleted);
        Assert.Equal(0, await write);
    }

    [Fact]
    public void Write_UnexpectedStatus_ThrowsTlsException()
    {
        var io = new ScriptedConnectionIoState();
        io.ScriptWrite(TlsOperationStatus.CertificateRequested);

        Assert.Throws<TlsException>(() => io.WriteAsync(new byte[10]));
    }

    // ── The union-mask regression: concurrent read+write must not clobber each other ──

    [Fact]
    public async Task ReadRenegotiationCompletion_DoesNotDropParkedWriteEpollOut()
    {
        // Reproduces the deterministic hang the union mask fixes: a write parked on EPOLLOUT and a read
        // flushing renegotiation output (also on EPOLLOUT). When the read completes first, the pre-fix
        // code wrote an absolute EPOLLIN and dropped the write's EPOLLOUT -> the write hung forever.
        var io = new ScriptedConnectionIoState();

        io.ScriptWrite(TlsOperationStatus.DestinationTooSmall); // write WouldBlocks -> parks on EPOLLOUT
        var write = io.WriteAsync(new byte[10]);
        Assert.Equal(InOut, io.LastEvents);

        io.ScriptRead(TlsOperationStatus.DestinationTooSmall);  // read enters reneg -> also wants EPOLLOUT
        var read = io.ReadAsync(new byte[16]);
        Assert.Equal(InOut, io.LastEvents);

        // Socket writable. The pump gives the reneg read priority and completes it.
        io.ScriptRead(TlsOperationStatus.Complete, bytesRead: 5);
        io.OnWritable();

        Assert.True(read.IsCompleted);
        Assert.Equal(5, await read);
        Assert.False(write.IsCompleted);          // write is still parked...
        Assert.Equal(InOut, io.LastEvents);       // ...and its EPOLLOUT must survive (regression guard).

        // Next writable wake now actually reaches the write and completes it.
        io.ScriptWrite(TlsOperationStatus.Complete);
        io.OnWritable();
        Assert.True(write.IsCompleted);
        Assert.Equal(10, await write);
        Assert.Equal(In, io.LastEvents);          // EPOLLOUT dropped once nobody needs it.
    }

    [Fact]
    public async Task WriteRenegotiation_DropsEpollOut_ThenCompletesOnReadable()
    {
        // A parked write that flips into renegotiation (NeedMoreData) must drop its EPOLLOUT and wait for
        // readable; OnReadable then prioritises the renegotiating write and completes it.
        var io = new ScriptedConnectionIoState();

        io.ScriptWrite(TlsOperationStatus.DestinationTooSmall);
        var write = io.WriteAsync(new byte[10]);
        Assert.Equal(InOut, io.LastEvents);

        io.ScriptWrite(TlsOperationStatus.NeedMoreData); // reneg: needs to read now
        io.OnWritable();
        Assert.False(write.IsCompleted);
        Assert.Equal(In, io.LastEvents); // EPOLLOUT dropped, back to baseline EPOLLIN

        io.ScriptWrite(TlsOperationStatus.Complete);
        io.OnReadable();
        Assert.True(write.IsCompleted);
        Assert.Equal(10, await write);
        Assert.Equal(In, io.LastEvents);
    }

    [Fact]
    public async Task OnReadable_PrioritisesRenegotiatingWriteOverPendingRead()
    {
        // Both sides are waiting for readable: a reneg write and a normal read. The write must be serviced
        // first (its progress unblocks the read's ciphertext).
        var io = new ScriptedConnectionIoState();

        io.ScriptWrite(TlsOperationStatus.NeedMoreData);   // write parks waiting for readable
        var write = io.WriteAsync(new byte[10]);

        io.ScriptRead(TlsOperationStatus.NeedMoreData);    // read parks waiting for readable
        var read = io.ReadAsync(new byte[16]);

        io.ScriptWrite(TlsOperationStatus.Complete);       // only the write is scripted to progress
        io.OnReadable();

        Assert.True(write.IsCompleted);
        Assert.Equal(10, await write);
        Assert.False(read.IsCompleted);                    // read untouched this wake
    }

    /// <summary>
    /// A <see cref="ConnectionIoState"/> with no real socket or pump: <see cref="TlsRead"/>/<see cref="TlsWrite"/>
    /// return scripted statuses and <see cref="ApplyEvents"/> records the computed epoll interest.
    /// </summary>
    private sealed class ScriptedConnectionIoState : ConnectionIoState
    {
        // A null status simulates an abrupt peer close (the runtime throws AuthenticationException).
        private readonly Queue<(TlsOperationStatus? Status, int Count)> _reads = new();
        private readonly Queue<(TlsOperationStatus? Status, int Count)> _writes = new();

        public ScriptedConnectionIoState()
            : base(fd: -1, session: null!, logger: null)
        {
            SetHandshakeComplete();
        }

        /// <summary>The last epoll interest mask the state machine computed and applied.</summary>
        public uint LastEvents { get; private set; } = NativeTls.EPOLLIN;

        /// <summary>Number of times the state machine applied an epoll interest (0 == no syscall).</summary>
        public int ApplyEventsCallCount { get; private set; }

        public void ScriptRead(TlsOperationStatus status, int bytesRead = 0) => _reads.Enqueue((status, bytesRead));

        public void ScriptWrite(TlsOperationStatus status, int bytesWritten = 0) => _writes.Enqueue((status, bytesWritten));

        /// <summary>Script the next read to surface an abrupt peer close (AuthenticationException).</summary>
        public void ScriptReadAbruptClose() => _reads.Enqueue((null, 0));

        /// <summary>Script the next write to surface an abrupt peer close (AuthenticationException).</summary>
        public void ScriptWriteAbruptClose() => _writes.Enqueue((null, 0));

        internal override TlsOperationStatus RawRead(Span<byte> buffer, out int bytesRead)
        {
            Assert.True(_reads.Count > 0, "Unexpected RawRead: no status scripted");
            var (status, count) = _reads.Dequeue();
            bytesRead = count;
            return status ?? throw new AuthenticationException();
        }

        internal override TlsOperationStatus RawWrite(ReadOnlySpan<byte> buffer, out int bytesWritten)
        {
            Assert.True(_writes.Count > 0, "Unexpected RawWrite: no status scripted");
            var (status, count) = _writes.Dequeue();
            bytesWritten = count;
            return status ?? throw new AuthenticationException();
        }

        internal override void ApplyEvents(uint events)
        {
            ApplyEventsCallCount++;
            LastEvents = events;
        }
    }
}

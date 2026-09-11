// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Authentication;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;

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
    public async Task Read_Renegotiation_ThenEof_DropsEpollOut()
    {
        var io = new ScriptedConnectionIoState();

        // Read enters renegotiation: it must flush handshake output, so it waits for writable (EPOLLOUT armed).
        io.ScriptRead(TlsOperationStatus.DestinationTooSmall);
        var read = io.ReadAsync(new byte[16]);
        Assert.False(read.IsCompleted);
        Assert.Equal(InOut, io.LastEvents);

        // Socket becomes writable, but the session reports EOF instead of resuming the flush. The read must
        // complete with 0 AND drop the read side's EPOLLOUT - otherwise OnWritable keeps firing (level-triggered)
        // with no active read until the connection is disposed.
        io.ScriptRead(TlsOperationStatus.Closed);
        io.OnWritable();

        Assert.True(read.IsCompleted);
        Assert.Equal(0, await read);
        Assert.Equal(In, io.LastEvents); // EPOLLOUT dropped on EOF.
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
    public void Read_TlsFailure_PropagatesAuthenticationException()
    {
        // The runtime maps both an abrupt close and a genuine TLS failure to the same AuthenticationException,
        // so TlsRead no longer swallows it as EOF - it must propagate so the connection faults.
        var io = new ScriptedConnectionIoState();
        io.ScriptReadFailure();

        Assert.Throws<AuthenticationException>(() => io.ReadAsync(new byte[16]));
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
    public void Write_Renegotiation_ReArmsReadableInterest()
    {
        // Write needs to read peer ciphertext first. EPOLLIN is no longer an unconditional baseline (the receive
        // loop can suspend it for backpressure), so the write must re-arm read interest itself.
        var io = new ScriptedConnectionIoState();
        io.ScriptWrite(TlsOperationStatus.NeedMoreData);

        var write = io.WriteAsync(new byte[10]);

        Assert.False(write.IsCompleted);
        Assert.Equal(1, io.ApplyEventsCallCount);
        Assert.Equal(In, io.LastEvents);
    }

    [Fact]
    public void Write_TlsFailure_PropagatesAuthenticationException()
    {
        var io = new ScriptedConnectionIoState();
        io.ScriptWriteFailure();

        Assert.Throws<AuthenticationException>(() => io.WriteAsync(new byte[10]));
    }

    [Fact]
    public void Write_UnexpectedStatus_ThrowsTlsException()
    {
        var io = new ScriptedConnectionIoState();
        io.ScriptWrite(TlsOperationStatus.CertificateRequested);

        Assert.Throws<TlsException>(() => io.WriteAsync(new byte[10]));
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void Write_WouldBlock_WhenEpollModRejected_ThrowsTlsException()
    {
        // A write that WouldBlock re-arms EPOLLOUT through UpdateEvents -> ApplyEvents -> pump.ModifyEvents.
        // If the kernel rejects the epoll_ctl MOD, the connection would otherwise be left parked on a writable
        // interest that was never registered. Assert the state machine turns that into a fatal TlsException so
        // the established-path drop convention (pump-thread guard / receive-send loop) tears the connection down.
        using var pump = new RejectingModifyPump();
        var io = new WouldBlockWriteIoState { Pump = pump };

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
    public void ConcurrentReadCompletionAndWrite_DoNotDropTheParkedWriteEpollOut()
    {
        // The multi-threaded sibling of the union-mask test above. UpdateEvents reads both want-flags and
        // applies an ABSOLUTE epoll interest (EPOLL_CTL_MOD replaces, it does not OR), and it runs on two
        // threads with no lock: the pump completing a renegotiating read, and the send loop submitting a
        // write. If the pump computes its post-completion mask (EPOLLIN only, seeing no pending write) and a
        // concurrent write then requests EPOLLOUT, the pump's stale mask can land last and silently drop the
        // EPOLLOUT the parked write is waiting on -> that write wedges until the connection dies. The union
        // mask alone does not fix this; it needs the writes to be serialized per connection.
        var io = new ScriptedConnectionIoState();

        // A read is parked flushing renegotiation output, so the connection currently wants EPOLLIN|EPOLLOUT.
        io.ScriptRead(TlsOperationStatus.DestinationTooSmall);
        var read = io.ReadAsync(new byte[16]);
        Assert.Equal(InOut, io.LastEvents);

        io.ScriptRead(TlsOperationStatus.Complete, bytesRead: 5);   // the renegotiation read then completes
        io.ScriptWrite(TlsOperationStatus.DestinationTooSmall);      // a concurrent write hits WouldBlock -> wants EPOLLOUT

        // Force the losing interleaving: the pump computes its EPOLLIN-only mask FIRST (while no write is
        // pending) and parks mid-apply; the write then requests EPOLLOUT; finally the pump's stale mask lands.
        io.EpollOutApplied.Reset();
        io.ArmGate();

        var pump = new Thread(() => io.OnWritable()) { IsBackground = true };
        pump.Start();
        Assert.True(io.GateReached.Wait(TimeSpan.FromSeconds(10)), "read completion never reached the epoll update");

        ValueTask<int> write = default;
        var sender = new Thread(() => write = io.WriteAsync(new byte[10])) { IsBackground = true };
        sender.Start();

        // Pre-fix: the write applies EPOLLIN|EPOLLOUT immediately; observe it, then let the stale mask land.
        // Fixed: the write blocks on the per-connection lock the parked pump holds, so this wait elapses and
        // the write only applies its (correct, union) mask AFTER the pump releases -> no lost EPOLLOUT.
        io.EpollOutApplied.Wait(TimeSpan.FromMilliseconds(500));
        io.ReleaseGate();

        Assert.True(pump.Join(TimeSpan.FromSeconds(10)));
        Assert.True(sender.Join(TimeSpan.FromSeconds(10)));

        Assert.True(read.IsCompleted);
        Assert.False(write.IsCompleted);      // the write is still parked...
        Assert.Equal(InOut, io.LastEvents);   // ...so the connection MUST still be requesting EPOLLOUT.
    }

    [Fact]
    public async Task Cancel_WaitsOutInFlightNativeRead_BeforeCompletingAwaitable()
    {
        // Cancel() completes the read awaitable so the receive loop unblocks and returns _readBuffer to the
        // pool. If it did so while the pump is inside SSL_read still writing into that buffer, the loop would
        // recycle a block under an in-flight native write (the mirror race recycles the output block under
        // SSL_write). Cancel() must take _sslLock and wait out any in-flight native call first.
        var io = new ScriptedConnectionIoState();

        io.ScriptRead(TlsOperationStatus.NeedMoreData);     // park a read waiting for more ciphertext
        var read = io.ReadAsync(new byte[16]);
        Assert.False(read.IsCompleted);

        io.ScriptRead(TlsOperationStatus.Complete, bytesRead: 5);   // the completion the pump will process
        io.ArmRawReadGate();

        // Pump parks inside RawRead - i.e. mid SSL_read - while holding _sslLock via OnReadable.
        var pump = new Thread(() => io.OnReadable()) { IsBackground = true };
        pump.Start();
        Assert.True(io.RawReadGateReached.Wait(TimeSpan.FromSeconds(10)), "pump never reached the in-flight read");

        var cancel = new Thread(() => io.Cancel()) { IsBackground = true };
        cancel.Start();

        // Fixed: Cancel blocks on _sslLock the parked pump holds, so it cannot complete while a native read is
        // in flight. Pre-fix it took no lock and would join here immediately, completing the awaitable early.
        Assert.False(cancel.Join(TimeSpan.FromMilliseconds(500)), "Cancel completed while a native read was in flight");

        io.ReleaseRawReadGate();

        Assert.True(cancel.Join(TimeSpan.FromSeconds(10)));
        Assert.True(pump.Join(TimeSpan.FromSeconds(10)));
        Assert.True(read.IsCompleted);
        Assert.Equal(5, await read);   // the read finished writing its 5 bytes before the buffer could be recycled
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

    // ── Read-interest suspend/resume (flush backpressure) ─────────────────────

    [Fact]
    public void SuspendReadInterest_DropsEpollIn_ResumeRestoresIt()
    {
        // While the receive loop is blocked on a backpressured flush, no read is pending, yet level-triggered
        // EPOLLIN on still-buffered ciphertext would spin the pump. Suspending must drop EPOLLIN; resuming re-arms it.
        var io = new ScriptedConnectionIoState();

        io.SuspendReadInterest();
        Assert.Equal(1, io.ApplyEventsCallCount);
        Assert.Equal(0u, io.LastEvents); // EPOLLIN dropped, nothing else wanted.

        io.SuspendReadInterest(); // idempotent - no second syscall
        Assert.Equal(1, io.ApplyEventsCallCount);

        io.ResumeReadInterest();
        Assert.Equal(2, io.ApplyEventsCallCount);
        Assert.Equal(In, io.LastEvents);

        io.ResumeReadInterest(); // idempotent
        Assert.Equal(2, io.ApplyEventsCallCount);
    }

    [Fact]
    public void SuspendReadInterest_KeepsEpollIn_WhenWriteStillNeedsToRead()
    {
        // A write parked in renegotiation (_writeWantsRead) must keep EPOLLIN armed even while the receive loop
        // suspends its own read interest, otherwise the renegotiating write is stranded with no wakeup.
        var io = new ScriptedConnectionIoState();

        io.ScriptWrite(TlsOperationStatus.NeedMoreData);
        var write = io.WriteAsync(new byte[10]);
        Assert.False(write.IsCompleted);
        Assert.Equal(In, io.LastEvents);

        io.SuspendReadInterest();
        Assert.Equal(In, io.LastEvents); // still armed for the write's renegotiation read.
    }

    // ── Dispose ─────────────────────────────────────────────────────────────

    [Fact]
    public void Dispose_DisposesSession_AfterGracefulShutdown()
    {
        var io = new ScriptedConnectionIoState();

        io.Dispose();

        Assert.Equal(1, io.ShutdownCallCount);
        Assert.Equal(1, io.DisposeCallCount);
    }

    [Fact]
    public void Dispose_WhenShutdownThrows_StillDisposesSession()
    {
        // Shutdown (close_notify) can throw on an abrupt peer close. The session must still be disposed so the
        // SSL handle / socket fd are not leaked to finalization; the exception propagates to the caller to log.
        var io = new ScriptedConnectionIoState();
        io.ScriptShutdownThrows();

        Assert.Throws<AuthenticationException>(io.Dispose);

        Assert.Equal(1, io.ShutdownCallCount);
        Assert.Equal(1, io.DisposeCallCount);
    }

    /// <summary>
    /// A <see cref="ConnectionIoState"/> with no real socket or pump: <see cref="TlsRead"/>/<see cref="TlsWrite"/>
    /// return scripted statuses and <see cref="ApplyEvents"/> records the computed epoll interest.
    /// </summary>
    private sealed class ScriptedConnectionIoState : ConnectionIoState
    {
        // A null status simulates a failed native SSL op (the runtime throws AuthenticationException).
        private readonly Queue<(TlsOperationStatus? Status, int Count)> _reads = new();
        private readonly Queue<(TlsOperationStatus? Status, int Count)> _writes = new();

        public ScriptedConnectionIoState()
            : base(fd: -1, session: null!, logger: NullLogger<ConnectionIoState>.Instance)
        {
            SetHandshakeComplete();
        }

        /// <summary>The last epoll interest mask the state machine computed and applied.</summary>
        public uint LastEvents { get; private set; } = NativeTls.EPOLLIN;

        /// <summary>Number of times the state machine applied an epoll interest (0 == no syscall).</summary>
        public int ApplyEventsCallCount { get; private set; }

        public void ScriptRead(TlsOperationStatus status, int bytesRead = 0) => _reads.Enqueue((status, bytesRead));

        public void ScriptWrite(TlsOperationStatus status, int bytesWritten = 0) => _writes.Enqueue((status, bytesWritten));

        /// <summary>Script the next read to surface a failed native SSL op (AuthenticationException).</summary>
        public void ScriptReadFailure() => _reads.Enqueue((null, 0));

        /// <summary>Script the next write to surface a failed native SSL op (AuthenticationException).</summary>
        public void ScriptWriteFailure() => _writes.Enqueue((null, 0));

        internal override TlsOperationStatus RawRead(Span<byte> buffer, out int bytesRead)
        {
            // Optionally park mid-read (still inside OnReadable's _sslLock) to model an in-flight SSL_read.
            if (Interlocked.CompareExchange(ref _rawReadGateArmed, 0, 1) == 1)
            {
                _rawReadGateReached.Set();
                _rawReadGateRelease.Wait();
            }

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

        // ── Gate hooks for the concurrent epoll-interest race test (opt-in; unarmed for every other test) ──
        private int _gateArmed;
        private readonly ManualResetEventSlim _gateReached = new();
        private readonly ManualResetEventSlim _gateRelease = new();
        private readonly ManualResetEventSlim _epollOutApplied = new();

        // ── Gate hook for the Cancel-vs-in-flight-native-read race test (opt-in; unarmed otherwise) ──
        private int _rawReadGateArmed;
        private readonly ManualResetEventSlim _rawReadGateReached = new();
        private readonly ManualResetEventSlim _rawReadGateRelease = new();

        /// <summary>Signalled once a gated <see cref="RawRead"/> has parked mid-read inside OnReadable's lock.</summary>
        public ManualResetEventSlim RawReadGateReached => _rawReadGateReached;

        /// <summary>Arm the gate so the NEXT <see cref="RawRead"/> parks (holding _sslLock) until <see cref="ReleaseRawReadGate"/>.</summary>
        public void ArmRawReadGate() => Volatile.Write(ref _rawReadGateArmed, 1);

        public void ReleaseRawReadGate() => _rawReadGateRelease.Set();

        /// <summary>Signalled once a gated <see cref="ApplyEvents"/> call has parked mid-update inside the gate.</summary>
        public ManualResetEventSlim GateReached => _gateReached;

        /// <summary>Signalled each time an EPOLLOUT-bearing interest mask is applied.</summary>
        public ManualResetEventSlim EpollOutApplied => _epollOutApplied;

        /// <summary>Arm the gate so the NEXT <see cref="ApplyEvents"/> call blocks mid-update until <see cref="ReleaseGate"/>.</summary>
        public void ArmGate() => Volatile.Write(ref _gateArmed, 1);

        public void ReleaseGate() => _gateRelease.Set();

        internal override void ApplyEvents(uint events)
        {
            if (Interlocked.CompareExchange(ref _gateArmed, 0, 1) == 1)
            {
                _gateReached.Set();
                _gateRelease.Wait();
            }

            ApplyEventsCallCount++;
            LastEvents = events;

            if ((events & NativeTls.EPOLLOUT) != 0)
            {
                _epollOutApplied.Set();
            }
        }

        // ── Dispose seam hooks: script a throwing Shutdown and observe that Dispose still runs ──
        private bool _shutdownThrows;

        /// <summary>Number of times the session shutdown (close_notify) seam ran.</summary>
        public int ShutdownCallCount { get; private set; }

        /// <summary>Number of times the session dispose seam ran (must stay 1 even if Shutdown throws).</summary>
        public int DisposeCallCount { get; private set; }

        /// <summary>Make the next session shutdown throw, simulating an abrupt peer close.</summary>
        public void ScriptShutdownThrows() => _shutdownThrows = true;

        internal override void ShutdownSession()
        {
            ShutdownCallCount++;
            if (_shutdownThrows)
            {
                throw new AuthenticationException("scripted abrupt close");
            }
        }

        internal override void DisposeSession() => DisposeCallCount++;
    }

    /// <summary>
    /// A <see cref="ConnectionIoState"/> whose first write always reports a WouldBlock
    /// (<see cref="TlsOperationStatus.DestinationTooSmall"/>), driving the base <see cref="ConnectionIoState.ApplyEvents"/>
    /// through a real (rejecting) pump so the epoll-MOD failure path can be exercised end to end.
    /// </summary>
    private sealed class WouldBlockWriteIoState : ConnectionIoState
    {
        public WouldBlockWriteIoState()
            : base(fd: 7, session: null!, logger: NullLogger<ConnectionIoState>.Instance)
        {
            SetHandshakeComplete();
        }

        internal override TlsOperationStatus RawWrite(ReadOnlySpan<byte> buffer, out int bytesWritten)
        {
            bytesWritten = 0;
            return TlsOperationStatus.DestinationTooSmall;
        }
    }

    /// <summary>
    /// A pump whose <see cref="TlsEventPump.ModifyEvents"/> always reports the kernel rejected the change,
    /// without issuing a real <c>epoll_ctl</c>. Constructing it calls <c>epoll_create1</c>, so tests using it
    /// are Linux-only.
    /// </summary>
    private sealed class RejectingModifyPump : TlsEventPump
    {
        public RejectingModifyPump()
            : base(tlsPumpLogger: NullLogger<TlsEventPump>.Instance, id: 0, handshakeTimeout: Timeout.InfiniteTimeSpan)
        {
        }

        private protected override bool TryModifyEstablishedInterest(int fd, uint events) => false;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

#pragma warning disable SYSLIB5007 // TlsSocketSession/TlsOperationStatus are experimental.

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Per-connection state for an established DirectTls connection. Drives non-blocking application
/// reads/writes directly over the runtime's socket-bound <see cref="TlsSocketSession"/>, which runs
/// <c>SSL_read</c>/<c>SSL_write</c> over the connection's file descriptor and returns a
/// <see cref="TlsOperationStatus"/>. The pump maps that status straight to an epoll interest:
/// <see cref="TlsOperationStatus.NeedMoreData"/> waits for the socket to become readable (<c>EPOLLIN</c>)
/// and <see cref="TlsOperationStatus.DestinationTooSmall"/> waits for it to become writable (<c>EPOLLOUT</c>),
/// for both reads and writes (a renegotiation can flip the direction). The handshake is completed by
/// <see cref="TlsEventPump"/> before this state is created. DirectTls terminates TLS for every connection,
/// so the session is the only byte-stream backend.
/// </summary>
/// <remarks>
/// Not <c>sealed</c> so tests can subclass it to script the raw session results via
/// <see cref="RawRead"/>/<see cref="RawWrite"/> and observe the epoll interest via <see cref="ApplyEvents"/>
/// without a live socket or pump. Those three members are the only <c>virtual</c> seams - they wrap just the
/// native <c>SSL_read</c>/<c>SSL_write</c> calls and the <c>epoll_ctl</c> syscall; everything above them
/// (status validation, abrupt-close translation, the read/write state machine and epoll-interest logic) is
/// the real code exercised end to end.
/// </remarks>
internal class ConnectionIoState : IDisposable
{
    private readonly ILogger _logger;
    private readonly TlsSocketSession _session;
    private readonly DirectTlsMetrics _metrics;
    private BaseConnectionContext? _connection;
    private uint _currentEpollInterest;

    // Serializes every native SSL operation (SSL_read / SSL_write / SSL_shutdown) on this connection's
    // session. TlsSocketSession is not safe for concurrent Read/Write from different threads - one SSL*
    // and its scratch buffers back both directions, and in TLS 1.3 post-handshake messages make reads and
    // writes touch shared state. Yet the receive loop, the send loop, and the pump thread can each drive a
    // native call simultaneously (most often under HTTP/2 duplex traffic, where a request body is read while
    // a response body is written). Without this gate, concurrent SSL_read/SSL_write corrupt the TLS state
    // machine and surface a spurious error that TlsRead translates to EOF, closing a live connection
    // mid-stream.
    //
    // It also serializes the read/write state machine itself - the initiating ReadAsync/WriteAsync (on the
    // receive/send loop threads) and the completing OnReadable/OnWritable (on the pump thread) both mutate
    // the epoll-interest flags (_readWantsWrite/_writeWantsWrite) and issue an *absolute* EPOLL_CTL_MOD via
    // UpdateEvents. Without a common gate those two sides race: the later epoll_ctl wins and can drop an
    // EPOLLOUT the other side is still waiting on, stranding a parked write (a WouldBlock'd final response
    // byte) with no wakeup until the peer times out. Holding this lock across each side's whole transition
    // makes the flag reads and the epoll_ctl atomic with respect to the other side. Because the sockets are
    // non-blocking every native call returns promptly and completions run continuations asynchronously, so
    // the lock is still held only briefly.
    //
    // Each side's transition (ReadAsync/WriteAsync/OnReadable/OnWritable) is written *inline* inside its lock
    // and must stay fully synchronous - it is deliberately not delegated to an async helper. A lock (Monitor)
    // is released when its synchronous scope returns, so an 'await' in the body would drop the lock at the
    // suspension point and let the continuation keep mutating this state unguarded. Keeping the body lexically
    // inside the lock turns any such 'await' into a compile error (CS1996); a plain helper call could hide an
    // await behind the method boundary and silently defeat that guarantee.
    private readonly object _sslLock = new();

    public readonly int Fd;

    /// <summary>The underlying TLS session, exposed so the connection can publish negotiated TLS features.</summary>
    public TlsSocketSession Session => _session;

    // Reference to pump for dynamic event modification
    internal TlsEventPump? Pump { get; set; }

    // Callback for fatal errors (e.g., peer disconnect) - allows owner to trigger disposal
    internal Action<Exception>? OnFatalError { get; set; }

    public bool IsHandshaked { get; private set; }

    // Read - reusable awaitable to avoid TCS allocations
    private readonly TlsAwaitable<int> _readAwaitable = new();
    private Memory<byte> _readBuffer;
    private bool _readWantsWrite;  // Read needs the socket to become writable (renegotiation)

    // Write - reusable awaitable to avoid TCS allocations
    private readonly TlsAwaitable<int> _writeAwaitable = new();
    private ReadOnlyMemory<byte> _writeBuffer;  // Remaining (unwritten) application bytes
    private int _writeTotal;                     // Original request length to report on completion
    private bool _writeWantsRead;                // Write needs the socket to become readable (renegotiation)
    private bool _writeWantsWrite;               // Write hit WouldBlock and needs the socket to become writable
    private bool _readInterestSuspended;         // Receive loop parked on backpressure; drop EPOLLIN to avoid a level-triggered spin
    private bool? _readBackpressureCounterEnabled;
    private bool? _writeBackpressureCounterEnabled;

    public ConnectionIoState(
        int fd,
        TlsSocketSession session,
        ILogger logger,
        DirectTlsMetrics? metrics = null)
    {
        _logger = logger;
        _metrics = metrics ?? DirectTlsMetrics.Disabled;

        Fd = fd;
        _session = session;
    }

    internal void SetConnection(BaseConnectionContext connection)
    {
        _connection = connection;
    }

    internal string ConnectionId => _connection?.ConnectionId ?? string.Empty;

    internal uint CurrentEpollInterest => Volatile.Read(ref _currentEpollInterest);

    internal void SetCurrentEpollInterest(uint events)
    {
        Volatile.Write(ref _currentEpollInterest, events);
    }

    /// <summary>
    /// Mark the handshake as complete. The TLS handshake is performed by the pump before this state
    /// exists, so this simply records that the connection is ready for application I/O.
    /// </summary>
    internal void SetHandshakeComplete()
    {
        IsHandshaked = true;
    }

    // ═══════════════════════════════════════════════════════════════
    // TLS BYTE-STREAM I/O (SSL_read / SSL_write over the fd)
    // ═══════════════════════════════════════════════════════════════

    // The only native session calls, isolated as the test seam (see class remarks). Virtual so tests can
    // script raw statuses or simulate an abrupt close without a live TlsSocketSession.
    internal virtual TlsOperationStatus RawRead(Span<byte> buffer, out int bytesRead)
    {
        lock (_sslLock)
        {
            return _session.Read(buffer, out bytesRead);
        }
    }

    internal virtual TlsOperationStatus RawWrite(ReadOnlySpan<byte> buffer, out int bytesWritten)
    {
        lock (_sslLock)
        {
            return _session.Write(buffer, out bytesWritten);
        }
    }

    internal virtual void ApplyEvents(uint events)
    {
        if (Pump is { } pump && !pump.ModifyEvents(Fd, events))
        {
            throw new TlsException($"Failed to update epoll interest for fd={Fd}");
        }
    }

    internal virtual void ShutdownSession() => _session.Shutdown();

    internal virtual void DisposeSession() => _session.Dispose();

    // Reads decrypted application bytes into buffer. NeedMoreData -> need more ciphertext (wait readable);
    // DestinationTooSmall -> renegotiation must flush handshake output (wait writable).
    private TlsOperationStatus TlsRead(Span<byte> buffer, out int bytesRead)
    {
        var status = RawRead(buffer, out bytesRead);
        if (bytesRead > 0)
        {
            _metrics.BytesRead(bytesRead);
        }

        return status is TlsOperationStatus.Complete or TlsOperationStatus.NeedMoreData
            or TlsOperationStatus.DestinationTooSmall or TlsOperationStatus.Closed
            ? status
            : throw new TlsException($"TLS read failed: {status}");
    }

    // Writes application bytes from buffer. DestinationTooSmall -> socket WouldBlock (wait writable);
    // NeedMoreData -> renegotiation must read peer ciphertext first (wait readable).
    private TlsOperationStatus TlsWrite(ReadOnlySpan<byte> buffer, out int bytesWritten)
    {
        var status = RawWrite(buffer, out bytesWritten);
        if (bytesWritten > 0)
        {
            _metrics.BytesWritten(bytesWritten);
        }

        return status is TlsOperationStatus.Complete or TlsOperationStatus.NeedMoreData
            or TlsOperationStatus.DestinationTooSmall or TlsOperationStatus.Closed
            ? status
            : throw new TlsException($"TLS write failed: {status}");
    }

    // ═══════════════════════════════════════════════════════════════
    // EPOLL INTEREST
    // ═══════════════════════════════════════════════════════════════

    // Applies the epoll interest as the UNION of what the read and write sides currently need. EPOLLIN is
    // requested whenever the receive loop is reading (the steady state) or a write is parked waiting to read peer
    // ciphertext for renegotiation (_writeWantsRead); it is dropped only while the receive loop is blocked on
    // backpressure (_readInterestSuspended), so still-buffered ciphertext can't make the level-triggered pump
    // spin. EPOLLOUT is requested whenever either side waits for the socket to become writable - a write that hit
    // WouldBlock (_writeWantsWrite) or a read flushing renegotiation output (_readWantsWrite). Because ModifyEvents
    // sets the interest absolutely (EPOLL_CTL_MOD replaces, it does not OR), computing the combined mask in one
    // place is what prevents one side's transition from silently dropping an interest the other side is still
    // waiting on, which would otherwise wedge that operation until the connection dies.
    private void UpdateEvents()
    {
        uint events = 0;

        if (!_readInterestSuspended || _writeWantsRead)
        {
            events |= NativeTls.EPOLLIN;
        }

        if (_readWantsWrite || _writeWantsWrite)
        {
            events |= NativeTls.EPOLLOUT;
        }

        ApplyEvents(events);
    }

    // Called by the receive loop when a FlushAsync to the application blocks on backpressure. While suspended,
    // UpdateEvents drops EPOLLIN so still-buffered ciphertext can't spin the level-triggered pump; a parked write
    // that still needs to read (_writeWantsRead) keeps EPOLLIN armed. Re-armed by ResumeReadInterest.
    internal void SuspendReadInterest()
    {
        string? connectionId;

        lock (_sslLock)
        {
            if (_readInterestSuspended)
            {
                return;
            }

            _readInterestSuspended = true;
            UpdateEvents();
            _readBackpressureCounterEnabled = _metrics.ReadConnectionPaused(_connection);
            connectionId = _connection?.ConnectionId;
        }

        if (connectionId is not null)
        {
            DirectTlsLog.ConnectionPause(_logger, connectionId);
        }
    }

    internal void ResumeReadInterest()
    {
        string? connectionId;

        lock (_sslLock)
        {
            if (!_readInterestSuspended)
            {
                return;
            }

            _readInterestSuspended = false;
            UpdateEvents();
            ReleaseReadBackpressureTelemetry();
            connectionId = _connection?.ConnectionId;
        }

        if (connectionId is not null)
        {
            DirectTlsLog.ConnectionResume(_logger, connectionId);
        }
    }

    private void ReleaseReadBackpressureTelemetry()
    {
        if (_readBackpressureCounterEnabled is not { } counterEnabled)
        {
            return;
        }

        _readBackpressureCounterEnabled = null;
        _metrics.ReadConnectionResumed(counterEnabled, _connection);
    }

    private void StartWriteBackpressureTelemetry()
    {
        _writeBackpressureCounterEnabled = _metrics.WriteConnectionPaused(_connection);
    }

    private void ReleaseWriteBackpressureTelemetry()
    {
        if (_writeBackpressureCounterEnabled is not { } counterEnabled)
        {
            return;
        }

        _writeBackpressureCounterEnabled = null;
        _metrics.WriteConnectionResumed(counterEnabled, _connection);
    }

    // ═══════════════════════════════════════════════════════════════
    // READ
    // ═══════════════════════════════════════════════════════════════

    public ValueTask<int> ReadAsync(Memory<byte> buffer)
    {
        // Serialize the whole transition (see _sslLock remarks): the flag updates and the epoll_ctl issued
        // here must be atomic with respect to the pump thread's OnReadable/OnWritable. The body is inlined
        // here rather than delegated to a helper so it stays synchronous - a stray 'await' would be a CS1996
        // compile error instead of silently releasing the lock mid-transition.
        lock (_sslLock)
        {
            if (!IsHandshaked)
            {
                throw new InvalidOperationException("Handshake not complete");
            }

            if (_readAwaitable.IsActive)
            {
                throw new InvalidOperationException("Read already pending");
            }

            var status = TlsRead(buffer.Span, out var read);

            switch (status)
            {
                case TlsOperationStatus.Complete:
                    return new ValueTask<int>(read);

                case TlsOperationStatus.NeedMoreData:
                    // Need more ciphertext; wait for the socket to become readable.
                    _readBuffer = buffer;
                    _readWantsWrite = false;
                    return _readAwaitable.Reset();

                case TlsOperationStatus.DestinationTooSmall:
                    // Renegotiation: the read needs to send handshake output. Wait for writable.
                    _readBuffer = buffer;
                    _readWantsWrite = true;
                    var pending = _readAwaitable.Reset();
                    UpdateEvents();
                    return pending;

                case TlsOperationStatus.Closed:
                default:
                    return new ValueTask<int>(0); // EOF
            }
        }
    }

    /// <summary>
    /// Advances the pending read against the TLS session when the socket signals readable/writable, either
    /// completing the read awaitable or re-arming epoll interest for more ciphertext / renegotiation output.
    /// </summary>
    /// <remarks>
    /// Unsynchronized: the caller must already hold <c>_sslLock</c> - this method never takes it. It mutates the
    /// read state machine (<c>_readBuffer</c>, <c>_readWantsWrite</c>, the read awaitable) and issues the absolute
    /// <c>epoll_ctl</c> via <see cref="UpdateEvents"/>, all of which must stay atomic with respect to
    /// <see cref="ReadAsync"/> and the write side (see the <c>_sslLock</c> remarks). Its only call sites are inside
    /// the <see cref="OnReadable"/>/<see cref="OnWritable"/> lock blocks.
    /// </remarks>
    private void TryCompleteReadUnsynchronized()
    {
        if (!_readAwaitable.IsActive)
        {
            _logger.LogDebug("TryCompleteReadUnsynchronized called but no read is pending");
            return; // Race: canceled or completed between check and call
        }

        var status = TlsRead(_readBuffer.Span, out var read);

        switch (status)
        {
            case TlsOperationStatus.Complete:
            {
                var wasWaitingForWrite = _readWantsWrite;
                _readBuffer = default;
                _readWantsWrite = false;

                if (wasWaitingForWrite)
                {
                    UpdateEvents();
                }

                _readAwaitable.TrySetResult(read);
                return;
            }

            case TlsOperationStatus.NeedMoreData:
                // Still need more ciphertext - if we were flushing renegotiation output, switch back to read.
                if (_readWantsWrite)
                {
                    _readWantsWrite = false;
                    UpdateEvents();
                }
                return;

            case TlsOperationStatus.DestinationTooSmall:
                // Renegotiation: need to write - request EPOLLOUT if not already.
                if (!_readWantsWrite)
                {
                    _readWantsWrite = true;
                    UpdateEvents();
                }
                return;

            case TlsOperationStatus.Closed:
            default:
            {
                // If this read was flushing renegotiation output, EPOLLOUT is armed. Recompute interest after
                // clearing the flag so the read side's writable interest is dropped - otherwise OnWritable keeps
                // firing (level-triggered) with no active read until the connection is disposed.
                var wasWaitingForWrite = _readWantsWrite;
                _readBuffer = default;
                _readWantsWrite = false;

                if (wasWaitingForWrite)
                {
                    UpdateEvents();
                }

                _readAwaitable.TrySetResult(0); // EOF
                return;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // WRITE
    // ═══════════════════════════════════════════════════════════════

    public ValueTask<int> WriteAsync(ReadOnlyMemory<byte> buffer)
    {
        // Serialize the whole transition (see _sslLock remarks): the flag updates and the epoll_ctl issued
        // here must be atomic with respect to the pump thread's OnReadable/OnWritable. The body is inlined
        // here rather than delegated to a helper so it stays synchronous - a stray 'await' would be a CS1996
        // compile error instead of silently releasing the lock mid-transition.
        lock (_sslLock)
        {
            if (!IsHandshaked)
            {
                throw new InvalidOperationException("Handshake not complete");
            }

            if (_writeAwaitable.IsActive)
            {
                throw new InvalidOperationException("Write already pending");
            }

            _writeBuffer = buffer;
            _writeTotal = buffer.Length;
            _writeWantsRead = false;
            _writeWantsWrite = false;

            var status = TlsWrite(_writeBuffer.Span, out var written);

            switch (status)
            {
                case TlsOperationStatus.Complete:
                    _writeBuffer = default;
                    return new ValueTask<int>(_writeTotal);

                case TlsOperationStatus.DestinationTooSmall:
                    // Socket WouldBlock mid-write. 'written' application bytes were consumed;
                    // retry the remainder once the socket is writable.
                    _writeBuffer = _writeBuffer.Slice(written);
                    _writeWantsWrite = true;
                    var pending = _writeAwaitable.Reset();
                    UpdateEvents();
                    StartWriteBackpressureTelemetry();
                    return pending;

                case TlsOperationStatus.NeedMoreData:
                    // Renegotiation: the write needs to read peer ciphertext first. Re-arm interest so EPOLLIN is
                    // present even if the receive loop has suspended it for backpressure.
                    _writeBuffer = _writeBuffer.Slice(written);
                    _writeWantsRead = true;
                    UpdateEvents();
                    return _writeAwaitable.Reset();

                case TlsOperationStatus.Closed:
                default:
                    _writeBuffer = default;
                    return new ValueTask<int>(0); // EOF
            }
        }
    }

    /// <summary>
    /// Advances the pending write against the TLS session when the socket signals writable/readable, either
    /// completing the write awaitable or re-arming epoll interest for a WouldBlock'd remainder / renegotiation input.
    /// </summary>
    /// <remarks>
    /// Unsynchronized: the caller must already hold <c>_sslLock</c> - this method never takes it. It mutates the
    /// write state machine (<c>_writeBuffer</c>, <c>_writeWantsRead</c>/<c>_writeWantsWrite</c>, the write awaitable)
    /// and issues the absolute <c>epoll_ctl</c> via <see cref="UpdateEvents"/>, all of which must stay atomic with
    /// respect to <see cref="WriteAsync"/> and the read side (see the <c>_sslLock</c> remarks). Its only call sites
    /// are inside the <see cref="OnReadable"/>/<see cref="OnWritable"/> lock blocks.
    /// </remarks>
    private void TryCompleteWriteUnsynchronized()
    {
        if (!_writeAwaitable.IsActive)
        {
            // Spurious EPOLLOUT - no write is pending, so drop the write side's writable interest.
            ReleaseWriteBackpressureTelemetry();
            _writeWantsWrite = false;
            UpdateEvents();
            return;
        }

        var status = TlsWrite(_writeBuffer.Span, out var written);

        switch (status)
        {
            case TlsOperationStatus.Complete:
                ReleaseWriteBackpressureTelemetry();
                _writeBuffer = default;
                _writeWantsRead = false;
                _writeWantsWrite = false;
                UpdateEvents();
                _writeAwaitable.TrySetResult(_writeTotal);
                return;

            case TlsOperationStatus.DestinationTooSmall:
                // Still WouldBlock. Advance past what was written and keep waiting for writable.
                _writeBuffer = _writeBuffer.Slice(written);
                if (_writeWantsRead || !_writeWantsWrite)
                {
                    StartWriteBackpressureTelemetry();
                    _writeWantsRead = false;
                    _writeWantsWrite = true;
                    UpdateEvents();
                }
                return;

            case TlsOperationStatus.NeedMoreData:
                // Renegotiation: need to read - drop the write side's EPOLLOUT, stay on baseline EPOLLIN.
                _writeBuffer = _writeBuffer.Slice(written);
                if (!_writeWantsRead)
                {
                    ReleaseWriteBackpressureTelemetry();
                    _writeWantsRead = true;
                    _writeWantsWrite = false;
                    UpdateEvents();
                }
                return;

            case TlsOperationStatus.Closed:
            default:
                ReleaseWriteBackpressureTelemetry();
                _writeBuffer = default;
                _writeWantsRead = false;
                _writeWantsWrite = false;
                UpdateEvents();
                _writeAwaitable.TrySetResult(0); // EOF
                return;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // EVENT HANDLERS (called by pump)
    // ═══════════════════════════════════════════════════════════════

    internal void OnReadable()
    {
        // Inlined under _sslLock and kept synchronous on purpose (see _sslLock remarks): a stray 'await' here
        // would be a CS1996 compile error rather than silently releasing the lock mid-transition.
        lock (_sslLock)
        {
            // A pending write waiting for read (renegotiation) takes priority.
            if (_writeWantsRead && _writeAwaitable.IsActive)
            {
                TryCompleteWriteUnsynchronized();
                return;
            }

            if (_readAwaitable.IsActive)
            {
                TryCompleteReadUnsynchronized();
            }
        }
    }

    internal void OnWritable()
    {
        // Inlined under _sslLock and kept synchronous on purpose (see _sslLock remarks): a stray 'await' here
        // would be a CS1996 compile error rather than silently releasing the lock mid-transition.
        lock (_sslLock)
        {
            // A pending read waiting for write (renegotiation) takes priority.
            if (_readWantsWrite && _readAwaitable.IsActive)
            {
                TryCompleteReadUnsynchronized();
                return;
            }

            if (_writeAwaitable.IsActive)
            {
                TryCompleteWriteUnsynchronized();
            }
        }
    }

    internal void OnError(Exception ex)
    {
        _readAwaitable.TrySetException(ex);
        _writeAwaitable.TrySetException(ex);

        // Notify owner about fatal error so it can trigger disposal.
        OnFatalError?.Invoke(ex);
    }

    /// <summary>
    /// Cancel any pending async operations (read/write awaitables).
    /// Called during connection disposal to unblock waiting tasks.
    /// </summary>
    internal void Cancel()
    {
        string? resumedConnectionId = null;

        // Hold _sslLock so cancellation waits out any in-flight SSL_read/SSL_write (both run under it):
        // completing the awaitable while the pump is mid SSL_read would let the receive loop complete the pipe
        // and recycle _readBuffer while the native write is still filling it (mirror race: SSL_write reading a
        // recycled output block). Once we hold the lock no native call is in flight, and the sticky Canceled
        // state set below stops the pump from starting a new one against the now-recycled buffer.
        lock (_sslLock)
        {
            // Sticky cancellation: completes any parked wait now and makes every future Reset() return an
            // already-cancelled result, so a loop that re-arms concurrently with disposal cannot park forever
            // on an fd DisposeAsync is about to unregister from epoll.
            _readAwaitable.Cancel();
            _writeAwaitable.Cancel();

            // DisposeAsync unregisters the fd before it cancels a backpressured pipe flush. Release the gauge
            // here so the receive loop's eventual ResumeReadInterest does not try to re-arm an unregistered fd
            // and leave the connection counted as paused after teardown.
            if (_readBackpressureCounterEnabled is not null)
            {
                resumedConnectionId = _connection?.ConnectionId;
            }

            _readInterestSuspended = false;
            ReleaseReadBackpressureTelemetry();
            ReleaseWriteBackpressureTelemetry();
        }

        if (resumedConnectionId is not null)
        {
            DirectTlsLog.ConnectionResume(_logger, resumedConnectionId);
        }
    }

    public void Dispose()
    {
        // Best-effort graceful write-side shutdown (TLS close_notify), then dispose the session, which
        // closes the underlying socket fd (the session owns the SafeSocketHandle).
        //
        // Concurrency note: this can race with the pump thread, which may still be inside RawRead/RawWrite
        // (i.e. TlsSocketSession.Read/Write) when DisposeAsync gets here. That is memory-safe because the
        // runtime's TlsSession passes its SSL* as a SafeSslHandle to every P/Invoke (SslRead/SslWrite), and
        // the socket fd is a SafeSocketHandle. Disposing those handles only *decrements* the ref count, so
        // the native SSL_free/close is deferred until any in-flight native call releases its DangerousAddRef.
        // The worst outcome of the race is a spurious ObjectDisposedException on the pump thread's next
        // native call, which TlsEventPump.PumpLoop already catches and turns into an ordinary connection drop.
        // Take _sslLock so the close_notify is not emitted while another thread is mid SSL_read/SSL_write,
        // which would otherwise corrupt the record stream (the "application data after close notify" alert).
        lock (_sslLock)
        {
            try
            {
                ShutdownSession();
            }
            finally
            {
                DisposeSession();
            }
        }
    }
}

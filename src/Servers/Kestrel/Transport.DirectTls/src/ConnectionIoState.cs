// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Authentication;
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
    private readonly ILogger? _logger;
    private readonly TlsSocketSession _session;

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

    public ConnectionIoState(int fd, TlsSocketSession session, ILogger? logger = null)
    {
        _logger = logger;

        Fd = fd;
        _session = session;
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
        => _session.Read(buffer, out bytesRead);

    internal virtual TlsOperationStatus RawWrite(ReadOnlySpan<byte> buffer, out int bytesWritten)
        => _session.Write(buffer, out bytesWritten);

    // Applies the computed epoll interest. Virtual so tests can observe the mask without a live pump
    internal virtual void ApplyEvents(uint events)
    {
        Pump?.ModifyEvents(Fd, events);
    }

    // Reads decrypted application bytes into buffer. NeedMoreData -> need more ciphertext (wait readable);
    // DestinationTooSmall -> renegotiation must flush handshake output (wait writable).
    private TlsOperationStatus TlsRead(Span<byte> buffer, out int bytesRead)
    {
        try
        {
            var status = RawRead(buffer, out bytesRead);
            return status is TlsOperationStatus.Complete or TlsOperationStatus.NeedMoreData
                or TlsOperationStatus.DestinationTooSmall or TlsOperationStatus.Closed
                ? status
                : throw new TlsException($"TLS read failed: {status}");
        }
        catch (AuthenticationException)
        {
            // Abrupt peer close (SSL_ERROR_SYSCALL: ECONNRESET / no close_notify) surfaces as
            // AuthenticationException from the runtime. Treat as EOF.
            bytesRead = 0;
            return TlsOperationStatus.Closed;
        }
    }

    // Writes application bytes from buffer. DestinationTooSmall -> socket WouldBlock (wait writable);
    // NeedMoreData -> renegotiation must read peer ciphertext first (wait readable).
    private TlsOperationStatus TlsWrite(ReadOnlySpan<byte> buffer, out int bytesWritten)
    {
        try
        {
            var status = RawWrite(buffer, out bytesWritten);
            return status is TlsOperationStatus.Complete or TlsOperationStatus.NeedMoreData
                or TlsOperationStatus.DestinationTooSmall or TlsOperationStatus.Closed
                ? status
                : throw new TlsException($"TLS write failed: {status}");
        }
        catch (AuthenticationException)
        {
            bytesWritten = 0;
            return TlsOperationStatus.Closed;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // EPOLL INTEREST
    // ═══════════════════════════════════════════════════════════════

    // Applies the epoll interest as the UNION of what the read and write sides currently need. EPOLLIN is
    // always requested (the receive loop is the connection's steady-state reader, and the pump adds EPOLLRDHUP);
    // EPOLLOUT is requested whenever either side is waiting for the socket to become writable - a write that hit
    // WouldBlock (_writeWantsWrite) or a read flushing renegotiation output (_readWantsWrite). Because
    // ModifyEvents sets the interest absolutely (EPOLL_CTL_MOD replaces, it does not OR), computing the combined
    // mask in one place is what prevents one side's transition from silently dropping an EPOLLOUT the other side
    // is still waiting on, which would otherwise wedge that operation until the connection dies. This is only
    // ever called on EPOLLOUT-interest transitions (never on the steady-state WANT_READ path), so it adds no
    // syscall to normal reads.
    private void UpdateEvents()
    {
        uint events = NativeTls.EPOLLIN;

        if (_readWantsWrite || _writeWantsWrite)
        {
            events |= NativeTls.EPOLLOUT;
        }

        ApplyEvents(events);
    }

    // ═══════════════════════════════════════════════════════════════
    // READ
    // ═══════════════════════════════════════════════════════════════

    public ValueTask<int> ReadAsync(Memory<byte> buffer)
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

    private void TryCompleteRead()
    {
        if (!_readAwaitable.IsActive)
        {
            _logger?.LogDebug("TryCompleteRead called but no read is pending");
            return; // Race: cancelled or completed between check and call
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
                _readBuffer = default;
                _readWantsWrite = false;
                _readAwaitable.TrySetResult(0); // EOF
                return;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // WRITE
    // ═══════════════════════════════════════════════════════════════

    public ValueTask<int> WriteAsync(ReadOnlyMemory<byte> buffer)
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
                return pending;

            case TlsOperationStatus.NeedMoreData:
                // Renegotiation: the write needs to read peer ciphertext first.
                _writeBuffer = _writeBuffer.Slice(written);
                _writeWantsRead = true;
                // EPOLLIN is already registered (baseline).
                return _writeAwaitable.Reset();

            case TlsOperationStatus.Closed:
            default:
                _writeBuffer = default;
                return new ValueTask<int>(0); // EOF
        }
    }

    private void TryCompleteWrite()
    {
        if (!_writeAwaitable.IsActive)
        {
            // Spurious EPOLLOUT - no write is pending, so drop the write side's writable interest.
            _writeWantsWrite = false;
            UpdateEvents();
            return;
        }

        var status = TlsWrite(_writeBuffer.Span, out var written);

        switch (status)
        {
            case TlsOperationStatus.Complete:
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
                    _writeWantsRead = true;
                    _writeWantsWrite = false;
                    UpdateEvents();
                }
                return;

            case TlsOperationStatus.Closed:
            default:
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
        // A pending write waiting for read (renegotiation) takes priority.
        if (_writeWantsRead && _writeAwaitable.IsActive)
        {
            TryCompleteWrite();
            return;
        }

        if (_readAwaitable.IsActive)
        {
            TryCompleteRead();
        }
    }

    internal void OnWritable()
    {
        // A pending read waiting for write (renegotiation) takes priority.
        if (_readWantsWrite && _readAwaitable.IsActive)
        {
            TryCompleteRead();
            return;
        }

        if (_writeAwaitable.IsActive)
        {
            TryCompleteWrite();
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
        _readAwaitable.TrySetCanceled();
        _writeAwaitable.TrySetCanceled();
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
        _session.Shutdown();
        _session.Dispose();
    }
}

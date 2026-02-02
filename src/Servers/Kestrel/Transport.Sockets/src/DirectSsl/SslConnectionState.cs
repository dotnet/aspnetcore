// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Uncomment the following line to enable debug counters for SSL diagnostics
// #define DIRECTSSL_DEBUG_COUNTERS

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;

internal sealed class SslConnectionState : IDisposable
{
    private readonly ILogger? _logger;

    public readonly int Fd;
    public readonly IntPtr Ssl;

    // Reference to pump for dynamic event modification
    internal SslEventPump? Pump { get; set; }

    // Callback for fatal errors (e.g., peer disconnect) - allows owner to trigger disposal
    internal Action<Exception>? OnFatalError { get; set; }

    // Handshake - reusable awaitable to avoid TCS allocations
    private readonly SslAwaitable<bool> _handshakeAwaitable = new();
    public bool IsHandshaked { get; private set; }

    // Read - reusable awaitable to avoid TCS allocations
    private readonly SslAwaitable<int> _readAwaitable = new();
    private Memory<byte> _readBuffer;
    private bool _readWantsWrite;  // SSL_read returned WANT_WRITE (renegotiation)

    // Write - reusable awaitable to avoid TCS allocations
    private readonly SslAwaitable<int> _writeAwaitable = new();
    private ReadOnlyMemory<byte> _writeBuffer;
    private bool _writeWantsRead;  // SSL_write returned WANT_READ (renegotiation)

    public SslConnectionState(int fd, IntPtr ssl, ILogger? logger = null)
    {
        _logger = logger;

        Fd = fd;
        Ssl = ssl;
    }

    /// <summary>
    /// Mark handshake as complete (used when handshake was done externally by pump).
    /// </summary>
    internal void SetHandshakeComplete()
    {
        IsHandshaked = true;
    }

    // ═══════════════════════════════════════════════════════════════
    // HANDSHAKE
    // ═══════════════════════════════════════════════════════════════

    public ValueTask HandshakeAsync()
    {
        // Clear any stale errors before handshake
        NativeSsl.ERR_clear_error();
        int n = NativeSsl.SSL_do_handshake(Ssl);

        if (n == 1)
        {
            IsHandshaked = true;
            return ValueTask.CompletedTask;
        }

        int error = NativeSsl.SSL_get_error(Ssl, n);

        if (error == NativeSsl.SSL_ERROR_WANT_READ || error == NativeSsl.SSL_ERROR_WANT_WRITE)
        {
            // Use pooled awaitable instead of allocating new TCS
            var valueTask = _handshakeAwaitable.Reset();
            return new ValueTask(valueTask.AsTask());
        }

        return ValueTask.FromException(new SslException($"Handshake failed: {error}"));
    }

    private void ContinueHandshake()
    {
        // Clear any stale errors before handshake continuation
        NativeSsl.ERR_clear_error();
        int n = NativeSsl.SSL_do_handshake(Ssl);

        if (n == 1)
        {
            IsHandshaked = true;
            _handshakeAwaitable.TrySetResult(true);
            return;
        }

        int error = NativeSsl.SSL_get_error(Ssl, n);

        if (error == NativeSsl.SSL_ERROR_WANT_READ || error == NativeSsl.SSL_ERROR_WANT_WRITE)
        {
            // Keep waiting
            return;
        }

        _handshakeAwaitable.TrySetException(new SslException($"Handshake failed: {error}"));
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

        int n = DoSslRead(buffer);

        if (n > 0)
        {
            return new ValueTask<int>(n);
        }

        if (n == 0)
        {
            return new ValueTask<int>(0); // EOF
        }

        int error = NativeSsl.SSL_get_error(Ssl, n);

        switch (error)
        {
            case NativeSsl.SSL_ERROR_WANT_READ:
                _readBuffer = buffer;
                _readWantsWrite = false;
                return _readAwaitable.Reset();

            case NativeSsl.SSL_ERROR_WANT_WRITE:
                // SSL_read needs to write (TLS renegotiation or post-handshake auth)
                // Register for EPOLLOUT - OnWritable will call TryCompleteRead
                _readBuffer = buffer;
                _readWantsWrite = true;
                Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN | NativeSsl.EPOLLOUT);
                return _readAwaitable.Reset();

            case NativeSsl.SSL_ERROR_ZERO_RETURN:
                // Peer sent close_notify - treat as EOF
#if DIRECTSSL_DEBUG_COUNTERS
                Interlocked.Increment(ref SslEventPump.TotalSslErrorZeroReturn);
#endif
                return new ValueTask<int>(0);

            case NativeSsl.SSL_ERROR_SYSCALL:
#if DIRECTSSL_DEBUG_COUNTERS
                Interlocked.Increment(ref SslEventPump.TotalSslErrorSyscall);
                Interlocked.Increment(ref SslEventPump.TotalSslErrorSyscallImmediate);
                if (n == 0)
                {
                    Interlocked.Increment(ref SslEventPump.TotalSslErrorSyscallRet0);
                }
                else
                {
                    Interlocked.Increment(ref SslEventPump.TotalSslErrorSyscallRetNeg1);
                }
                // Track errno distribution
                if (_lastErrno == 0)
                {
                    Interlocked.Increment(ref SslEventPump.TotalSslErrorSyscallErrno0);
                }
                else if (_lastErrno == 11)
                {
                    Interlocked.Increment(ref SslEventPump.TotalSslErrorSyscallErrno11);
                }
                else
                {
                    Interlocked.Increment(ref SslEventPump.TotalSslErrorSyscallErrnoOther);
                }
#endif

                // For SSL_ERROR_SYSCALL: if n==0 it's unexpected EOF, if n==-1 check errno
                // Use _lastErrno which was captured immediately after SSL_read
                if (n == 0 || _lastErrno == 0 || _lastErrno == 104 /* ECONNRESET */)
                {
                    return new ValueTask<int>(0);  // Treat as EOF
                }
                if (_lastErrno == 11 /* EAGAIN */ || _lastErrno == 115 /* EINPROGRESS */)
                {
                    // No data available - should wait for epoll
                    _readBuffer = buffer;
                    _readWantsWrite = false;
                    return _readAwaitable.Reset();
                }
                // There's an actual error
                return ValueTask.FromException<int>(new SslException($"SSL_read syscall error: errno={_lastErrno}"));

            default:
#if DIRECTSSL_DEBUG_COUNTERS
                Interlocked.Increment(ref SslEventPump.TotalSslErrorOther);
#endif
                return ValueTask.FromException<int>(new SslException($"SSL_read failed: {error}"));
        }
    }

    private void TryCompleteRead()
    {
        if (!_readAwaitable.IsActive)
        {
            _logger?.LogDebug("TryCompleteRead called but no read is pending");
            return; // Race: cancelled or completed between check and call
        }

        int n = DoSslRead(_readBuffer);

        if (n > 0)
        {
            var wasWaitingForWrite = _readWantsWrite;
            _readBuffer = default;
            _readWantsWrite = false;

            // If we were waiting for write, remove EPOLLOUT now that read completed
            if (wasWaitingForWrite)
            {
                Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN);
            }

            _readAwaitable.TrySetResult(n);
            return;
        }

        if (n == 0)
        {
            var wasWaitingForWrite = _readWantsWrite;
            _readBuffer = default;
            _readWantsWrite = false;

            if (wasWaitingForWrite)
            {
                Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN);
            }

            _readAwaitable.TrySetResult(0);
            return;
        }

        int error = NativeSsl.SSL_get_error(Ssl, n);

        switch (error)
        {
            case NativeSsl.SSL_ERROR_WANT_READ:
                // Need to wait for more data - if we were waiting for write, switch back to read
                if (_readWantsWrite)
                {
                    _readWantsWrite = false;
                    Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN);
                }
                return;

            case NativeSsl.SSL_ERROR_WANT_WRITE:
                // Need to write - register for EPOLLOUT if not already
                if (!_readWantsWrite)
                {
                    _readWantsWrite = true;
                    Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN | NativeSsl.EPOLLOUT);
                }
                return;

            case NativeSsl.SSL_ERROR_ZERO_RETURN:
                // Peer sent close_notify - treat as EOF
#if DIRECTSSL_DEBUG_COUNTERS
                Interlocked.Increment(ref SslEventPump.TotalSslErrorZeroReturn);
#endif
                _readBuffer = default;
                _readWantsWrite = false;
                _readAwaitable.TrySetResult(0);
                return;

            case NativeSsl.SSL_ERROR_SYSCALL:
#if DIRECTSSL_DEBUG_COUNTERS
                Interlocked.Increment(ref SslEventPump.TotalSslErrorSyscall);
                Interlocked.Increment(ref SslEventPump.TotalSslErrorSyscallAfterEpoll);
                if (n == 0)
                {
                    Interlocked.Increment(ref SslEventPump.TotalSslErrorSyscallRet0);
                }
                else
                {
                    Interlocked.Increment(ref SslEventPump.TotalSslErrorSyscallRetNeg1);
                }
#endif
                // For SSL_ERROR_SYSCALL: if n==0 it's unexpected EOF, if n==-1 check errno
                // Use _lastErrno which was captured immediately after SSL_read
                if (n == 0 || _lastErrno == 0 || _lastErrno == 104 /* ECONNRESET */)
                {
                    _readBuffer = default;
                    _readWantsWrite = false;
                    _readAwaitable.TrySetResult(0);  // Treat as EOF
                    return;
                }
                if (_lastErrno == 11 /* EAGAIN */ || _lastErrno == 115 /* EINPROGRESS */)
                {
                    // No data available - wait for more (shouldn't happen after epoll wakeup)
                    return;
                }
                _readBuffer = default;
                _readWantsWrite = false;
                _readAwaitable.TrySetException(new SslException($"SSL_read syscall error: errno={_lastErrno}"));
                return;

            case NativeSsl.SSL_ERROR_SSL:
#if DIRECTSSL_DEBUG_COUNTERS
                Interlocked.Increment(ref SslEventPump.TotalSslErrorSsl);
#endif
                _readBuffer = default;
                _readWantsWrite = false;
                _readAwaitable.TrySetException(new SslException($"SSL_read failed: {error}"));
                return;

            default:
#if DIRECTSSL_DEBUG_COUNTERS
                Interlocked.Increment(ref SslEventPump.TotalSslErrorOther);
#endif
                _readBuffer = default;
                _readWantsWrite = false;
                _readAwaitable.TrySetException(new SslException($"SSL_read failed: {error}"));
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

        var n = DoSslWrite(buffer);
        if (n > 0)
        {
#if DIRECTSSL_DEBUG_COUNTERS
            Interlocked.Increment(ref SslEventPump.TotalWriteImmediate);
#endif
            return new ValueTask<int>(n);
        }

        var error = NativeSsl.SSL_get_error(Ssl, n);
        switch (error)
        {
            case NativeSsl.SSL_ERROR_WANT_WRITE:
#if DIRECTSSL_DEBUG_COUNTERS
                Interlocked.Increment(ref SslEventPump.TotalWriteWouldBlock);
#endif
                _writeBuffer = buffer;
                _writeWantsRead = false;

                // Dynamically add EPOLLOUT since the write would block
                Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN | NativeSsl.EPOLLOUT);

                return _writeAwaitable.Reset();

            case NativeSsl.SSL_ERROR_WANT_READ:
                // SSL_write needs to read (TLS renegotiation or post-handshake auth)
                // Stay registered for EPOLLIN - OnReadable will call TryCompleteWrite
                _writeBuffer = buffer;
                _writeWantsRead = true;
                // EPOLLIN is already registered, no need to modify
                return _writeAwaitable.Reset();

            case NativeSsl.SSL_ERROR_ZERO_RETURN:
                // Peer closed connection cleanly - return 0 (EOF)
                return new ValueTask<int>(0);

            case NativeSsl.SSL_ERROR_SYSCALL:
                // Check ERR_peek_error() == 0 to detect clean EOF
                if (NativeSsl.ERR_peek_error() == 0)
                {
                    return new ValueTask<int>(0);  // Treat as EOF
                }

                return ValueTask.FromException<int>(new IOException($"SSL write syscall error"));

            default:
                return ValueTask.FromException<int>(new SslException($"SSL_write failed: {error}"));
        }
    }

    private void TryCompleteWrite()
    {
        if (!_writeAwaitable.IsActive)
        {
            // Spurious EPOLLOUT - remove it to avoid future wakeups
            Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN);
            return;
        }

        var n = DoSslWrite(_writeBuffer);
        if (n > 0)
        {
            var wasWaitingForRead = _writeWantsRead;
            _writeBuffer = default;
            _writeWantsRead = false;

            // Write completed - remove EPOLLOUT if we had it registered
            if (!wasWaitingForRead)
            {
                Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN);
            }

            _writeAwaitable.TrySetResult(n);
            return;
        }

        var error = NativeSsl.SSL_get_error(Ssl, n);
        switch (error)
        {
            case NativeSsl.SSL_ERROR_WANT_WRITE:
                // Need to wait for write - if we were waiting for read, switch to write
                if (_writeWantsRead)
                {
                    _writeWantsRead = false;
                    Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN | NativeSsl.EPOLLOUT);
                }
                return;

            case NativeSsl.SSL_ERROR_WANT_READ:
                // Need to read - remove EPOLLOUT if we had it, stay on EPOLLIN
                if (!_writeWantsRead)
                {
                    _writeWantsRead = true;
                    Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN);
                }
                return;

            case NativeSsl.SSL_ERROR_ZERO_RETURN:
                // Peer closed - return 0
                _writeBuffer = default;
                _writeWantsRead = false;
                Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN);
                _writeAwaitable.TrySetResult(0);
                return;

            default:
                _writeBuffer = default;
                _writeWantsRead = false;
                Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN);
                _writeAwaitable.TrySetException(new SslException($"SSL_write failed: {error}"));
                return;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // EVENT HANDLERS (called by pump)
    // ═══════════════════════════════════════════════════════════════

    internal void OnReadable()
    {
        if (_handshakeAwaitable.IsActive)
        {
            ContinueHandshake();
            return;
        }

        // Check if a pending write was waiting for read (renegotiation)
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
        if (_handshakeAwaitable.IsActive)
        {
            ContinueHandshake();
            return;
        }

        // Check if a pending read was waiting for write (renegotiation)
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
        _handshakeAwaitable.TrySetException(ex);
        _readAwaitable.TrySetException(ex);
        _writeAwaitable.TrySetException(ex);

        // Notify owner about fatal error so it can trigger disposal
        OnFatalError?.Invoke(ex);
    }

    /// <summary>
    /// Cancel any pending async operations (read/write awaitables).
    /// Called during connection disposal to unblock waiting tasks.
    /// </summary>
    internal void Cancel()
    {
        _handshakeAwaitable.TrySetCanceled();
        _readAwaitable.TrySetCanceled();
        _writeAwaitable.TrySetCanceled();
    }

    // ═══════════════════════════════════════════════════════════════
    // SSL OPERATIONS
    // ═══════════════════════════════════════════════════════════════

    private int DoSslRead(Memory<byte> buffer)
    {
        // Clear any stale errors before SSL operation
        NativeSsl.ERR_clear_error();
        unsafe
        {
            fixed (byte* ptr = buffer.Span)
            {
                int result = NativeSsl.SSL_read(Ssl, ptr, buffer.Length);
                // Capture errno immediately after syscall, before any other calls
                _lastErrno = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                return result;
            }
        }
    }

    // Stored errno from the last SSL_read call
    private int _lastErrno;

    private int DoSslWrite(ReadOnlyMemory<byte> buffer)
    {
        // Clear any stale errors before SSL operation
        NativeSsl.ERR_clear_error();
        unsafe
        {
            fixed (byte* ptr = buffer.Span)
            {
                return NativeSsl.SSL_write(Ssl, (byte*)ptr, buffer.Length);
            }
        }
    }

    public void Dispose()
    {
        // Clear any stale errors before shutdown
        NativeSsl.ERR_clear_error();

        // Use quiet shutdown - don't wait for peer's close_notify
        // This is appropriate because:
        // 1. The peer may have already closed the connection (SSL_ERROR_SYSCALL with errno=0)
        // 2. Waiting for close_notify can block or fail if connection is broken
        // 3. Quiet shutdown is set when connection is timed out, errored, or buffered
        NativeSsl.SSL_set_quiet_shutdown(Ssl, 1);

        // Single SSL_shutdown call - with quiet shutdown, this just cleans up locally
        NativeSsl.SSL_shutdown(Ssl);

        NativeSsl.SSL_free(Ssl);
    }
}

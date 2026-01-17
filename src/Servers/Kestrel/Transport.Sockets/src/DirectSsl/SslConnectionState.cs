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

    // Handshake
    private TaskCompletionSource<bool>? _handshakeTcs;
    public bool IsHandshaked { get; private set; }

    // Read
    private TaskCompletionSource<int>? _readTcs;
    private Memory<byte> _readBuffer;
    private bool _readWantsWrite;  // SSL_read returned WANT_WRITE (renegotiation)

    // Write
    private TaskCompletionSource<int>? _writeTcs;
    private ReadOnlyMemory<byte> _writeBuffer;
    private bool _writeWantsRead;  // SSL_write returned WANT_READ (renegotiation)

    public SslConnectionState(int fd, IntPtr ssl, ILogger? logger = null)
    {
        _logger = logger;

        Fd = fd;
        Ssl = ssl;
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
            _handshakeTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return new ValueTask(_handshakeTcs.Task);
        }

        return ValueTask.FromException(new SslException($"Handshake failed: {error}"));
    }

    private void ContinueHandshake()
    {
        var tcs = _handshakeTcs!;

        // Clear any stale errors before handshake continuation
        NativeSsl.ERR_clear_error();
        int n = NativeSsl.SSL_do_handshake(Ssl);

        if (n == 1)
        {
            _handshakeTcs = null;
            IsHandshaked = true;
            tcs.TrySetResult(true);
            return;
        }

        int error = NativeSsl.SSL_get_error(Ssl, n);

        if (error == NativeSsl.SSL_ERROR_WANT_READ || error == NativeSsl.SSL_ERROR_WANT_WRITE)
        {
            // Keep waiting
            return;
        }

        _handshakeTcs = null;
        tcs.TrySetException(new SslException($"Handshake failed: {error}"));
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

        if (_readTcs != null)
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
                _readTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                _readWantsWrite = false;
                return new ValueTask<int>(_readTcs.Task);

            case NativeSsl.SSL_ERROR_WANT_WRITE:
                // SSL_read needs to write (TLS renegotiation or post-handshake auth)
                // Register for EPOLLOUT - OnWritable will call TryCompleteRead
                _readBuffer = buffer;
                _readTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                _readWantsWrite = true;
                Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN | NativeSsl.EPOLLOUT);
                return new ValueTask<int>(_readTcs.Task);
            
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
                    _readTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _readWantsWrite = false;
                    return new ValueTask<int>(_readTcs.Task);
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
        var tcs = _readTcs;
        if (tcs == null)
        {
            _logger?.LogDebug("TryCompleteRead called but no read tcs is pending");
            return; // Race: cancelled or completed between check and call
        }

        int n = DoSslRead(_readBuffer);

        if (n > 0)
        {
            var wasWaitingForWrite = _readWantsWrite;
            _readTcs = null;
            _readBuffer = default;
            _readWantsWrite = false;
            
            // If we were waiting for write, remove EPOLLOUT now that read completed
            if (wasWaitingForWrite)
            {
                Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN);
            }
            
            tcs.TrySetResult(n);
            return;
        }

        if (n == 0)
        {
            var wasWaitingForWrite = _readWantsWrite;
            _readTcs = null;
            _readBuffer = default;
            _readWantsWrite = false;
            
            if (wasWaitingForWrite)
            {
                Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN);
            }
            
            tcs.TrySetResult(0);
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
                _readTcs = null;
                _readBuffer = default;
                _readWantsWrite = false;
                tcs.TrySetResult(0);
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
                    _readTcs = null;
                    _readBuffer = default;
                    _readWantsWrite = false;
                    tcs.TrySetResult(0);  // Treat as EOF
                    return;
                }
                if (_lastErrno == 11 /* EAGAIN */ || _lastErrno == 115 /* EINPROGRESS */)
                {
                    // No data available - wait for more (shouldn't happen after epoll wakeup)
                    return;
                }
                _readTcs = null;
                _readBuffer = default;
                _readWantsWrite = false;
                tcs.TrySetException(new SslException($"SSL_read syscall error: errno={_lastErrno}"));
                return;
            
            case NativeSsl.SSL_ERROR_SSL:
#if DIRECTSSL_DEBUG_COUNTERS
                Interlocked.Increment(ref SslEventPump.TotalSslErrorSsl);
#endif
                _readTcs = null;
                _readBuffer = default;
                _readWantsWrite = false;
                tcs.TrySetException(new SslException($"SSL_read failed: {error}"));
                return;

            default:
#if DIRECTSSL_DEBUG_COUNTERS
                Interlocked.Increment(ref SslEventPump.TotalSslErrorOther);
#endif
                _readTcs = null;
                _readBuffer = default;
                _readWantsWrite = false;
                tcs.TrySetException(new SslException($"SSL_read failed: {error}"));
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

        if (_writeTcs != null)
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
                _writeTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                _writeWantsRead = false;
                
                // Dynamically add EPOLLOUT since the write would block
                Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN | NativeSsl.EPOLLOUT);
                
                return new ValueTask<int>(_writeTcs.Task);

            case NativeSsl.SSL_ERROR_WANT_READ:
                // SSL_write needs to read (TLS renegotiation or post-handshake auth)
                // Stay registered for EPOLLIN - OnReadable will call TryCompleteWrite
                _writeBuffer = buffer;
                _writeTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                _writeWantsRead = true;
                // EPOLLIN is already registered, no need to modify
                return new ValueTask<int>(_writeTcs.Task);

            case NativeSsl.SSL_ERROR_ZERO_RETURN:
                // Peer closed connection cleanly - return 0 (EOF)
                return new ValueTask<int>(0);

            case NativeSsl.SSL_ERROR_SYSCALL:
                // nginx pattern: check ERR_peek_error() == 0 to detect clean EOF
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
        var tcs = _writeTcs;
        if (tcs == null)
        {
            // Spurious EPOLLOUT - remove it to avoid future wakeups
            Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN);
            return;
        }

        var n = DoSslWrite(_writeBuffer);
        if (n > 0)
        {
            var wasWaitingForRead = _writeWantsRead;
            _writeTcs = null;
            _writeBuffer = default;
            _writeWantsRead = false;
            
            // Write completed - remove EPOLLOUT if we had it registered
            if (!wasWaitingForRead)
            {
                Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN);
            }
            
            tcs.TrySetResult(n);
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
                _writeTcs = null;
                _writeBuffer = default;
                _writeWantsRead = false;
                Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN);
                tcs.TrySetResult(0);
                return;

            default:
                _writeTcs = null;
                _writeBuffer = default;
                _writeWantsRead = false;
                Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN);
                tcs.TrySetException(new SslException($"SSL_write failed: {error}"));
                return;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // EVENT HANDLERS (called by pump)
    // ═══════════════════════════════════════════════════════════════

    internal void OnReadable()
    {
        if (_handshakeTcs != null)
        {
            ContinueHandshake();
            return;
        }

        // Check if a pending write was waiting for read (renegotiation)
        if (_writeWantsRead && _writeTcs != null)
        {
            TryCompleteWrite();
            return;
        }

        if (_readTcs != null)
        {
            TryCompleteRead();
        }
    }

    internal void OnWritable()
    {
        if (_handshakeTcs != null)
        {
            ContinueHandshake();
            return;
        }

        // Check if a pending read was waiting for write (renegotiation)
        if (_readWantsWrite && _readTcs != null)
        {
            TryCompleteRead();
            return;
        }

        if (_writeTcs != null)
        {
            TryCompleteWrite();
        }
    }

    internal void OnError(Exception ex)
    {
        _handshakeTcs?.TrySetException(ex);
        _readTcs?.TrySetException(ex);
        _writeTcs?.TrySetException(ex);

        _handshakeTcs = null;
        _readTcs = null;
        _writeTcs = null;
        
        // Notify owner about fatal error so it can trigger disposal
        OnFatalError?.Invoke(ex);
    }

    /// <summary>
    /// Cancel any pending async operations (read/write TCS).
    /// Called during connection disposal to unblock waiting tasks.
    /// </summary>
    internal void Cancel()
    {
        _handshakeTcs?.TrySetCanceled();
        _readTcs?.TrySetCanceled();
        _writeTcs?.TrySetCanceled();

        _handshakeTcs = null;
        _readTcs = null;
        _writeTcs = null;
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
        
        // Use quiet shutdown (nginx's approach) - don't wait for peer's close_notify
        // This is appropriate because:
        // 1. The peer may have already closed the connection (SSL_ERROR_SYSCALL with errno=0)
        // 2. Waiting for close_notify can block or fail if connection is broken
        // 3. nginx sets SSL_set_quiet_shutdown(1) when c->timedout || c->error || c->buffered
        NativeSsl.SSL_set_quiet_shutdown(Ssl, 1);
        
        // Single SSL_shutdown call - with quiet shutdown, this just cleans up locally
        NativeSsl.SSL_shutdown(Ssl);
        
        NativeSsl.SSL_free(Ssl);
    }
}
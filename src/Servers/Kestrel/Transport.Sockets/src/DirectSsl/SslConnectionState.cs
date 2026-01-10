// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;

internal sealed class SslConnectionState : IDisposable
{
    private readonly ILogger? _logger;

    public readonly int Fd;
    public readonly IntPtr Ssl;

    // Reference to pump for dynamic event modification
    internal SslEventPump? Pump { get; set; }

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

            default:
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

            default:
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
            return new ValueTask<int>(n);
        }

        var error = NativeSsl.SSL_get_error(Ssl, n);
        switch (error)
        {
            case NativeSsl.SSL_ERROR_WANT_WRITE:
                _writeBuffer = buffer;
                _writeTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                
                // Dynamically add EPOLLOUT since the write would block
                Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN | NativeSsl.EPOLLOUT);
                
                return new ValueTask<int>(_writeTcs.Task);

            case NativeSsl.SSL_ERROR_ZERO_RETURN:
                // Peer closed connection cleanly - return 0 (EOF)
                return new ValueTask<int>(0);

            case NativeSsl.SSL_ERROR_SYSCALL:
                // Check if it's just a connection reset
                int errno = Marshal.GetLastWin32Error();
                if (errno == 0 || errno == 104 /* ECONNRESET */)
                {
                    return new ValueTask<int>(0);  // Treat as EOF
                }

                return ValueTask.FromException<int>(new IOException($"SSL syscall error: {errno}"));

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
            _writeTcs = null;
            _writeBuffer = default;
            
            // Write completed - remove EPOLLOUT to avoid spurious wakeups
            Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN);
            
            tcs.TrySetResult(n);
            return;
        }

        var error = NativeSsl.SSL_get_error(Ssl, n);
        switch (error)
        {
            case NativeSsl.SSL_ERROR_WANT_WRITE:
                // Keep waiting (EPOLLOUT stays registered)
                return;

            case NativeSsl.SSL_ERROR_ZERO_RETURN:
                // Peer closed - return 0
                _writeTcs = null;
                _writeBuffer = default;
                Pump?.ModifyEvents(Fd, NativeSsl.EPOLLIN);
                tcs.TrySetResult(0);
                return;

            default:
                _writeTcs = null;
                _writeBuffer = default;
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
        unsafe
        {
            fixed (byte* ptr = buffer.Span)
            {
                return NativeSsl.SSL_read(Ssl, ptr, buffer.Length);
            }
        }
    }

    private int DoSslWrite(ReadOnlyMemory<byte> buffer)
    {
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
        // Send close_notify alert for graceful TLS shutdown
        // SSL_shutdown may return 0 (need to call again) or 1 (complete)
        // We call it once - if peer has already closed, that's fine
        NativeSsl.SSL_shutdown(Ssl);
        NativeSsl.SSL_free(Ssl);
    }
}
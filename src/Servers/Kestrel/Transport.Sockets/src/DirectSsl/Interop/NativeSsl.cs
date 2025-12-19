// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Interop;

/// <summary>
/// P/Invoke wrapper for the native async TLS library.
/// 
/// This library provides:
/// - Epoll management for async I/O
/// - Non-blocking SSL handshake with automatic epoll registration
/// - SSL read/write for application data
/// </summary>
internal static partial class NativeSsl
{
    private const string LibName = "libnative_ssl.so";

    // ========================================================================
    // Handshake status codes (must match demo_native.h)
    // ========================================================================
    
    /// <summary>Handshake completed successfully</summary>
    public const int HANDSHAKE_COMPLETE = 0;
    
    /// <summary>Need to wait for socket to be readable</summary>
    public const int HANDSHAKE_WANT_READ = 1;
    
    /// <summary>Need to wait for socket to be writable</summary>
    public const int HANDSHAKE_WANT_WRITE = 2;
    
    /// <summary>Handshake failed</summary>
    public const int HANDSHAKE_ERROR = -1;

    // ========================================================================
    // Epoll Management
    // ========================================================================

    /// <summary>
    /// Create a new epoll instance.
    /// Each async context (or worker) should have its own epoll.
    /// </summary>
    /// <returns>Epoll file descriptor, or -1 on error</returns>
    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int create_epoll();

    /// <summary>
    /// Close an epoll instance.
    /// </summary>
    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void close_epoll(int epoll_fd);

    /// <summary>
    /// Wait for an I/O event on the epoll instance.
    /// This BLOCKS until an event is ready or timeout occurs.
    /// 
    /// Call this from Task.Run() to avoid blocking the async context.
    /// </summary>
    /// <param name="epoll_fd">Epoll instance</param>
    /// <param name="timeout_ms">Timeout in milliseconds (-1 for infinite)</param>
    /// <returns>Ready socket FD, 0 on timeout, -1 on error</returns>
    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int epoll_wait_one(int epoll_fd, int timeout_ms);

    /// <summary>
    /// Remove fd from epoll. Must be called when connection closes.
    /// </summary>
    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int epoll_remove(int epoll_fd, int client_fd);

    /// <summary>
    /// Wait for an I/O event and return both fd and event flags.
    /// </summary>
    /// <param name="epoll_fd">Epoll instance</param>
    /// <param name="timeout_ms">Timeout in milliseconds (-1 for infinite)</param>
    /// <param name="out_fd">Output: ready fd</param>
    /// <param name="out_events">Output: event flags (EPOLLIN, EPOLLOUT, EPOLLHUP, EPOLLERR)</param>
    /// <returns>1: event received, 0: timeout, -1: error</returns>
    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int epoll_wait_one_ex(int epoll_fd, int timeout_ms, out int out_fd, out int out_events);

    // Epoll event flags
    public const int EPOLLIN = 0x001;
    public const int EPOLLOUT = 0x004;
    public const int EPOLLERR = 0x008;
    public const int EPOLLHUP = 0x010;

    // ========================================================================
    // Socket Utilities
    // ========================================================================

    /// <summary>
    /// Set a socket to non-blocking mode.
    /// This is called automatically by ssl_connection_create().
    /// </summary>
    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int set_socket_nonblocking(int fd);

    /// <summary>
    /// Set TCP_NODELAY on socket (disable Nagle's algorithm).
    /// </summary>
    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int set_tcp_nodelay(int fd);

    // ========================================================================
    // SSL Connection Management
    // ========================================================================

    /// <summary>
    /// Create an SSL object for a client connection AND register with epoll.
    /// 
    /// This function:
    /// 1. Makes the socket non-blocking
    /// 2. Creates SSL object from SSL_CTX
    /// 3. Associates SSL with socket FD (SSL_set_fd)
    /// 4. Sets SSL to accept mode (server-side)
    /// 5. Registers with epoll (EPOLL_CTL_ADD with EPOLLIN)
    /// 
    /// Like async_mt: ADD happens here, then only MOD in ssl_try_handshake.
    /// </summary>
    /// <param name="ssl_ctx">SSL context (from SslContext.Handle)</param>
    /// <param name="client_fd">Accepted client socket FD</param>
    /// <param name="epoll_fd">Epoll instance to register with</param>
    /// <returns>SSL pointer, or IntPtr.Zero on error</returns>
    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial IntPtr ssl_connection_create(IntPtr ssl_ctx, int client_fd, int epoll_fd);

    /// <summary>
    /// Destroy an SSL connection (calls SSL_shutdown + SSL_free).
    /// </summary>
    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial void ssl_connection_destroy(IntPtr ssl);

    /// <summary>
    /// Get the socket FD from an SSL object.
    /// </summary>
    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int ssl_get_fd(IntPtr ssl);

    // ========================================================================
    // Core Async Handshake API
    // ========================================================================

    /// <summary>
    /// Try to advance the TLS handshake.
    /// 
    /// This is the CORE function for async TLS:
    /// 1. Calls SSL_do_handshake() internally
    /// 2. If complete: returns HANDSHAKE_COMPLETE
    /// 3. If needs I/O: registers with epoll, returns HANDSHAKE_WANT_READ/WRITE
    /// 4. On error: returns HANDSHAKE_ERROR
    /// 
    /// Usage pattern in C#:
    /// <code>
    /// while (true) {
    ///     int status = NativeSsl.ssl_try_handshake(ssl, fd, epoll);
    ///     if (status == HANDSHAKE_COMPLETE) break;
    ///     if (status == HANDSHAKE_ERROR) throw ...;
    ///     
    ///     // Wait for I/O readiness
    ///     await Task.Run(() => NativeSsl.epoll_wait_one(epoll, -1));
    /// }
    /// </code>
    /// </summary>
    /// <param name="ssl">SSL object</param>
    /// <param name="client_fd">Client socket FD</param>
    /// <param name="epoll_fd">Epoll instance for event registration</param>
    /// <returns>HANDSHAKE_* status code</returns>
    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int ssl_try_handshake(IntPtr ssl, int client_fd, int epoll_fd);

    // ========================================================================
    // SSL Read/Write
    // ========================================================================

    /// <summary>
    /// Read decrypted data from the SSL connection.
    /// 
    /// After handshake completes, use this to receive application data.
    /// SSL_read handles decryption automatically.
    /// </summary>
    /// <param name="ssl">SSL object (handshake must be complete)</param>
    /// <param name="buffer">Buffer to receive data</param>
    /// <param name="buffer_size">Max bytes to read</param>
    /// <returns>
    /// > 0: Bytes read
    /// 0: Connection closed (EOF)
    /// -1: Would block (no data yet)
    /// -2: Error
    /// </returns>
    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static unsafe partial int ssl_read(IntPtr ssl, byte* buffer, int buffer_size);

    /// <summary>
    /// Write data through the SSL connection (encrypts and sends).
    /// </summary>
    /// <param name="ssl">SSL object (handshake must be complete)</param>
    /// <param name="data">Plaintext data to send</param>
    /// <param name="length">Number of bytes</param>
    /// <returns>
    /// > 0: Bytes written
    /// -1: Would block (buffer full)
    /// -2: Error
    /// </returns>
    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static unsafe partial int ssl_write(IntPtr ssl, byte* data, int length);

    // ========================================================================
    // Epoll Registration for I/O
    // ========================================================================

    /// <summary>
    /// Register a socket for read events (EPOLLIN).
    /// Used after SSL_read returns WANT_READ.
    /// </summary>
    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int epoll_register_read(int epoll_fd, int client_fd);

    /// <summary>
    /// Register a socket for write events (EPOLLOUT).
    /// Used after SSL_write returns WANT_WRITE.
    /// </summary>
    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static partial int epoll_register_write(int epoll_fd, int client_fd);

    /// <summary>
    /// Helper to register for read events.
    /// </summary>
    public static void RegisterForRead(int epollFd, int clientFd)
    {
        epoll_register_read(epollFd, clientFd);
    }

    /// <summary>
    /// Helper to register for write events.
    /// </summary>
    public static void RegisterForWrite(int epollFd, int clientFd)
    {
        epoll_register_write(epollFd, clientFd);
    }

    // ========================================================================
    // Error Handling
    // ========================================================================

    /// <summary>
    /// Get the last OpenSSL error message.
    /// </summary>
    [LibraryImport(LibName)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    public static unsafe partial int ssl_get_last_error(byte* buffer, int buffer_size);

    /// <summary>
    /// Get the last OpenSSL error as a string.
    /// </summary>
    public static unsafe string GetLastError()
    {
        const int bufferSize = 512;
        byte* buffer = stackalloc byte[bufferSize];
        int written = ssl_get_last_error(buffer, bufferSize);
        if (written > 0)
        {
            return System.Text.Encoding.UTF8.GetString(buffer, written);
        }
        return "Unknown error";
    }
}

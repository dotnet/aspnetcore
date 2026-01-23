// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// using System.Runtime.InteropServices;

// namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Interop;

// /// <summary>
// /// P/Invoke wrapper for the native async TLS library.
// /// 
// /// This library provides:
// /// - Epoll management for async I/O
// /// - Non-blocking SSL handshake with automatic epoll registration
// /// - SSL read/write for application data
// /// </summary>
// internal static partial class NativeSsl
// {
//     private const string LibName = "libnative_ssl.so";

//     // ========================================================================
//     // Handshake status codes (must match demo_native.h)
//     // ========================================================================

//     /// <summary>Handshake completed successfully</summary>
//     public const int HANDSHAKE_COMPLETE = 0;

//     /// <summary>Need to wait for socket to be readable</summary>
//     public const int HANDSHAKE_WANT_READ = 1;

//     /// <summary>Need to wait for socket to be writable</summary>
//     public const int HANDSHAKE_WANT_WRITE = 2;

//     /// <summary>Handshake failed</summary>
//     public const int HANDSHAKE_ERROR = -1;

//     // ========================================================================
//     // Epoll Management
//     // ========================================================================

//     /// <summary>
//     /// Create a new epoll instance.
//     /// Each async context (or worker) should have its own epoll.
//     /// </summary>
//     /// <returns>Epoll file descriptor, or -1 on error</returns>
//     [LibraryImport(LibName)]
//     [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
//     public static partial int create_epoll();

//     /// <summary>
//     /// Close an epoll instance.
//     /// </summary>
//     [LibraryImport(LibName)]
//     [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
//     public static partial void close_epoll(int epoll_fd);

//     /// <summary>
//     /// Wait for an I/O event on the epoll instance.
//     /// This BLOCKS until an event is ready or timeout occurs.
//     /// 
//     /// Call this from Task.Run() to avoid blocking the async context.
//     /// </summary>
//     /// <param name="epoll_fd">Epoll instance</param>
//     /// <param name="timeout_ms">Timeout in milliseconds (-1 for infinite)</param>
//     /// <returns>Ready socket FD, 0 on timeout, -1 on error</returns>
//     [LibraryImport(LibName)]
//     [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
//     public static partial int epoll_wait_one(int epoll_fd, int timeout_ms);

//     /// <summary>
//     /// Remove fd from epoll. Must be called when connection closes.
//     /// </summary>
//     [LibraryImport(LibName)]
//     [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
//     public static partial int epoll_remove(int epoll_fd, int client_fd);

//     /// <summary>
//     /// Wait for an I/O event and return both fd and event flags.
//     /// </summary>
//     /// <param name="epoll_fd">Epoll instance</param>
//     /// <param name="timeout_ms">Timeout in milliseconds (-1 for infinite)</param>
//     /// <param name="out_fd">Output: ready fd</param>
//     /// <param name="out_events">Output: event flags (EPOLLIN, EPOLLOUT, EPOLLHUP, EPOLLERR)</param>
//     /// <returns>1: event received, 0: timeout, -1: error</returns>
//     [LibraryImport(LibName)]
//     [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
//     public static partial int epoll_wait_one_ex(int epoll_fd, int timeout_ms, out int out_fd, out int out_events);

//     // Epoll event flags
//     public const int EPOLLIN = 0x001;
//     public const int EPOLLOUT = 0x004;
//     public const int EPOLLERR = 0x008;
//     public const int EPOLLHUP = 0x010;

//     // ========================================================================
//     // Socket Utilities
//     // ========================================================================

//     /// <summary>
//     /// Set a socket to non-blocking mode.
//     /// This is called automatically by ssl_connection_create().
//     /// </summary>
//     [LibraryImport(LibName)]
//     [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
//     public static partial int set_socket_nonblocking(int fd);

//     /// <summary>
//     /// Set TCP_NODELAY on socket (disable Nagle's algorithm).
//     /// </summary>
//     [LibraryImport(LibName)]
//     [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
//     public static partial int set_tcp_nodelay(int fd);

//     // ========================================================================
//     // SSL Connection Management
//     // ========================================================================

//     /// <summary>
//     /// Create an SSL object for a client connection AND register with epoll.
//     /// 
//     /// This function:
//     /// 1. Makes the socket non-blocking
//     /// 2. Creates SSL object from SSL_CTX
//     /// 3. Associates SSL with socket FD (SSL_set_fd)
//     /// 4. Sets SSL to accept mode (server-side)
//     /// 5. Registers with epoll (EPOLL_CTL_ADD with EPOLLIN)
//     /// 
//     /// Like async_mt: ADD happens here, then only MOD in ssl_try_handshake.
//     /// </summary>
//     /// <param name="ssl_ctx">SSL context (from SslContext.Handle)</param>
//     /// <param name="client_fd">Accepted client socket FD</param>
//     /// <param name="epoll_fd">Epoll instance to register with</param>
//     /// <returns>SSL pointer, or IntPtr.Zero on error</returns>
//     [LibraryImport(LibName)]
//     [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
//     public static partial IntPtr ssl_connection_create(IntPtr ssl_ctx, int client_fd, int epoll_fd);

//     /// <summary>
//     /// Destroy an SSL connection (calls SSL_shutdown + SSL_free).
//     /// </summary>
//     [LibraryImport(LibName)]
//     [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
//     public static partial void ssl_connection_destroy(IntPtr ssl);

//     /// <summary>
//     /// Get the socket FD from an SSL object.
//     /// </summary>
//     [LibraryImport(LibName)]
//     [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
//     public static partial int ssl_get_fd(IntPtr ssl);

//     // ========================================================================
//     // Core Async Handshake API
//     // ========================================================================

//     /// <summary>
//     /// Try to advance the TLS handshake.
//     /// 
//     /// This is the CORE function for async TLS:
//     /// 1. Calls SSL_do_handshake() internally
//     /// 2. If complete: returns HANDSHAKE_COMPLETE
//     /// 3. If needs I/O: registers with epoll, returns HANDSHAKE_WANT_READ/WRITE
//     /// 4. On error: returns HANDSHAKE_ERROR
//     /// 
//     /// Usage pattern in C#:
//     /// <code>
//     /// while (true) {
//     ///     int status = NativeSsl.ssl_try_handshake(ssl, fd, epoll);
//     ///     if (status == HANDSHAKE_COMPLETE) break;
//     ///     if (status == HANDSHAKE_ERROR) throw ...;
//     ///     
//     ///     // Wait for I/O readiness
//     ///     await Task.Run(() => NativeSsl.epoll_wait_one(epoll, -1));
//     /// }
//     /// </code>
//     /// </summary>
//     /// <param name="ssl">SSL object</param>
//     /// <param name="client_fd">Client socket FD</param>
//     /// <param name="epoll_fd">Epoll instance for event registration</param>
//     /// <returns>HANDSHAKE_* status code</returns>
//     [LibraryImport(LibName)]
//     [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
//     public static partial int ssl_try_handshake(IntPtr ssl, int client_fd, int epoll_fd);

//     // ========================================================================
//     // SSL Read/Write
//     // ========================================================================

//     /// <summary>
//     /// Read decrypted data from the SSL connection.
//     /// 
//     /// After handshake completes, use this to receive application data.
//     /// SSL_read handles decryption automatically.
//     /// </summary>
//     /// <param name="ssl">SSL object (handshake must be complete)</param>
//     /// <param name="buffer">Buffer to receive data</param>
//     /// <param name="buffer_size">Max bytes to read</param>
//     /// <returns>
//     /// > 0: Bytes read
//     /// 0: Connection closed (EOF)
//     /// -1: Would block (no data yet)
//     /// -2: Error
//     /// </returns>
//     [LibraryImport(LibName)]
//     [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
//     public static unsafe partial int ssl_read(IntPtr ssl, byte* buffer, int buffer_size);

//     /// <summary>
//     /// Write data through the SSL connection (encrypts and sends).
//     /// </summary>
//     /// <param name="ssl">SSL object (handshake must be complete)</param>
//     /// <param name="data">Plaintext data to send</param>
//     /// <param name="length">Number of bytes</param>
//     /// <returns>
//     /// > 0: Bytes written
//     /// -1: Would block (buffer full)
//     /// -2: Error
//     /// </returns>
//     [LibraryImport(LibName)]
//     [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
//     public static unsafe partial int ssl_write(IntPtr ssl, byte* data, int length);

//     // ========================================================================
//     // Epoll Registration for I/O
//     // ========================================================================

//     /// <summary>
//     /// Register a socket for read events (EPOLLIN).
//     /// Used after SSL_read returns WANT_READ.
//     /// </summary>
//     [LibraryImport(LibName)]
//     [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
//     public static partial int epoll_register_read(int epoll_fd, int client_fd);

//     /// <summary>
//     /// Register a socket for write events (EPOLLOUT).
//     /// Used after SSL_write returns WANT_WRITE.
//     /// </summary>
//     [LibraryImport(LibName)]
//     [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
//     public static partial int epoll_register_write(int epoll_fd, int client_fd);

//     /// <summary>
//     /// Helper to register for read events.
//     /// </summary>
//     public static void RegisterForRead(int epollFd, int clientFd)
//     {
//         epoll_register_read(epollFd, clientFd);
//     }

//     /// <summary>
//     /// Helper to register for write events.
//     /// </summary>
//     public static void RegisterForWrite(int epollFd, int clientFd)
//     {
//         epoll_register_write(epollFd, clientFd);
//     }

//     // ========================================================================
//     // Error Handling
//     // ========================================================================

//     /// <summary>
//     /// Get the last OpenSSL error message.
//     /// </summary>
//     [LibraryImport(LibName)]
//     [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
//     public static unsafe partial int ssl_get_last_error(byte* buffer, int buffer_size);

//     /// <summary>
//     /// Get the last OpenSSL error as a string.
//     /// </summary>
//     public static unsafe string GetLastError()
//     {
//         const int bufferSize = 512;
//         byte* buffer = stackalloc byte[bufferSize];
//         int written = ssl_get_last_error(buffer, bufferSize);
//         if (written > 0)
//         {
//             return System.Text.Encoding.UTF8.GetString(buffer, written);
//         }
//         return "Unknown error";
//     }
// }

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Interop;

internal static partial class NativeSsl
{
    private const string LIBSSL = "libssl.so.3";
    private const string LIBC = "libc.so.6";

    // SSL
    [LibraryImport(LIBSSL)] public static partial IntPtr SSL_CTX_new(IntPtr method);
    [LibraryImport(LIBSSL)] public static partial IntPtr TLS_server_method();
    [LibraryImport(LIBSSL, StringMarshalling = StringMarshalling.Utf8)] public static partial int SSL_CTX_use_certificate_file(IntPtr ctx, string file, int type);
    [LibraryImport(LIBSSL, StringMarshalling = StringMarshalling.Utf8)] public static partial int SSL_CTX_use_PrivateKey_file(IntPtr ctx, string file, int type);
    [LibraryImport(LIBSSL)] public static partial IntPtr SSL_new(IntPtr ctx);
    [LibraryImport(LIBSSL)] public static partial int SSL_set_fd(IntPtr ssl, int fd);
    [LibraryImport(LIBSSL)] public static partial void SSL_set_accept_state(IntPtr ssl);
    [LibraryImport(LIBSSL)] public static partial int SSL_do_handshake(IntPtr ssl);
    [LibraryImport(LIBSSL)] public static partial int SSL_get_error(IntPtr ssl, int ret);
    [LibraryImport(LIBSSL, SetLastError = true)] public static unsafe partial int SSL_read(IntPtr ssl, byte* buf, int num);
    [LibraryImport(LIBSSL, SetLastError = true)] public static unsafe partial int SSL_write(IntPtr ssl, byte* buf, int num);
    [LibraryImport(LIBSSL)] public static partial int SSL_shutdown(IntPtr ssl);
    [LibraryImport(LIBSSL)] public static partial void SSL_set_quiet_shutdown(IntPtr ssl, int mode);
    [LibraryImport(LIBSSL)] public static partial void SSL_free(IntPtr ssl);
    [LibraryImport(LIBSSL)] public static partial void SSL_CTX_free(IntPtr ctx);
    
    // Error handling - libcrypto
    private const string LIBCRYPTO = "libcrypto.so.3";
    [LibraryImport(LIBCRYPTO)] public static partial void ERR_clear_error();
    [LibraryImport(LIBCRYPTO)] public static partial ulong ERR_peek_error();
    [LibraryImport(LIBCRYPTO)] public static partial ulong ERR_get_error();
    [LibraryImport(LIBCRYPTO)] public static unsafe partial void ERR_error_string_n(ulong e, byte* buf, nuint len);
    
    /// <summary>
    /// Get the latest OpenSSL error as a string.
    /// </summary>
    public static unsafe string GetErrorString()
    {
        ulong err = ERR_get_error();
        if (err == 0)
        {
            return "No error";
        }
        
        byte* buf = stackalloc byte[256];
        ERR_error_string_n(err, buf, 256);
        return Marshal.PtrToStringUTF8((IntPtr)buf) ?? "Unknown error";
    }

    // SSL error codes
    public const int SSL_ERROR_NONE = 0;
    public const int SSL_ERROR_SSL = 1;
    public const int SSL_ERROR_WANT_READ = 2;
    public const int SSL_ERROR_WANT_WRITE = 3;
    public const int SSL_ERROR_WANT_X509_LOOKUP = 4;
    public const int SSL_ERROR_SYSCALL = 5;
    public const int SSL_ERROR_ZERO_RETURN = 6;
    public const int SSL_ERROR_WANT_CONNECT = 7;
    public const int SSL_ERROR_WANT_ACCEPT = 8;

    // SSL file types
    public const int SSL_FILETYPE_PEM = 1;

    // Epoll
    [LibraryImport(LIBC)] public static partial int epoll_create1(int flags);
    [LibraryImport(LIBC)] public static partial int epoll_ctl(int epfd, int op, int fd, ref EpollEvent ev);
    [LibraryImport(LIBC)] public static partial int epoll_ctl(int epfd, int op, int fd, IntPtr ev);
    [LibraryImport(LIBC)] public static partial int epoll_wait(int epfd, EpollEvent[] events, int maxevents, int timeout);
    [LibraryImport(LIBC)] public static partial int close(int fd);
    [LibraryImport(LIBC)] public static partial int fcntl(int fd, int cmd, int arg);

    // Epoll constants
    public const int EPOLL_CTL_ADD = 1;
    public const int EPOLL_CTL_DEL = 2;
    public const int EPOLL_CTL_MOD = 3;
    public const uint EPOLLIN = 0x001;
    public const uint EPOLLOUT = 0x004;
    public const uint EPOLLERR = 0x008;
    public const uint EPOLLHUP = 0x010;
    public const uint EPOLLET = 0x80000000;
    public const uint EPOLLRDHUP = 0x2000;
    public const uint EPOLLEXCLUSIVE = 0x10000000;  // Prevents thundering herd - only one worker wakes per event

    // Socket accept
    public const int SOCK_NONBLOCK = 0x800;  // O_NONBLOCK for socket
    [LibraryImport(LIBC, SetLastError = true)]
    public static unsafe partial int accept4(int sockfd, void* addr, void* addrlen, int flags);
    
    /// <summary>
    /// Accept a connection from the listen socket using accept4 with SOCK_NONBLOCK.
    /// Returns the client fd on success, -1 if EAGAIN (no pending connections), or -2 on error.
    /// </summary>
    public static unsafe int AcceptNonBlocking(int listenFd)
    {
        int clientFd = accept4(listenFd, null, null, SOCK_NONBLOCK);
        if (clientFd < 0)
        {
            int errno = Marshal.GetLastWin32Error();
            // EAGAIN (11) or EWOULDBLOCK (same on Linux) - no pending connections
            if (errno == 11)
            {
                return -1;
            }
            // Other error
            return -2;
        }
        return clientFd;
    }

    // fcntl
    public const int F_GETFL = 3;
    public const int F_SETFL = 4;
    public const int O_NONBLOCK = 2048;
    
    // Socket options
    public const int SOL_TCP = 6;
    public const int TCP_NODELAY = 1;
    [LibraryImport(LIBC, SetLastError = true)]
    public static unsafe partial int setsockopt(int sockfd, int level, int optname, void* optval, int optlen);
    
    /// <summary>
    /// Set TCP_NODELAY on a socket to disable Nagle's algorithm.
    /// </summary>
    public static unsafe void SetTcpNoDelay(int fd)
    {
        int optval = 1;
        setsockopt(fd, SOL_TCP, TCP_NODELAY, &optval, sizeof(int));
    }

    public static void SetNonBlocking(int fd)
    {
        int flags = fcntl(fd, F_GETFL, 0);
        fcntl(fd, F_SETFL, flags | O_NONBLOCK);
    }
}
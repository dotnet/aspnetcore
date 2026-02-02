// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    // sockaddr structures for accept4 with address capture
    public const int AF_INET = 2;
    public const int AF_INET6 = 10;

    [StructLayout(LayoutKind.Sequential)]
    public struct SockAddrIn
    {
        public ushort sin_family;
        public ushort sin_port;      // Network byte order (big-endian)
        public uint sin_addr;        // Network byte order (big-endian)
        public ulong sin_zero;       // Padding
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SockAddrIn6
    {
        public ushort sin6_family;
        public ushort sin6_port;     // Network byte order (big-endian)
        public uint sin6_flowinfo;
        public fixed byte sin6_addr[16];
        public uint sin6_scope_id;
    }

    // Union-like storage for sockaddr (large enough for IPv6)
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SockAddrStorage
    {
        public ushort ss_family;
        public fixed byte data[126];  // Large enough for any sockaddr
    }

    /// <summary>
    /// Accept a connection from the listen socket using accept4 with SOCK_NONBLOCK.
    /// Also captures the peer address to avoid a separate getpeername syscall.
    /// Returns the client fd on success, -1 if EAGAIN (no pending connections), or -2 on error.
    /// </summary>
    public static unsafe (int fd, System.Net.IPEndPoint? remoteEndPoint) AcceptNonBlockingWithPeerAddress(int listenFd)
    {
        SockAddrStorage addr = default;
        int addrLen = sizeof(SockAddrStorage);

        int clientFd = accept4(listenFd, &addr, &addrLen, SOCK_NONBLOCK);
        if (clientFd < 0)
        {
            int errno = Marshal.GetLastWin32Error();
            // EAGAIN (11) or EWOULDBLOCK (same on Linux) - no pending connections
            if (errno == 11)
            {
                return (-1, null);
            }
            // Other error
            return (-2, null);
        }

        // Parse the sockaddr to IPEndPoint
        System.Net.IPEndPoint? remoteEndPoint = null;
        try
        {
            if (addr.ss_family == AF_INET)
            {
                // IPv4
                var addr4 = *(SockAddrIn*)&addr;
                // Convert from network byte order to host byte order
                ushort port = (ushort)System.Net.IPAddress.NetworkToHostOrder((short)addr4.sin_port);
                var ipBytes = BitConverter.GetBytes(addr4.sin_addr);
                var ipAddress = new System.Net.IPAddress(ipBytes);
                remoteEndPoint = new System.Net.IPEndPoint(ipAddress, port);
            }
            else if (addr.ss_family == AF_INET6)
            {
                // IPv6
                var addr6 = *(SockAddrIn6*)&addr;
                ushort port = (ushort)System.Net.IPAddress.NetworkToHostOrder((short)addr6.sin6_port);
                var ipBytes = new byte[16];
                for (int i = 0; i < 16; i++)
                {
                    ipBytes[i] = addr6.sin6_addr[i];
                }
                var ipAddress = new System.Net.IPAddress(ipBytes, addr6.sin6_scope_id);
                remoteEndPoint = new System.Net.IPEndPoint(ipAddress, port);
            }
        }
        catch
        {
            // If we can't parse the address, just return null - connection still works
        }

        return (clientFd, remoteEndPoint);
    }

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

    // Socket shutdown constants
    public const int SHUT_RD = 0;
    public const int SHUT_WR = 1;
    public const int SHUT_RDWR = 2;

    [LibraryImport(LIBC, SetLastError = true)]
    public static partial int shutdown(int sockfd, int how);

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

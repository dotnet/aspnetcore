// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Interop;

internal static partial class NativeTls
{
    private const string LIBC = "libc.so.6";

    // Epoll. SetLastError = true is required so Marshal.GetLastPInvokeError() returns the real errno
    // after a failed call; [LibraryImport] defaults to SetLastError = false, which would otherwise leave
    // callers (notably the EINTR retry in TlsEventPump.PumpLoop) reading a stale/garbage value.
    [LibraryImport(LIBC, SetLastError = true)] public static partial int epoll_create1(int flags);
    [LibraryImport(LIBC, SetLastError = true)] public static partial int epoll_ctl(int epfd, int op, int fd, ref EpollEvent ev);
    [LibraryImport(LIBC, SetLastError = true)] public static partial int epoll_ctl(int epfd, int op, int fd, IntPtr ev);
    [LibraryImport(LIBC, SetLastError = true)] public static partial int epoll_wait(int epfd, EpollEvent[] events, int maxevents, int timeout);
    [LibraryImport(LIBC, SetLastError = true)] public static partial int close(int fd);

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
}

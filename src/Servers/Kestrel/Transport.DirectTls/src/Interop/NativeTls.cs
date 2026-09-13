// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Interop;

internal static partial class NativeTls
{
    private const string LIBC = "libc";

    // The native epoll_event layout differs by architecture: x86_64/i386 pack the struct (12 bytes, data at
    // offset 4) while every other architecture aligns it naturally (16 bytes, data at offset 8). A single
    // managed [StructLayout] cannot satisfy both, and one assembly is loaded on every architecture, so the
    // matching native struct (EpollEventPacked or EpollEventAligned) is selected at runtime.
    private static readonly bool s_packedEpoll =
        RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.X86;

    // Exposed for tests: true when this architecture uses the 12-byte packed epoll_event layout.
    internal static bool UsesPackedEpollLayout => s_packedEpoll;

    // Epoll. SetLastError = true is required so Marshal.GetLastPInvokeError() returns the real errno
    // after a failed call; [LibraryImport] defaults to SetLastError = false, which would otherwise leave
    // callers (notably the EINTR retry in TlsEventPump.PumpLoop) reading a stale/garbage value.
    [LibraryImport(LIBC, SetLastError = true)] public static partial int epoll_create1(int flags);
    [LibraryImport(LIBC, SetLastError = true)] public static partial int epoll_ctl(int epfd, int op, int fd, IntPtr ev);
    [LibraryImport(LIBC, SetLastError = true)] public static partial int close(int fd);

    // accept4: accept a pending connection and return a new fd, or -1 with errno set. addr/addrlen are passed
    // as IntPtr.Zero (the peer is read later via the wrapped managed Socket's RemoteEndPoint, so no sockaddr is
    // parsed here). SOCK_NONBLOCK sets the accepted fd non-blocking atomically and SOCK_CLOEXEC sets close-on-exec
    // atomically (so the fd is not inherited across exec), both saving a follow-up fcntl.
    // Reporting EAGAIN/EBADF/EINTR as errno return values lets the accept loop drain without exceptions.
    [LibraryImport(LIBC, SetLastError = true)] public static partial int accept4(int sockfd, IntPtr addr, IntPtr addrlen, int flags);

    // eventfd: the pump's cross-thread wakeup. A thread pool thread may write to same fd and make it wake up on epoll_wait.
    // The counter is drained with a single 8-byte read (EFD_NONBLOCK makes an empty read return EAGAIN rather than block the pump).
    [LibraryImport(LIBC, SetLastError = true)] public static partial int eventfd(uint initval, int flags);

    [LibraryImport(LIBC, SetLastError = true)] public static partial nint read(int fd, ref long buf, nuint count);

    [LibraryImport(LIBC, SetLastError = true)] public static partial nint write(int fd, ref long buf, nuint count);

    [LibraryImport(LIBC, SetLastError = true)]
    private static partial int epoll_ctl(int epfd, int op, int fd, ref EpollEventPacked ev);

    [LibraryImport(LIBC, SetLastError = true)]
    private static partial int epoll_ctl(int epfd, int op, int fd, ref EpollEventAligned ev);

    [LibraryImport(LIBC, SetLastError = true)]
    private static partial int epoll_wait(int epfd, EpollEventPacked[] events, int maxevents, int timeout);

    [LibraryImport(LIBC, SetLastError = true)]
    private static partial int epoll_wait(int epfd, EpollEventAligned[] events, int maxevents, int timeout);

    // Registers/modifies interest using the architecture-correct native epoll_event struct.
    public static int epoll_ctl(int epfd, int op, int fd, ref EpollEvent ev)
    {
        if (s_packedEpoll)
        {
            var native = new EpollEventPacked { Events = ev.Events, Data = ev.Data.U64 };
            return epoll_ctl(epfd, op, fd, ref native);
        }
        else
        {
            var native = new EpollEventAligned { Events = ev.Events, Data = ev.Data.U64 };
            return epoll_ctl(epfd, op, fd, ref native);
        }
    }

    // Reusable epoll_wait result buffer that holds whichever native array matches the architecture ABI and
    // exposes entries as the logical EpollEvent, keeping the pump loop free of any per-entry layout branch.
    internal sealed class EpollEventBuffer
    {
        private readonly EpollEventPacked[]? _packed;
        private readonly EpollEventAligned[]? _aligned;

        public EpollEventBuffer(int maxEvents)
        {
            if (s_packedEpoll)
            {
                _packed = new EpollEventPacked[maxEvents];
            }
            else
            {
                _aligned = new EpollEventAligned[maxEvents];
            }
        }

        public int Wait(int epfd, int timeout)
            => _packed is not null
                ? epoll_wait(epfd, _packed, _packed.Length, timeout)
                : epoll_wait(epfd, _aligned!, _aligned!.Length, timeout);

        public EpollEvent this[int index]
            => _packed is not null
                ? new EpollEvent { Events = _packed[index].Events, Data = new EpollData { U64 = _packed[index].Data } }
                : new EpollEvent { Events = _aligned![index].Events, Data = new EpollData { U64 = _aligned[index].Data } };
    }

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

    // accept4 / epoll_create1 flags. Both CLOEXEC values equal Linux O_CLOEXEC (0x80000) on the architectures
    // this transport targets (x64/arm64/arm). SOCK_NONBLOCK sets the accepted fd non-blocking atomically;
    // SOCK_CLOEXEC / EPOLL_CLOEXEC set close-on-exec atomically so accepted connection fds and the epoll fd are
    // not inherited by a child process across exec (matching the runtime's own socket layer).
    public const int SOCK_NONBLOCK = 0x800;    // Linux O_NONBLOCK
    public const int SOCK_CLOEXEC = 0x80000;   // Linux O_CLOEXEC
    public const int EPOLL_CLOEXEC = 0x80000;  // Linux O_CLOEXEC

    // eventfd flags (Linux eventfd2). Same values as O_NONBLOCK / O_CLOEXEC.
    public const int EFD_NONBLOCK = 0x800;
    public const int EFD_CLOEXEC = 0x80000;

    // errno values the accept loop distinguishes (Linux asm-generic/errno-base.h). EWOULDBLOCK == EAGAIN on Linux.
    public const int EINTR = 4;    // interrupted by a signal before a connection was accepted - retry
    public const int EBADF = 9;    // listen fd closed underneath the pump during shutdown
    public const int EAGAIN = 11;  // backlog drained (non-blocking accept has nothing pending)
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Interop;

internal static partial class NativeTls
{
    private const string LIBC = "libc.so.6";

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

        public int MaxEvents => _packed?.Length ?? _aligned!.Length;

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
}

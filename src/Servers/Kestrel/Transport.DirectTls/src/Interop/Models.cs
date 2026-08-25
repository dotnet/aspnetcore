// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Interop;

// Architecture-agnostic view of a native epoll_event used throughout the transport. NativeTls converts it
// to/from the architecture-correct native struct (EpollEventPacked on x86_64, EpollEventAligned elsewhere)
// at the epoll_ctl/epoll_wait boundary.
internal struct EpollEvent
{
    public uint Events;
    public EpollData Data;
}

[StructLayout(LayoutKind.Explicit)]
internal struct EpollData
{
    [FieldOffset(0)] public int Fd;
    [FieldOffset(0)] public long U64;
}

// Native epoll_event as laid out on x86_64: glibc/kernel mark it __attribute__((packed)) (EPOLL_PACKED), so
// the 8-byte data union sits at offset 4 for a 12-byte struct. i386 lands data at the same offset via 4-byte
// alignment of the 64-bit union, so it shares this layout.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct EpollEventPacked
{
    public uint Events;
    public long Data;
}

// Native epoll_event on every other architecture (arm64, arm, ...): natural 8-byte alignment inserts 4 bytes
// of padding after events, placing data at offset 8 for a 16-byte struct.
[StructLayout(LayoutKind.Sequential)]
internal struct EpollEventAligned
{
    public uint Events;
    public long Data;
}

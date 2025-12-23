// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Interop;

[StructLayout(LayoutKind.Sequential)]
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
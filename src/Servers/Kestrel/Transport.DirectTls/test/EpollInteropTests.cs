// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Interop;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Tests;

/// <summary>
/// Guards the architecture-correct native epoll_event marshalling in <see cref="NativeTls"/>. A single managed
/// layout can't be right on every architecture: x86_64/i386 pack the struct (12 bytes, data at offset 4) while
/// arm64 and others align naturally (16 bytes, data at offset 8), so the matching native struct is picked at
/// runtime. The layout tests are pure managed and run anywhere; the epoll round-trip is Linux-only.
/// </summary>
public class EpollInteropTests
{
    [Fact]
    public void EpollEventPacked_HasX86_64Layout()
    {
        Assert.Equal(12, Marshal.SizeOf<EpollEventPacked>());
        Assert.Equal(4, (int)Marshal.OffsetOf<EpollEventPacked>(nameof(EpollEventPacked.Data)));
    }

    [Fact]
    public void EpollEventAligned_HasNaturallyAlignedLayout()
    {
        Assert.Equal(16, Marshal.SizeOf<EpollEventAligned>());
        Assert.Equal(8, (int)Marshal.OffsetOf<EpollEventAligned>(nameof(EpollEventAligned.Data)));
    }

    [Fact]
    public void SelectedLayout_MatchesArchitecture()
    {
        bool expectPacked = RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.X86;
        Assert.Equal(expectPacked, NativeTls.UsesPackedEpollLayout);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void EpollWait_RoundTripsRegisteredFdAndEvents()
    {
        // End-to-end proof that epoll_ctl encoding and EpollEventBuffer decoding agree with the kernel ABI on
        // the architecture actually running the test: register a readable eventfd, then read it back verbatim.
        int epfd = NativeTls.epoll_create1(0);
        Assert.True(epfd >= 0, $"epoll_create1 failed: errno={Marshal.GetLastWin32Error()}");

        int efd = EventFdNative.eventfd(0, 0);
        Assert.True(efd >= 0, $"eventfd failed: errno={Marshal.GetLastWin32Error()}");

        try
        {
            // Writing a non-zero counter makes the eventfd readable so epoll_wait returns immediately.
            byte[] one = BitConverter.GetBytes(1L);
            Assert.Equal(8, (int)EventFdNative.write(efd, one, (nuint)one.Length));

            var registration = new EpollEvent { Events = NativeTls.EPOLLIN, Data = new EpollData { Fd = efd } };
            Assert.Equal(0, NativeTls.epoll_ctl(epfd, NativeTls.EPOLL_CTL_ADD, efd, ref registration));

            var buffer = new NativeTls.EpollEventBuffer(8);
            int count = buffer.Wait(epfd, 1000);

            Assert.Equal(1, count);
            var entry = buffer[0];
            Assert.Equal(efd, entry.Data.Fd);
            Assert.True((entry.Events & NativeTls.EPOLLIN) != 0);
        }
        finally
        {
            NativeTls.close(efd);
            NativeTls.close(epfd);
        }
    }
}

internal static partial class EventFdNative
{
    private const string LIBC = "libc.so.6";

    [LibraryImport(LIBC, SetLastError = true)]
    internal static partial int eventfd(uint initval, int flags);

    [LibraryImport(LIBC, SetLastError = true)]
    internal static partial nint write(int fd, byte[] buf, nuint count);
}

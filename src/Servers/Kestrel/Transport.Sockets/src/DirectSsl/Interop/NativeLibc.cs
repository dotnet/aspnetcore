// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Interop;

internal static partial class NativeLibc
{
    public static int SetSocketOption(int sockfd, ref int optval, uint optlen)
    {
        return NativeLibc.setsockopt(sockfd, IPPROTO_TCP, TCP_DEFER_ACCEPT, ref optval, optlen);
    }

    // P/Invoke definitions
    [LibraryImport("libc", SetLastError = true)]
    private static partial int setsockopt(int sockfd, int level, int optname, ref int optval, uint optlen);

    private const int IPPROTO_TCP = 6;
    private const int TCP_DEFER_ACCEPT = 9;
}

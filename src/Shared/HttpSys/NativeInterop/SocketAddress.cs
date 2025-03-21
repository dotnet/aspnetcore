// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Windows.Win32.Networking.WinSock;

namespace Microsoft.AspNetCore.HttpSys.Internal;

internal abstract class SocketAddress
{
    internal abstract int GetPort();

    internal abstract IPAddress? GetIPAddress();

    internal static unsafe SocketAddress? CopyOutAddress(SOCKADDR* pSockaddr)
    {
        // Per https://learn.microsoft.com/windows/win32/api/ws2def/ns-ws2def-sockaddr,
        // use the SOCKADDR* pointer only to read the address family, then cast the pointer to
        // the appropriate family type and continue processing.
        return pSockaddr->sa_family switch
        {
            ADDRESS_FAMILY.AF_INET => new SocketAddressIPv4(*(SOCKADDR_IN*)pSockaddr),
            ADDRESS_FAMILY.AF_INET6 => new SocketAddressIPv6(*(SOCKADDR_IN6*)pSockaddr),
            _ => null
        };
    }

    private sealed class SocketAddressIPv4 : SocketAddress
    {
        private readonly SOCKADDR_IN _sockaddr;

        internal SocketAddressIPv4(in SOCKADDR_IN sockaddr)
        {
            _sockaddr = sockaddr;
        }

        internal override int GetPort()
        {
            // sin_port is network byte order
            return IPAddress.NetworkToHostOrder((short)_sockaddr.sin_port);
        }

        internal override IPAddress? GetIPAddress()
        {
            // address is network byte order
            return new IPAddress(_sockaddr.sin_addr.S_un.S_addr);
        }
    }

    private sealed class SocketAddressIPv6 : SocketAddress
    {
        private readonly SOCKADDR_IN6 _sockaddr;

        internal SocketAddressIPv6(in SOCKADDR_IN6 sockaddr)
        {
            _sockaddr = sockaddr;
        }

        internal override int GetPort()
        {
            // sin6_port is network byte order
            return IPAddress.NetworkToHostOrder((short)_sockaddr.sin6_port);
        }

        internal override IPAddress? GetIPAddress()
        {
            // address is network byte order
            // when CsWin32 gets support for inline arrays, remove 'AsReadOnlySpan' call below.
            // https://github.com/microsoft/CsWin32/issues/1086
            return new IPAddress(_sockaddr.sin6_addr.u.Byte.AsReadOnlySpan()); // TODO: Does scope id matter?
        }
    }
}

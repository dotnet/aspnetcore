// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
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

    // internal for testing
    internal sealed class SocketAddressIPv4 : SocketAddress
    {
        private readonly SOCKADDR_IN _sockaddr;

        internal SocketAddressIPv4(in SOCKADDR_IN sockaddr)
        {
            _sockaddr = sockaddr;
        }

        internal override int GetPort()
        {
            // _sockaddr.sin_port has network byte order.
            // ---
            // We could use IPAddress.NetworkToHostOrder() here (see https://source.dot.net/#System.Net.Primitives/System/Net/IPAddress.cs,547),
            // but it does not have a support for unsigned-short, resulting in incorrect representation of the result.
            // ---
            // However, BinaryPrimitives.ReverseEndianness() does support unsigned-short (see https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Buffers/Binary/BinaryPrimitives.ReverseEndianness.cs,100)
            // which makes a correct conversion for a TCP port number range (0 - 65535).
            return BitConverter.IsLittleEndian
                ? BinaryPrimitives.ReverseEndianness(_sockaddr.sin_port)
                : _sockaddr.sin_port;
        }

        internal override IPAddress? GetIPAddress()
        {
            // address is network byte order
            return new IPAddress(_sockaddr.sin_addr.S_un.S_addr);
        }
    }

    // internal for testing
    internal sealed class SocketAddressIPv6 : SocketAddress
    {
        private readonly SOCKADDR_IN6 _sockaddr;

        internal SocketAddressIPv6(in SOCKADDR_IN6 sockaddr)
        {
            _sockaddr = sockaddr;
        }

        internal override int GetPort()
        {
            // _sockaddr.sin6_port has network byte order.
            // ---
            // We could use IPAddress.NetworkToHostOrder() here (see https://source.dot.net/#System.Net.Primitives/System/Net/IPAddress.cs,547),
            // but it does not have a support for unsigned-short, resulting in incorrect representation of the result.
            // ---
            // However, BinaryPrimitives.ReverseEndianness() does support unsigned-short (see https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Buffers/Binary/BinaryPrimitives.ReverseEndianness.cs,100)
            // which makes a correct conversion for a TCP port number range (0 - 65535).
            return BitConverter.IsLittleEndian
                ? BinaryPrimitives.ReverseEndianness(_sockaddr.sin6_port)
                : _sockaddr.sin6_port;
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

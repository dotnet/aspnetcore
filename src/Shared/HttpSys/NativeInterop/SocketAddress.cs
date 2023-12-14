// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.AspNetCore.HttpSys.Internal;

internal sealed class SocketAddress
{
    private const int NumberOfIPv6Labels = 8;
    private const int IPv6AddressSize = 28;
    private const int IPv4AddressSize = 16;
    private const int WriteableOffset = 2;

    private readonly byte[] _buffer;
    private readonly int _size;

    private SocketAddress(AddressFamily family, int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, WriteableOffset);
        Family = family;
        _size = size;
        // Sized to match the native structure
        _buffer = new byte[((size / IntPtr.Size) + 2) * IntPtr.Size]; // sizeof DWORD
    }

    internal AddressFamily Family { get; }

    internal int GetPort()
    {
        return (_buffer[2] << 8 & 0xFF00) | (_buffer[3]);
    }

    internal IPAddress? GetIPAddress()
    {
        if (Family == AddressFamily.InterNetworkV6)
        {
            return GetIpv6Address();
        }
        else if (Family == AddressFamily.InterNetwork)
        {
            return GetIPv4Address();
        }
        else
        {
            return null;
        }
    }

    private IPAddress GetIpv6Address()
    {
        Contract.Assert(_size >= IPv6AddressSize);
        var bytes = new byte[NumberOfIPv6Labels * 2];
        Array.Copy(_buffer, 8, bytes, 0, NumberOfIPv6Labels * 2);
        return new IPAddress(bytes); // TODO: Does scope id matter?
    }

    private IPAddress GetIPv4Address()
    {
        Contract.Assert(_size >= IPv4AddressSize);
        return new IPAddress(new byte[] { _buffer[4], _buffer[5], _buffer[6], _buffer[7] });
    }

    internal static unsafe SocketAddress? CopyOutAddress(IntPtr address)
    {
        var addressFamily = *((ushort*)address);
        if (addressFamily == (ushort)AddressFamily.InterNetwork)
        {
            var v4address = new SocketAddress(AddressFamily.InterNetwork, IPv4AddressSize);
            fixed (byte* pBuffer = v4address._buffer)
            {
                for (var index = 2; index < IPv4AddressSize; index++)
                {
                    pBuffer[index] = ((byte*)address)[index];
                }
            }
            return v4address;
        }
        if (addressFamily == (ushort)AddressFamily.InterNetworkV6)
        {
            var v6address = new SocketAddress(AddressFamily.InterNetworkV6, IPv6AddressSize);
            fixed (byte* pBuffer = v6address._buffer)
            {
                for (var index = 2; index < IPv6AddressSize; index++)
                {
                    pBuffer[index] = ((byte*)address)[index];
                }
            }
            return v6address;
        }

        return null;
    }
}

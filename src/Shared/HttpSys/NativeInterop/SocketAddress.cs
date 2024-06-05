// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Microsoft.AspNetCore.HttpSys.Internal;

// a little perf app measured these times when comparing the internal
// buffer implemented as a managed byte[] or unmanaged memory IntPtr
// that's why we use byte[]
// byte[] total ms:19656
// IntPtr total ms:25671

/// <devdoc>
///    <para>
///       This class is used when subclassing EndPoint, and provides indication
///       on how to format the memory buffers that winsock uses for network addresses.
///    </para>
/// </devdoc>
internal sealed class SocketAddress
{
    private const int NumberOfIPv6Labels = 8;
    // Lower case hex, no leading zeros
    private const string IPv6NumberFormat = "{0:x}";
    private const char IPv6StringSeparator = ':';
    private const string IPv4StringFormat = "{0:d}.{1:d}.{2:d}.{3:d}";

    internal const int IPv6AddressSize = 28;
    internal const int IPv4AddressSize = 16;

    private const int WriteableOffset = 2;

    private readonly int _size;
    private readonly byte[] _buffer;
    private int _hash;

    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public SocketAddress(AddressFamily family, int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, WriteableOffset);
        _size = size;
        _buffer = new byte[((size / IntPtr.Size) + 2) * IntPtr.Size]; // sizeof DWORD

#if BIGENDIAN
            m_Buffer[0] = unchecked((byte)((int)family>>8));
            m_Buffer[1] = unchecked((byte)((int)family   ));
#else
        _buffer[0] = unchecked((byte)((int)family));
        _buffer[1] = unchecked((byte)((int)family >> 8));
#endif
    }

    internal byte[] Buffer
    {
        get { return _buffer; }
    }

    internal AddressFamily Family
    {
        get
        {
            int family;
#if BIGENDIAN
                family = ((int)m_Buffer[0]<<8) | m_Buffer[1];
#else
            family = _buffer[0] | ((int)_buffer[1] << 8);
#endif
            return (AddressFamily)family;
        }
    }

    internal int Size
    {
        get
        {
            return _size;
        }
    }

    // access to unmanaged serialized data. this doesn't
    // allow access to the first 2 bytes of unmanaged memory
    // that are supposed to contain the address family which
    // is readonly.
    //
    // <SECREVIEW> you can still use negative offsets as a back door in case
    // winsock changes the way it uses SOCKADDR. maybe we want to prohibit it?
    // maybe we should make the class sealed to avoid potentially dangerous calls
    // into winsock with unproperly formatted data? </SECREVIEW>

    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    private byte this[int offset]
    {
        get
        {
            // access
            if (offset < 0 || offset >= Size)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            return _buffer[offset];
        }
    }

    internal int GetPort()
    {
        return (int)((_buffer[2] << 8 & 0xFF00) | (_buffer[3]));
    }

    public override bool Equals(object? comparand)
    {
        SocketAddress? castedComparand = comparand as SocketAddress;
        if (castedComparand == null || this.Size != castedComparand.Size)
        {
            return false;
        }
        for (int i = 0; i < this.Size; i++)
        {
            if (this[i] != castedComparand[i])
            {
                return false;
            }
        }
        return true;
    }

    public override int GetHashCode()
    {
        if (_hash == 0)
        {
            int i;
            int size = Size & ~3;

            for (i = 0; i < size; i += 4)
            {
                _hash ^= (int)_buffer[i]
                        | ((int)_buffer[i + 1] << 8)
                        | ((int)_buffer[i + 2] << 16)
                        | ((int)_buffer[i + 3] << 24);
            }
            if ((Size & 3) != 0)
            {
                int remnant = 0;
                int shift = 0;

                for (; i < Size; ++i)
                {
                    remnant |= ((int)_buffer[i]) << shift;
                    shift += 8;
                }
                _hash ^= remnant;
            }
        }
        return _hash;
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
        Contract.Assert(Size >= IPv6AddressSize);
        byte[] bytes = new byte[NumberOfIPv6Labels * 2];
        Array.Copy(_buffer, 8, bytes, 0, NumberOfIPv6Labels * 2);
        return new IPAddress(bytes); // TODO: Does scope id matter?
    }

    private IPAddress GetIPv4Address()
    {
        Contract.Assert(Size >= IPv4AddressSize);
        return new IPAddress(new byte[] { _buffer[4], _buffer[5], _buffer[6], _buffer[7] });
    }

    public override string ToString()
    {
        StringBuilder bytes = new StringBuilder();
        for (int i = WriteableOffset; i < this.Size; i++)
        {
            if (i > WriteableOffset)
            {
                bytes.Append(',');
            }
            bytes.Append(this[i].ToString(NumberFormatInfo.InvariantInfo));
        }
        return Family.ToString() + ":" + Size.ToString(NumberFormatInfo.InvariantInfo) + ":{" + bytes.ToString() + "}";
    }

    internal string? GetIPAddressString()
    {
        if (Family == AddressFamily.InterNetworkV6)
        {
            return GetIpv6AddressString();
        }
        else if (Family == AddressFamily.InterNetwork)
        {
            return GetIPv4AddressString();
        }
        else
        {
            return null;
        }
    }

    private string GetIPv4AddressString()
    {
        Contract.Assert(Size >= IPv4AddressSize);

        return string.Format(CultureInfo.InvariantCulture, IPv4StringFormat,
            _buffer[4], _buffer[5], _buffer[6], _buffer[7]);
    }

    // TODO: Does scope ID ever matter?
    private unsafe string GetIpv6AddressString()
    {
        Contract.Assert(Size >= IPv6AddressSize);

        fixed (byte* rawBytes = _buffer)
        {
            // Convert from bytes to shorts.
            ushort* rawShorts = stackalloc ushort[NumberOfIPv6Labels];
            int numbersOffset = 0;
            // The address doesn't start at the beginning of the buffer.
            for (int i = 8; i < ((NumberOfIPv6Labels * 2) + 8); i += 2)
            {
                rawShorts[numbersOffset++] = (ushort)(rawBytes[i] << 8 | rawBytes[i + 1]);
            }
            return GetIPv6AddressString(rawShorts);
        }
    }

    private static unsafe string GetIPv6AddressString(ushort* numbers)
    {
        // RFC 5952 Sections 4 & 5 - Compressed, lower case, with possible embedded IPv4 addresses.

        // Start to finish, inclusive.  <-1, -1> for no compression
        KeyValuePair<int, int> range = FindCompressionRange(numbers);
        bool ipv4Embedded = ShouldHaveIpv4Embedded(numbers);

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < NumberOfIPv6Labels; i++)
        {
            if (ipv4Embedded && i == (NumberOfIPv6Labels - 2))
            {
                // Write the remaining digits as an IPv4 address
                builder.Append(IPv6StringSeparator);
                builder.Append(string.Format(CultureInfo.InvariantCulture, IPv4StringFormat,
                    numbers[i] >> 8, numbers[i] & 0xFF, numbers[i + 1] >> 8, numbers[i + 1] & 0xFF));
                break;
            }

            // Compression; 1::1, ::1, 1::
            if (range.Key == i)
            {
                // Start compression, add :
                builder.Append(IPv6StringSeparator);
            }
            if (range.Key <= i && range.Value == (NumberOfIPv6Labels - 1))
            {
                // Remainder compressed; 1::
                builder.Append(IPv6StringSeparator);
                break;
            }
            if (range.Key <= i && i <= range.Value)
            {
                continue; // Compressed
            }

            if (i != 0)
            {
                builder.Append(IPv6StringSeparator);
            }
            builder.Append(string.Format(CultureInfo.InvariantCulture, IPv6NumberFormat, numbers[i]));
        }

        return builder.ToString();
    }

    // RFC 5952 Section 4.2.3
    // Longest consecutive sequence of zero segments, minimum 2.
    // On equal, first sequence wins.
    // <-1, -1> for no compression.
    private static unsafe KeyValuePair<int, int> FindCompressionRange(ushort* numbers)
    {
        int longestSequenceLength = 0;
        int longestSequenceStart = -1;

        int currentSequenceLength = 0;
        for (int i = 0; i < NumberOfIPv6Labels; i++)
        {
            if (numbers[i] == 0)
            {
                // In a sequence
                currentSequenceLength++;
                if (currentSequenceLength > longestSequenceLength)
                {
                    longestSequenceLength = currentSequenceLength;
                    longestSequenceStart = i - currentSequenceLength + 1;
                }
            }
            else
            {
                currentSequenceLength = 0;
            }
        }

        if (longestSequenceLength >= 2)
        {
            return new KeyValuePair<int, int>(longestSequenceStart,
                longestSequenceStart + longestSequenceLength - 1);
        }

        return new KeyValuePair<int, int>(-1, -1); // No compression
    }

    // Returns true if the IPv6 address should be formated with an embedded IPv4 address:
    // ::192.168.1.1
    private static unsafe bool ShouldHaveIpv4Embedded(ushort* numbers)
    {
        // 0:0 : 0:0 : x:x : x.x.x.x
        if (numbers[0] == 0 && numbers[1] == 0 && numbers[2] == 0 && numbers[3] == 0 && numbers[6] != 0)
        {
            // RFC 5952 Section 5 - 0:0 : 0:0 : 0:[0 | FFFF] : x.x.x.x
            if (numbers[4] == 0 && (numbers[5] == 0 || numbers[5] == 0xFFFF))
            {
                return true;

                // SIIT - 0:0 : 0:0 : FFFF:0 : x.x.x.x
            }
            else if (numbers[4] == 0xFFFF && numbers[5] == 0)
            {
                return true;
            }
        }
        // ISATAP
        if (numbers[4] == 0 && numbers[5] == 0x5EFE)
        {
            return true;
        }

        return false;
    }
} // class SocketAddress

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class SocketNativeMethods
    {
        internal const ushort AF_INET = 2;
        internal const ushort AF_INET6 = 23;
        internal const ushort AF_UNSPEC = 0;

        internal static SOCKADDR_INET Convert(IPEndPoint endpoint)
        {
            var lResult = ConvertToSocketAddrInet(endpoint.Address);
            SetPort(endpoint.Address.AddressFamily, ref lResult, endpoint.Port);
            return lResult;
        }

        private static void SetPort(AddressFamily addressFamily, ref SOCKADDR_INET socketAddrInet, int originalPort)
        {
            if (0 > originalPort || originalPort > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(originalPort), originalPort, "invalid port");
            }
            var convertedPort = (ushort)IPAddress.HostToNetworkOrder((short)originalPort);
            switch (addressFamily)
            {
                case AddressFamily.InterNetwork:
                    socketAddrInet.Ipv4.sin_port = convertedPort;
                    break;
                case AddressFamily.InterNetworkV6:
                    socketAddrInet.Ipv6.sin6_port = convertedPort;
                    break;
                default:
                    socketAddrInet.Ipv6.sin6_port = convertedPort;
                    break;
            }
        }

        internal static SOCKADDR_INET ConvertToSocketAddrInet(IPAddress ipAddress)
        {
            var lResult = new SOCKADDR_INET();
            switch (ipAddress.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    lResult.Ipv4.sin_addr0 = ipAddress.GetAddressBytes()[0];
                    lResult.Ipv4.sin_addr1 = ipAddress.GetAddressBytes()[1];
                    lResult.Ipv4.sin_addr2 = ipAddress.GetAddressBytes()[2];
                    lResult.Ipv4.sin_addr3 = ipAddress.GetAddressBytes()[3];
                    lResult.Ipv4.sin_family = AF_UNSPEC;
                    break;
                case AddressFamily.InterNetworkV6:
                    lResult.Ipv6.sin6_addr0 = ipAddress.GetAddressBytes()[0];
                    lResult.Ipv6.sin6_addr1 = ipAddress.GetAddressBytes()[1];
                    lResult.Ipv6.sin6_addr2 = ipAddress.GetAddressBytes()[2];
                    lResult.Ipv6.sin6_addr3 = ipAddress.GetAddressBytes()[3];
                    lResult.Ipv6.sin6_addr4 = ipAddress.GetAddressBytes()[4];
                    lResult.Ipv6.sin6_addr5 = ipAddress.GetAddressBytes()[5];
                    lResult.Ipv6.sin6_addr6 = ipAddress.GetAddressBytes()[6];
                    lResult.Ipv6.sin6_addr7 = ipAddress.GetAddressBytes()[7];
                    lResult.Ipv6.sin6_addr8 = ipAddress.GetAddressBytes()[8];
                    lResult.Ipv6.sin6_addr9 = ipAddress.GetAddressBytes()[9];
                    lResult.Ipv6.sin6_addr10 = ipAddress.GetAddressBytes()[10];
                    lResult.Ipv6.sin6_addr11 = ipAddress.GetAddressBytes()[11];
                    lResult.Ipv6.sin6_addr12 = ipAddress.GetAddressBytes()[12];
                    lResult.Ipv6.sin6_addr13 = ipAddress.GetAddressBytes()[13];
                    lResult.Ipv6.sin6_addr14 = ipAddress.GetAddressBytes()[14];
                    lResult.Ipv6.sin6_addr15 = ipAddress.GetAddressBytes()[15];
                    lResult.Ipv6.sin6_family = AF_UNSPEC;
                    break;
                default:
                    throw new ArgumentException("Only IPv4 or IPv6 are supported");
            }
            return lResult;
        }

        internal static IPAddress ConvertToIPAddress(SOCKADDR_INET addr)
        {
            switch (addr.si_family)
            {
                case AF_INET:
                    {
                        var result = new byte[4];
                        result[0] = addr.Ipv4.sin_addr0;
                        result[1] = addr.Ipv4.sin_addr1;
                        result[2] = addr.Ipv4.sin_addr2;
                        result[3] = addr.Ipv4.sin_addr3;
                        return new IPAddress(result);
                    }
                case AF_INET6:
                    {
                        var result = new byte[16];
                        result[0] = addr.Ipv6.sin6_addr0;
                        result[1] = addr.Ipv6.sin6_addr1;
                        result[2] = addr.Ipv6.sin6_addr2;
                        result[3] = addr.Ipv6.sin6_addr3;
                        result[4] = addr.Ipv6.sin6_addr4;
                        result[5] = addr.Ipv6.sin6_addr5;
                        result[6] = addr.Ipv6.sin6_addr6;
                        result[7] = addr.Ipv6.sin6_addr7;
                        result[8] = addr.Ipv6.sin6_addr8;
                        result[9] = addr.Ipv6.sin6_addr9;
                        result[10] = addr.Ipv6.sin6_addr10;
                        result[11] = addr.Ipv6.sin6_addr11;
                        result[12] = addr.Ipv6.sin6_addr12;
                        result[13] = addr.Ipv6.sin6_addr13;
                        result[14] = addr.Ipv6.sin6_addr14;
                        result[15] = addr.Ipv6.sin6_addr15;
                        return new IPAddress(result);
                    }
                default:
                    throw new ArgumentException("Address family not supported");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SOCKADDR_IN
        {
            internal ushort sin_family;
            internal ushort sin_port;
            internal byte sin_addr0;
            internal byte sin_addr1;
            internal byte sin_addr2;
            internal byte sin_addr3;

            internal byte[] Address
            {
                get
                {
                    return new byte[] { sin_addr0, sin_addr1, sin_addr2, sin_addr3 };
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SOCKADDR_IN6
        {
            internal ushort sin6_family;
            internal ushort sin6_port;
            internal uint sin6_flowinfo;
            internal byte sin6_addr0;
            internal byte sin6_addr1;
            internal byte sin6_addr2;
            internal byte sin6_addr3;
            internal byte sin6_addr4;
            internal byte sin6_addr5;
            internal byte sin6_addr6;
            internal byte sin6_addr7;
            internal byte sin6_addr8;
            internal byte sin6_addr9;
            internal byte sin6_addr10;
            internal byte sin6_addr11;
            internal byte sin6_addr12;
            internal byte sin6_addr13;
            internal byte sin6_addr14;
            internal byte sin6_addr15;
            internal uint sin6_scope_id;

            internal byte[] Address
            {
                get
                {
                    return new byte[] {
                    sin6_addr0, sin6_addr1, sin6_addr2, sin6_addr3 ,
                    sin6_addr4, sin6_addr5, sin6_addr6, sin6_addr7 ,
                    sin6_addr8, sin6_addr9, sin6_addr10, sin6_addr11 ,
                    sin6_addr12, sin6_addr13, sin6_addr14, sin6_addr15 };
                }
            }
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
        internal struct SOCKADDR_INET
        {
            [FieldOffset(0)]
            internal SOCKADDR_IN Ipv4;
            [FieldOffset(0)]
            internal SOCKADDR_IN6 Ipv6;
            [FieldOffset(0)]
            internal ushort si_family;
        }
    }
}

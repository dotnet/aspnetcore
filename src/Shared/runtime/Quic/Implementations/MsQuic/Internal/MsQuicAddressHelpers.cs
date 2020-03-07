// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class MsQuicAddressHelpers
    {
        internal const ushort IPv4 = 2;
        internal const ushort IPv6 = 23;

        internal static unsafe IPEndPoint INetToIPEndPoint(SOCKADDR_INET inetAddress)
        {
            if (inetAddress.si_family == IPv4)
            {
                return new IPEndPoint(new IPAddress(inetAddress.Ipv4.Address), (ushort)IPAddress.NetworkToHostOrder((short)inetAddress.Ipv4.sin_port));
            }
            else
            {
                return new IPEndPoint(new IPAddress(inetAddress.Ipv6.Address), (ushort)IPAddress.NetworkToHostOrder((short)inetAddress.Ipv6._port));
            }
        }

        internal static SOCKADDR_INET IPEndPointToINet(IPEndPoint endpoint)
        {
            SOCKADDR_INET socketAddress = default;
            byte[] buffer = endpoint.Address.GetAddressBytes();
            if (endpoint.Address != IPAddress.Any && endpoint.Address != IPAddress.IPv6Any)
            {
                switch (endpoint.Address.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        socketAddress.Ipv4.sin_addr0 = buffer[0];
                        socketAddress.Ipv4.sin_addr1 = buffer[1];
                        socketAddress.Ipv4.sin_addr2 = buffer[2];
                        socketAddress.Ipv4.sin_addr3 = buffer[3];
                        socketAddress.Ipv4.sin_family = IPv4;
                        break;
                    case AddressFamily.InterNetworkV6:
                        socketAddress.Ipv6._addr0 = buffer[0];
                        socketAddress.Ipv6._addr1 = buffer[1];
                        socketAddress.Ipv6._addr2 = buffer[2];
                        socketAddress.Ipv6._addr3 = buffer[3];
                        socketAddress.Ipv6._addr4 = buffer[4];
                        socketAddress.Ipv6._addr5 = buffer[5];
                        socketAddress.Ipv6._addr6 = buffer[6];
                        socketAddress.Ipv6._addr7 = buffer[7];
                        socketAddress.Ipv6._addr8 = buffer[8];
                        socketAddress.Ipv6._addr9 = buffer[9];
                        socketAddress.Ipv6._addr10 = buffer[10];
                        socketAddress.Ipv6._addr11 = buffer[11];
                        socketAddress.Ipv6._addr12 = buffer[12];
                        socketAddress.Ipv6._addr13 = buffer[13];
                        socketAddress.Ipv6._addr14 = buffer[14];
                        socketAddress.Ipv6._addr15 = buffer[15];
                        socketAddress.Ipv6._family = IPv6;
                        break;
                    default:
                        throw new ArgumentException("Only IPv4 or IPv6 are supported");
                }
            }

            SetPort(endpoint.Address.AddressFamily, ref socketAddress, endpoint.Port);
            return socketAddress;
        }

        private static void SetPort(AddressFamily addressFamily, ref SOCKADDR_INET socketAddrInet, int originalPort)
        {
            ushort convertedPort = (ushort)IPAddress.HostToNetworkOrder((short)originalPort);
            switch (addressFamily)
            {
                case AddressFamily.InterNetwork:
                    socketAddrInet.Ipv4.sin_port = convertedPort;
                    break;
                case AddressFamily.InterNetworkV6:
                default:
                    socketAddrInet.Ipv6._port = convertedPort;
                    break;
            }
        }
    }
}

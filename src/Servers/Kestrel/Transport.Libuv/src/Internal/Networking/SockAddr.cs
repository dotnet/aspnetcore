// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SockAddr
    {
        // this type represents native memory occupied by sockaddr struct
        // https://msdn.microsoft.com/en-us/library/windows/desktop/ms740496(v=vs.85).aspx
        // although the c/c++ header defines it as a 2-byte short followed by a 14-byte array,
        // the simplest way to reserve the same size in c# is with four nameless long values
        private long _field0;
        private long _field1;
        private long _field2;
        private long _field3;

        public SockAddr(long ignored)
        {
            _field0 = _field1 = _field2 = _field3 = 0;
        }

        public unsafe IPEndPoint GetIPEndPoint()
        {
            // The bytes are represented in network byte order.
            //
            // Example 1: [2001:4898:e0:391:b9ef:1124:9d3e:a354]:39179
            //
            // 0000 0000 0b99 0017  => The third and fourth bytes 990B is the actual port
            // 9103 e000 9848 0120  => IPv6 address is represented in the 128bit field1 and field2.
            // 54a3 3e9d 2411 efb9     Read these two 64-bit long from right to left byte by byte.
            // 0000 0000 0000 0010  => Scope ID 0x10 (eg [::1%16]) the first 4 bytes of field3 in host byte order.
            //
            // Example 2: 10.135.34.141:39178 when adopt dual-stack sockets, IPv4 is mapped to IPv6
            //
            // 0000 0000 0a99 0017  => The port representation are the same
            // 0000 0000 0000 0000
            // 8d22 870a ffff 0000  => IPv4 occupies the last 32 bit: 0A.87.22.8d is the actual address.
            // 0000 0000 0000 0000
            //
            // Example 3: 10.135.34.141:12804, not dual-stack sockets
            //
            // 8d22 870a fd31 0002  => sa_family == AF_INET (02)
            // 0000 0000 0000 0000
            // 0000 0000 0000 0000
            // 0000 0000 0000 0000
            //
            // Example 4: 127.0.0.1:52798, on a Mac OS
            //
            // 0100 007F 3ECE 0210  => sa_family == AF_INET (02) Note that struct sockaddr on mac use
            // 0000 0000 0000 0000     the second unint8 field for sa family type
            // 0000 0000 0000 0000     http://www.opensource.apple.com/source/xnu/xnu-1456.1.26/bsd/sys/socket.h
            // 0000 0000 0000 0000
            //
            // Reference:
            //  - Windows: https://msdn.microsoft.com/en-us/library/windows/desktop/ms740506(v=vs.85).aspx
            //  - Linux: https://github.com/torvalds/linux/blob/6a13feb9c82803e2b815eca72fa7a9f5561d7861/include/linux/socket.h
            //  - Linux (sin6_scope_id): https://github.com/torvalds/linux/blob/5924bbecd0267d87c24110cbe2041b5075173a25/net/sunrpc/addr.c#L82
            //  - Apple: http://www.opensource.apple.com/source/xnu/xnu-1456.1.26/bsd/sys/socket.h

            // Quick calculate the port by mask the field and locate the byte 3 and byte 4
            // and then shift them to correct place to form a int.
            var port = ((int)(_field0 & 0x00FF0000) >> 8) | (int)((_field0 & 0xFF000000) >> 24);

            int family = (int)_field0;
            if (PlatformApis.IsDarwin)
            {
                // see explanation in example 4
                family = family >> 8;
            }
            family = family & 0xFF;

            if (family == 2)
            {
                // AF_INET => IPv4
                return new IPEndPoint(new IPAddress((_field0 >> 32) & 0xFFFFFFFF), port);
            }
            else if (IsIPv4MappedToIPv6())
            {
                var ipv4bits = (_field2 >> 32) & 0x00000000FFFFFFFF;
                return new IPEndPoint(new IPAddress(ipv4bits), port);
            }
            else
            {
                // otherwise IPv6
                var bytes = new byte[16];
                fixed (byte* b = bytes)
                {
                    *((long*)b) = _field1;
                    *((long*)(b + 8)) = _field2;
                }

                return new IPEndPoint(new IPAddress(bytes, ScopeId), port);
            }
        }

        public uint ScopeId
        {
            get
            {
                return (uint)_field3;
            }
            set
            {
                _field3 &= unchecked ((long)0xFFFFFFFF00000000);
                _field3 |= value;
            }
        }

        private bool IsIPv4MappedToIPv6()
        {
            // If the IPAddress is an IPv4 mapped to IPv6, return the IPv4 representation instead.
            // For example [::FFFF:127.0.0.1] will be transform to IPAddress of 127.0.0.1
            if (_field1 != 0)
            {
                return false;
            }

            return (_field2 & 0xFFFFFFFF) == 0xFFFF0000;
        }
    }
}

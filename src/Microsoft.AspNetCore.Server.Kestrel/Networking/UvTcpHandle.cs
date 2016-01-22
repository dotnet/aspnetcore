// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Networking
{
    public class UvTcpHandle : UvStreamHandle
    {
        public UvTcpHandle(IKestrelTrace logger) : base(logger)
        {
        }

        public void Init(UvLoopHandle loop)
        {
            CreateMemory(
                loop.Libuv,
                loop.ThreadId,
                loop.Libuv.handle_size(Libuv.HandleType.TCP));

            _uv.tcp_init(loop, this);
        }

        public void Init(UvLoopHandle loop, Action<Action<IntPtr>, IntPtr> queueCloseHandle)
        {
            CreateHandle(
                loop.Libuv,
                loop.ThreadId,
                loop.Libuv.handle_size(Libuv.HandleType.TCP), queueCloseHandle);

            _uv.tcp_init(loop, this);
        }

        public void Bind(ServerAddress address)
        {
            var endpoint = CreateIPEndpoint(address);

            SockAddr addr;
            var addressText = endpoint.Address.ToString();

            Exception error1;
            _uv.ip4_addr(addressText, endpoint.Port, out addr, out error1);

            if (error1 != null)
            {
                Exception error2;
                _uv.ip6_addr(addressText, endpoint.Port, out addr, out error2);
                if (error2 != null)
                {
                    throw error1;
                }
            }

            _uv.tcp_bind(this, ref addr, 0);
        }

        public IPEndPoint GetPeerIPEndPoint()
        {
            SockAddr socketAddress;
            int namelen = Marshal.SizeOf<SockAddr>();
            _uv.tcp_getpeername(this, out socketAddress, ref namelen);

            return socketAddress.GetIPEndPoint();
        }

        public IPEndPoint GetSockIPEndPoint()
        {
            SockAddr socketAddress;
            int namelen = Marshal.SizeOf<SockAddr>();
            _uv.tcp_getsockname(this, out socketAddress, ref namelen);

            return socketAddress.GetIPEndPoint();
        }

        public void Open(IntPtr hSocket)
        {
            _uv.tcp_open(this, hSocket);
        }

        public void NoDelay(bool enable)
        {
            _uv.tcp_nodelay(this, enable);
        }

        /// <summary>
        /// Returns an <see cref="IPEndPoint"/> for the given host an port.
        /// If the host parameter isn't "localhost" or an IP address, use IPAddress.Any.
        /// </summary>
        public static IPEndPoint CreateIPEndpoint(ServerAddress address)
        {
            // TODO: IPv6 support
            IPAddress ip;

            if (!IPAddress.TryParse(address.Host, out ip))
            {
                if (string.Equals(address.Host, "localhost", StringComparison.OrdinalIgnoreCase))
                {
                    ip = IPAddress.Loopback;
                }
                else
                {
                    ip = IPAddress.IPv6Any;
                }
            }

            return new IPEndPoint(ip, address.Port);
        }
    }
}

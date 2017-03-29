// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Networking
{
    public class UvTcpHandle : UvStreamHandle
    {
        public UvTcpHandle(IKestrelTrace logger) : base(logger)
        {
        }

        public void Init(UvLoopHandle loop, Action<Action<IntPtr>, IntPtr> queueCloseHandle)
        {
            CreateHandle(
                loop.Libuv,
                loop.ThreadId,
                loop.Libuv.handle_size(LibuvFunctions.HandleType.TCP), queueCloseHandle);

            _uv.tcp_init(loop, this);
        }

        public void Open(IntPtr fileDescriptor)
        {
            _uv.tcp_open(this, fileDescriptor);
        }

        public void Bind(IPEndPoint endPoint)
        {
            SockAddr addr;
            var addressText = endPoint.Address.ToString();

            Exception error1;
            _uv.ip4_addr(addressText, endPoint.Port, out addr, out error1);

            if (error1 != null)
            {
                Exception error2;
                _uv.ip6_addr(addressText, endPoint.Port, out addr, out error2);
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

        public void NoDelay(bool enable)
        {
            _uv.tcp_nodelay(this, enable);
        }
    }
}

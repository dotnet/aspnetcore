// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking
{
    public class UvTcpHandle : UvStreamHandle
    {
        public UvTcpHandle(ILibuvTrace logger) : base(logger)
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
            var addressText = endPoint.Address.ToString();

            _uv.ip4_addr(addressText, endPoint.Port, out var addr, out var error1);

            if (error1 != null)
            {
                _uv.ip6_addr(addressText, endPoint.Port, out addr, out var error2);
                if (error2 != null)
                {
                    throw error1;
                }

                if (endPoint.Address.ScopeId != addr.ScopeId)
                {
                    // IPAddress.ScopeId cannot be less than 0 or greater than 0xFFFFFFFF
                    // https://msdn.microsoft.com/en-us/library/system.net.ipaddress.scopeid(v=vs.110).aspx
                    addr.ScopeId = (uint)endPoint.Address.ScopeId;
                }
            }

            _uv.tcp_bind(this, ref addr, 0);
        }

        public IPEndPoint GetPeerIPEndPoint()
        {
            int namelen = Marshal.SizeOf<SockAddr>();
            _uv.tcp_getpeername(this, out var socketAddress, ref namelen);

            return socketAddress.GetIPEndPoint();
        }

        public IPEndPoint GetSockIPEndPoint()
        {
            int namelen = Marshal.SizeOf<SockAddr>();
            _uv.tcp_getsockname(this, out var socketAddress, ref namelen);

            return socketAddress.GetIPEndPoint();
        }

        public void NoDelay(bool enable)
        {
            _uv.tcp_nodelay(this, enable);
        }
    }
}

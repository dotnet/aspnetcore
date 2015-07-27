// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public class UvTcpHandle : UvStreamHandle
    {
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

        public void Bind(IPEndPoint endpoint)
        {
            Libuv.sockaddr addr;
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

        public void Open(IntPtr hSocket)
        {
            _uv.tcp_open(this, hSocket);
        }
    }
}

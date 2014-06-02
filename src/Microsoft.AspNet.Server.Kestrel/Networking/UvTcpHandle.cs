// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public class UvTcpHandle : UvStreamHandle
    {
        public void Init(UvLoopHandle loop)
        {
            CreateHandle(loop, 256);
            _uv.tcp_init(loop, this);
        }

        public void Bind(IPEndPoint endpoint)
        {
            Libuv.sockaddr addr;
            _uv.ip4_addr(endpoint.Address.ToString(), endpoint.Port, out addr);
            _uv.tcp_bind(this, ref addr, 0);
        }
    }
}

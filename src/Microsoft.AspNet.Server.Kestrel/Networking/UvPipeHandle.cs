// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public class UvPipeHandle : UvStreamHandle
    {
        public void Init(UvLoopHandle loop, bool ipc)
        {
            CreateMemory(
                loop.Libuv,
                loop.ThreadId, 
                loop.Libuv.handle_size(Libuv.HandleType.NAMED_PIPE));

            _uv.pipe_init(loop, this, ipc);
        }

        public void Init(UvLoopHandle loop, Action<Action<IntPtr>, IntPtr> queueCloseHandle)
        {
            CreateHandle(
                loop.Libuv, 
                loop.ThreadId,
                loop.Libuv.handle_size(Libuv.HandleType.TCP), queueCloseHandle);

            _uv.pipe_init(loop, this, false);
        }

        public void Bind(string name)
        {
            _uv.pipe_bind(this, name);
        }

        //public void Open(IntPtr hSocket)
        //{
        //}
    }
}

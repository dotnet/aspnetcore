// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Networking
{
    public class UvPipeHandle : UvStreamHandle
    {
        public UvPipeHandle(IKestrelTrace logger) : base(logger)
        {
        }

        public void Init(UvLoopHandle loop, Action<Action<IntPtr>, IntPtr> queueCloseHandle, bool ipc = false)
        {
            CreateHandle(
                loop.Libuv, 
                loop.ThreadId,
                loop.Libuv.handle_size(Libuv.HandleType.NAMED_PIPE), queueCloseHandle);

            _uv.pipe_init(loop, this, ipc);
        }

        public void Bind(string name)
        {
            _uv.pipe_bind(this, name);
        }

        public int PendingCount()
        {
            return _uv.pipe_pending_count(this);
        }
    }
}

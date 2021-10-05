// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking
{
    internal class UvPipeHandle : UvStreamHandle
    {
        public UvPipeHandle(ILogger logger) : base(logger)
        {
        }

        public void Init(UvLoopHandle loop, Action<Action<IntPtr>, IntPtr> queueCloseHandle, bool ipc = false)
        {
            CreateHandle(
                loop.Libuv,
                loop.ThreadId,
                loop.Libuv.handle_size(LibuvFunctions.HandleType.NAMED_PIPE), queueCloseHandle);

            _uv.pipe_init(loop, this, ipc);
        }

        public void Open(IntPtr fileDescriptor)
        {
            _uv.pipe_open(this, fileDescriptor);
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

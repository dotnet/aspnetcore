// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Networking
{
    public class UvLoopHandle : UvMemory
    {
        public UvLoopHandle(IKestrelTrace logger) : base(logger)
        {
        }

        public void Init(Libuv uv)
        {
            CreateMemory(
                uv,
                Thread.CurrentThread.ManagedThreadId,
                uv.loop_size());

            _uv.loop_init(this);
        }

        public int Run(int mode = 0)
        {
            return _uv.run(this, mode);
        }

        public void Stop()
        {
            _uv.stop(this);
        }

        unsafe protected override bool ReleaseHandle()
        {
            var memory = handle;
            if (memory != IntPtr.Zero)
            {
                // loop_close clears the gcHandlePtr
                var gcHandlePtr = *(IntPtr*)memory;

                _uv.loop_close(this);
                handle = IntPtr.Zero;

                DestroyMemory(memory, gcHandlePtr);
            }

            return true;
        }
    }
}

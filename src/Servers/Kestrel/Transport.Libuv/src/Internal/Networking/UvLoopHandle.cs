// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking
{
    internal class UvLoopHandle : UvMemory
    {
        public UvLoopHandle(LibuvTrace logger) : base(logger)
        {
        }

        public void Init(LibuvFunctions uv)
        {
            CreateMemory(
                uv,
                Environment.CurrentManagedThreadId,
                uv.loop_size());

            _uv.loop_init(this);
        }

        public void Run(int mode = 0)
        {
            _uv.run(this, mode);
        }

        public void Stop()
        {
            _uv.stop(this);
        }

        public long Now()
        {
            return _uv.now(this);
        }

        protected override unsafe bool ReleaseHandle()
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

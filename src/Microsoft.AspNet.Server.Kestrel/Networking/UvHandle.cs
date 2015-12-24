// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public abstract class UvHandle : UvMemory
    {
        private static readonly Libuv.uv_close_cb _destroyMemory = (handle) => DestroyMemory(handle);
        private Action<Action<IntPtr>, IntPtr> _queueCloseHandle;

        protected UvHandle(IKestrelTrace logger) : base (logger)
        {
        }

        unsafe protected void CreateHandle(
            Libuv uv,
            int threadId,
            int size,
            Action<Action<IntPtr>, IntPtr> queueCloseHandle)
        {
            _queueCloseHandle = queueCloseHandle;
            CreateMemory(uv, threadId, size);
        }

        protected override bool ReleaseHandle()
        {
            var memory = handle;
            if (memory != IntPtr.Zero)
            {
                handle = IntPtr.Zero;

                if (Thread.CurrentThread.ManagedThreadId == ThreadId)
                {
                    _uv.close(memory, _destroyMemory);
                }
                else if (_queueCloseHandle != null)
                {
                    // This can be called from the finalizer.
                    // Ensure the closure doesn't reference "this".
                    var uv = _uv;
                    _queueCloseHandle(memory2 => uv.close(memory2, _destroyMemory), memory);
                }
            }
            return true;
        }

        public void Reference()
        {
            _uv.@ref(this);
        }

        public void Unreference()
        {
            _uv.unref(this);
        }
    }
}

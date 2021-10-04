// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking
{
    internal class UvRequest : UvMemory
    {
        protected UvRequest(LibuvTrace logger) : base(logger, GCHandleType.Normal)
        {
        }

        public virtual void Init(LibuvThread thread)
        {
#if DEBUG
            // Store weak handles to all UvRequest objects so we can do leak detection
            // while running tests
            thread.Requests.Add(new WeakReference(this));
#endif
        }

        protected override bool ReleaseHandle()
        {
            DestroyMemory(handle);
            handle = IntPtr.Zero;
            return true;
        }
    }
}


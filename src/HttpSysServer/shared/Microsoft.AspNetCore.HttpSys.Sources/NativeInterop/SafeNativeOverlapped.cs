// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.AspNetCore.HttpSys.Internal
{
    internal class SafeNativeOverlapped : SafeHandle
    {
        internal static readonly SafeNativeOverlapped Zero = new SafeNativeOverlapped();
        private ThreadPoolBoundHandle _boundHandle;

        internal SafeNativeOverlapped()
            : base(IntPtr.Zero, true)
        {
        }

        internal unsafe SafeNativeOverlapped(ThreadPoolBoundHandle boundHandle, NativeOverlapped* handle)
            : base(IntPtr.Zero, true)
        {
            SetHandle((IntPtr)handle);
            _boundHandle = boundHandle;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        public void ReinitializeNativeOverlapped()
        {
            IntPtr handleSnapshot = handle;

            if (handleSnapshot != IntPtr.Zero)
            {
                unsafe
                {
                    ((NativeOverlapped*)handleSnapshot)->InternalHigh = IntPtr.Zero;
                    ((NativeOverlapped*)handleSnapshot)->InternalLow = IntPtr.Zero;
                    ((NativeOverlapped*)handleSnapshot)->EventHandle = IntPtr.Zero;
                }
            }
        }

        protected override bool ReleaseHandle()
        {
            IntPtr oldHandle = Interlocked.Exchange(ref handle, IntPtr.Zero);
            // Do not call free durring AppDomain shutdown, there may be an outstanding operation.
            // Overlapped will take care calling free when the native callback completes.
            if (oldHandle != IntPtr.Zero && !NclUtilities.HasShutdownStarted)
            {
                unsafe
                {
                    _boundHandle.FreeNativeOverlapped((NativeOverlapped*)oldHandle);
                }
            }
            return true;
        }
    }
}

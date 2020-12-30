// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

// Remove once HttpSys has enabled nullable
#nullable enable

namespace Microsoft.AspNetCore.HttpSys.Internal
{
    internal class SafeNativeOverlapped : SafeHandle
    {
        internal static readonly SafeNativeOverlapped Zero = new SafeNativeOverlapped();
        private ThreadPoolBoundHandle? _boundHandle;

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

        protected override bool ReleaseHandle()
        {
            Debug.Assert(_boundHandle != null, "ReleaseHandle can't be called on SafeNativeOverlapped.Zero.");

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

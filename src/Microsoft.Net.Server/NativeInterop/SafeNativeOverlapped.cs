// -----------------------------------------------------------------------
// <copyright file="SafeNativeOverlapped.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Net.Server
{
    internal class SafeNativeOverlapped : SafeHandle
    {
        internal static readonly SafeNativeOverlapped Zero = new SafeNativeOverlapped();

        internal SafeNativeOverlapped()
            : this(IntPtr.Zero)
        {
        }

        internal unsafe SafeNativeOverlapped(NativeOverlapped* handle)
            : this((IntPtr)handle)
        {
        }

        internal SafeNativeOverlapped(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
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
                    Overlapped.Free((NativeOverlapped*)oldHandle);
                }
            }
            return true;
        }
    }
}

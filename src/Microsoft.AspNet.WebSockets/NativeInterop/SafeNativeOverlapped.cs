// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

//------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.AspNet.WebSockets
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
            if (oldHandle != IntPtr.Zero && !HasShutdownStarted)
            {
                unsafe
                {
                    Overlapped.Free((NativeOverlapped*)oldHandle);
                }
            }
            return true;
        }

        internal static bool HasShutdownStarted
        {
            get
            {
                return Environment.HasShutdownStarted
#if NET45
                    || AppDomain.CurrentDomain.IsFinalizingForUnload()
#endif
                    ;
            }
        }
    }
}

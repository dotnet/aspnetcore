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

// -----------------------------------------------------------------------
// <copyright file="SafeCloseHandle.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNet.Security.Windows
{
    [SuppressUnmanagedCodeSecurity]
    internal sealed class SafeCloseHandle : CriticalHandleZeroOrMinusOneIsInvalid
    {
        private int _disposed;

        private SafeCloseHandle()
            : base()
        {
        }

        internal IntPtr DangerousGetHandle()
        {
            return handle;
        }

        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
            {
                if (Interlocked.Increment(ref _disposed) == 1)
                {
                    return UnsafeNclNativeMethods.SafeNetHandles.CloseHandle(handle);
                }
            }
            return true;
        }

        // This method will bypass refCount check done by VM
        // Means it will force handle release if has a valid value
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal void Abort()
        {
            ReleaseHandle();
            SetHandleAsInvalid();
        }
    }
}

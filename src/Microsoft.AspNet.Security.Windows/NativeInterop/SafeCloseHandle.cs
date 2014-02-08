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

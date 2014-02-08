// -----------------------------------------------------------------------
// <copyright file="SafeLocalFree.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNet.Security.Windows
{
    [SuppressUnmanagedCodeSecurity]
    internal sealed class SafeLocalFree : SafeHandleZeroOrMinusOneIsInvalid
    {
        private const int LmemFixed = 0;
        private const int NULL = 0;

        // This returned handle cannot be modified by the application.
        public static SafeLocalFree Zero = new SafeLocalFree(false);

        private SafeLocalFree()
            : base(true)
        {
        }

        private SafeLocalFree(bool ownsHandle)
            : base(ownsHandle)
        {
        }

        public static SafeLocalFree LocalAlloc(int cb)
        {
            SafeLocalFree result = UnsafeNclNativeMethods.SafeNetHandles.LocalAlloc(LmemFixed, (UIntPtr)cb);
            if (result.IsInvalid)
            {
                result.SetHandleAsInvalid();
                throw new OutOfMemoryException();
            }
            return result;
        }

        protected override bool ReleaseHandle()
        {
            return UnsafeNclNativeMethods.SafeNetHandles.LocalFree(handle) == IntPtr.Zero;
        }
    }
}

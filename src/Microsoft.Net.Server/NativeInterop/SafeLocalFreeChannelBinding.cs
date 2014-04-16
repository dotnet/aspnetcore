// -----------------------------------------------------------------------
// <copyright file="SafeLocalFreeChannelBinding.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Security.Authentication.ExtendedProtection;

namespace Microsoft.Net.Server
{
    internal class SafeLocalFreeChannelBinding : ChannelBinding
    {
        private const int LMEM_FIXED = 0;
        private int size;

        public override int Size
        {
            get { return size; }
        }

        public static SafeLocalFreeChannelBinding LocalAlloc(int cb)
        {
            SafeLocalFreeChannelBinding result;

            result = UnsafeNclNativeMethods.SafeNetHandles.LocalAllocChannelBinding(LMEM_FIXED, (UIntPtr)cb);
            if (result.IsInvalid)
            {
                result.SetHandleAsInvalid();
                throw new OutOfMemoryException();
            }

            result.size = cb;
            return result;
        }

        protected override bool ReleaseHandle()
        {
            return UnsafeNclNativeMethods.SafeNetHandles.LocalFree(handle) == IntPtr.Zero;
        }
    }
}

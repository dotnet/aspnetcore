// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Authentication.ExtendedProtection;

// Remove once HttpSys has enabled nullable
#nullable enable

namespace Microsoft.AspNetCore.HttpSys.Internal
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

        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero || handle.ToInt32() == -1;
            }
        }
    }
}

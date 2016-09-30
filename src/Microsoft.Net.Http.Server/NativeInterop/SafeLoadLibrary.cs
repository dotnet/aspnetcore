// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Win32.SafeHandles;

namespace Microsoft.Net.Http.Server
{
    internal sealed class SafeLoadLibrary : SafeHandleZeroOrMinusOneIsInvalid
    {
        private const string KERNEL32 = "kernel32.dll";

        public static readonly SafeLoadLibrary Zero = new SafeLoadLibrary(false);

        private SafeLoadLibrary()
            : base(true)
        {
        }

        private SafeLoadLibrary(bool ownsHandle)
            : base(ownsHandle)
        {
        }

        public static unsafe SafeLoadLibrary LoadLibraryEx(string library)
        {
            SafeLoadLibrary result = UnsafeNclNativeMethods.SafeNetHandles.LoadLibraryExW(library, null, 0);
            if (result.IsInvalid)
            {
                result.SetHandleAsInvalid();
            }
            return result;
        }

        protected override bool ReleaseHandle()
        {
            return UnsafeNclNativeMethods.SafeNetHandles.FreeLibrary(handle);
        }
    }
}

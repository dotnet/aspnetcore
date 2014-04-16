// -----------------------------------------------------------------------
// <copyright file="SafeLoadLibrary.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Win32.SafeHandles;

namespace Microsoft.Net.Server
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

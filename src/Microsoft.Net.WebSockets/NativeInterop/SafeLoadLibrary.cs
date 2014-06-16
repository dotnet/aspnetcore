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

using Microsoft.Win32.SafeHandles;

namespace Microsoft.Net.WebSockets
{
    internal sealed class SafeLoadLibrary : SafeHandleZeroOrMinusOneIsInvalid
    {
        private const string KERNEL32 = "kernel32.dll";

        public static readonly SafeLoadLibrary Zero = new SafeLoadLibrary(false);

        private SafeLoadLibrary() : base(true)
        {
        }

        private SafeLoadLibrary(bool ownsHandle) : base(ownsHandle)
        {
        }

        public static unsafe SafeLoadLibrary LoadLibraryEx(string library)
        {
            SafeLoadLibrary result = UnsafeNativeMethods.SafeNetHandles.LoadLibraryExW(library, null, 0);
            if (result.IsInvalid)
            {
                result.SetHandleAsInvalid();
            }
            return result;
        }

        protected override bool ReleaseHandle()
        {
            return UnsafeNativeMethods.SafeNetHandles.FreeLibrary(handle);
        }
    }
}

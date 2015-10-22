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

// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==

#if DOTNET5_4

namespace Microsoft.Win32.SafeHandles
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;

    // Class of critical handle which uses 0 or -1 as an invalid handle.
    [System.Security.SecurityCritical]  // auto-generated_required
    internal abstract class CriticalHandleZeroOrMinusOneIsInvalid : CriticalHandle
    {
        protected CriticalHandleZeroOrMinusOneIsInvalid()
            : base(IntPtr.Zero)
        {
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get { return handle == new IntPtr(0) || handle == new IntPtr(-1); }
        }
    }
}

#endif

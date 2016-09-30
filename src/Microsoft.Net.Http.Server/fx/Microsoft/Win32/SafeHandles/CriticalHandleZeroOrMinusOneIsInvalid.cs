// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETSTANDARD1_3

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

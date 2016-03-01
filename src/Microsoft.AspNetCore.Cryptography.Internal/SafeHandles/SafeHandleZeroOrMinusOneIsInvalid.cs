// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

#if NETSTANDARD1_3
namespace Microsoft.Win32.SafeHandles
{
    internal abstract class SafeHandleZeroOrMinusOneIsInvalid : SafeHandle
    {
        // Called by P/Invoke when returning SafeHandles
        protected SafeHandleZeroOrMinusOneIsInvalid(bool ownsHandle)
            : base(IntPtr.Zero, ownsHandle)
        {
        }

        public override bool IsInvalid
        {
            get
            {
                return (handle == IntPtr.Zero || handle == (IntPtr)(-1));
            }
        }
    }
}
#endif

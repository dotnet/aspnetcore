// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Win32.SafeHandles;

// Remove once HttpSys has enabled nullable
#nullable enable

namespace Microsoft.AspNetCore.HttpSys.Internal
{
    internal sealed class SafeLocalMemHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeLocalMemHandle()
            : base(true)
        {
        }

        internal SafeLocalMemHandle(IntPtr existingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(existingHandle);
        }

        protected override bool ReleaseHandle()
        {
            return UnsafeNclNativeMethods.SafeNetHandles.LocalFree(handle) == IntPtr.Zero;
        }
    }
}

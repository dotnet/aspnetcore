// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Win32.SafeHandles;

// Remove once HttpSys has enabled nullable
#nullable enable

namespace Microsoft.AspNetCore.HttpSys.Internal
{
    internal sealed class HeapAllocHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private static readonly IntPtr ProcessHeap = UnsafeNclNativeMethods.GetProcessHeap();

        // Called by P/Invoke when returning SafeHandles
        private HeapAllocHandle()
            : base(ownsHandle: true)
        {
        }

        // Do not provide a finalizer - SafeHandle's critical finalizer will call ReleaseHandle for you.
        protected override bool ReleaseHandle()
        {
            return UnsafeNclNativeMethods.HeapFree(ProcessHeap, 0, handle);
        }
    }
}

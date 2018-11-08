// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNetCore.Cryptography.SafeHandles
{
    /// <summary>
    /// Represents a handle returned by LocalAlloc.
    /// </summary>
    internal class LocalAllocHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Called by P/Invoke when returning SafeHandles
        protected LocalAllocHandle()
            : base(ownsHandle: true) { }

        // Do not provide a finalizer - SafeHandle's critical finalizer will call ReleaseHandle for you.
        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle); // actually calls LocalFree
            return true;
        }
    }
}

// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNet.Security.DataProtection
{
    internal sealed class BCryptAlgorithmHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Called by P/Invoke when returning SafeHandles
        private BCryptAlgorithmHandle()
            : base(ownsHandle: true)
        {
        }

        // Do not provide a finalizer - SafeHandle's critical finalizer will call ReleaseHandle for you.
        protected override bool ReleaseHandle()
        {
            return (UnsafeNativeMethods.BCryptCloseAlgorithmProvider(handle, dwFlags: 0) == 0);
        }
    }
}

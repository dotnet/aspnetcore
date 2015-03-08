// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

#if DNXCORE50
namespace Microsoft.AspNet.Cryptography.SafeHandles
{
    /// <summary>
    /// Represents a managed view over an NCRYPT_KEY_HANDLE.
    /// </summary>
    internal class SafeNCryptKeyHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Called by P/Invoke when returning SafeHandles
        protected SafeNCryptKeyHandle()
            : base(ownsHandle: true) { }

        // Do not provide a finalizer - SafeHandle's critical finalizer will call ReleaseHandle for you.
        protected override bool ReleaseHandle()
        {
            // TODO: Replace me with a real implementation on CoreClr.
            throw new NotImplementedException();
        }
    }
}
#endif

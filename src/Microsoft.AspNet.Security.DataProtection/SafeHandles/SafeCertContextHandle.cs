// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNet.Security.DataProtection.SafeHandles
{
    internal sealed class SafeCertContextHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeCertContextHandle()
            : base(ownsHandle: true)
        {
        }

        public static SafeCertContextHandle CreateDuplicateFrom(IntPtr existingHandle)
        {
            SafeCertContextHandle newHandle = UnsafeNativeMethods.CertDuplicateCertificateContext(existingHandle);
            CryptoUtil.AssertSafeHandleIsValid(newHandle);
            return newHandle;
        }

        // Do not provide a finalizer - SafeHandle's critical finalizer will call ReleaseHandle for you.
        protected override bool ReleaseHandle()
        {
            return UnsafeNativeMethods.CertFreeCertificateContext(handle);
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNetCore.Cryptography.SafeHandles
{
    internal unsafe sealed class NCryptDescriptorHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private NCryptDescriptorHandle()
            : base(ownsHandle: true)
        {
        }

        public string GetProtectionDescriptorRuleString()
        {
            // from ncryptprotect.h
            const int NCRYPT_PROTECTION_INFO_TYPE_DESCRIPTOR_STRING = 0x00000001;

            LocalAllocHandle ruleStringHandle;
            int ntstatus = UnsafeNativeMethods.NCryptGetProtectionDescriptorInfo(
                hDescriptor: this,
                pMemPara: IntPtr.Zero,
                dwInfoType: NCRYPT_PROTECTION_INFO_TYPE_DESCRIPTOR_STRING,
                ppvInfo: out ruleStringHandle);
            UnsafeNativeMethods.ThrowExceptionForNCryptStatus(ntstatus);
            CryptoUtil.AssertSafeHandleIsValid(ruleStringHandle);

            using (ruleStringHandle)
            {
                return new String((char*)ruleStringHandle.DangerousGetHandle());
            }
        }

        // Do not provide a finalizer - SafeHandle's critical finalizer will call ReleaseHandle for you.
        protected override bool ReleaseHandle()
        {
            return (UnsafeNativeMethods.NCryptCloseProtectionDescriptor(handle) == 0);
        }
    }
}

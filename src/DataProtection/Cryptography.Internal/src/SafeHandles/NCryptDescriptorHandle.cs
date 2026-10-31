// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNetCore.Cryptography.SafeHandles;

internal sealed unsafe class NCryptDescriptorHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public NCryptDescriptorHandle()
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

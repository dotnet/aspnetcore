// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNetCore.Cryptography.SafeHandles;

internal abstract unsafe class BCryptHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    protected BCryptHandle()
        : base(ownsHandle: true)
    {
    }

    protected uint GetProperty(string pszProperty, void* pbOutput, uint cbOutput)
    {
        uint retVal;
        int ntstatus = UnsafeNativeMethods.BCryptGetProperty(this, pszProperty, pbOutput, cbOutput, out retVal, dwFlags: 0);
        UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);
        return retVal;
    }

    protected void SetProperty(string pszProperty, void* pbInput, uint cbInput)
    {
        int ntstatus = UnsafeNativeMethods.BCryptSetProperty(this, pszProperty, pbInput, cbInput, dwFlags: 0);
        UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);
    }
}

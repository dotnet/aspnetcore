// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Cryptography.SafeHandles;

internal sealed unsafe class BCryptHashHandle : BCryptHandle
{
    private BCryptAlgorithmHandle? _algProviderHandle;

    // Called by P/Invoke when returning SafeHandles
    public BCryptHashHandle() { }

    /// <summary>
    /// Duplicates this hash handle, including any existing hashed state.
    /// </summary>
    public BCryptHashHandle DuplicateHash()
    {
        BCryptHashHandle duplicateHandle;
        int ntstatus = UnsafeNativeMethods.BCryptDuplicateHash(this, out duplicateHandle, IntPtr.Zero, 0, 0);
        UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);
        CryptoUtil.AssertSafeHandleIsValid(duplicateHandle);

        duplicateHandle._algProviderHandle = this._algProviderHandle;
        return duplicateHandle;
    }

    /// <summary>
    /// Calculates the cryptographic hash over a set of input data.
    /// </summary>
    public void HashData(byte* pbInput, uint cbInput, byte* pbHashDigest, uint cbHashDigest)
    {
        int ntstatus;
        if (cbInput > 0)
        {
            ntstatus = UnsafeNativeMethods.BCryptHashData(
                hHash: this,
                pbInput: pbInput,
                cbInput: cbInput,
                dwFlags: 0);
            UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);
        }

        ntstatus = UnsafeNativeMethods.BCryptFinishHash(
            hHash: this,
            pbOutput: pbHashDigest,
            cbOutput: cbHashDigest,
            dwFlags: 0);
        UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);
    }

    // Do not provide a finalizer - SafeHandle's critical finalizer will call ReleaseHandle for you.
    protected override bool ReleaseHandle()
    {
        return (UnsafeNativeMethods.BCryptDestroyHash(handle) == 0);
    }

    // We don't actually need to hold a reference to the algorithm handle, as the native CNG library
    // already holds the reference for us. But once we create a hash from an algorithm provider, odds
    // are good that we'll create another hash from the same algorithm provider at some point in the
    // future. And since algorithm providers are expensive to create, we'll hold a strong reference
    // to all known in-use providers. This way the cached algorithm provider handles utility class
    // doesn't keep creating providers over and over.
    internal void SetAlgorithmProviderHandle(BCryptAlgorithmHandle algProviderHandle)
    {
        _algProviderHandle = algProviderHandle;
    }
}

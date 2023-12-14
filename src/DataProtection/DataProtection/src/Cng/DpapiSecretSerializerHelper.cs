// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.Cryptography.SafeHandles;

namespace Microsoft.AspNetCore.DataProtection.Cng;

internal static unsafe class DpapiSecretSerializerHelper
{
    // from ncrypt.h
    private const uint NCRYPT_SILENT_FLAG = 0x00000040;

    // from dpapi.h
    private const uint CRYPTPROTECT_UI_FORBIDDEN = 0x1;
    private const uint CRYPTPROTECT_LOCAL_MACHINE = 0x4;

    private static readonly byte[] _purpose = Encoding.UTF8.GetBytes("DPAPI-Protected Secret");

    // Probes to see if protecting to the current Windows user account is available.
    // In theory this should never fail if the user profile is available, so it's more a defense-in-depth check.
    public static bool CanProtectToCurrentUserAccount()
    {
        try
        {
            Guid dummy;
            using var secret = new Secret((byte*)&dummy, sizeof(Guid));
            ProtectWithDpapi(secret, protectToLocalMachine: false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static byte[] ProtectWithDpapi(ISecret secret, bool protectToLocalMachine = false)
    {
        Debug.Assert(secret != null);

        var plaintextSecret = new byte[secret.Length];
        fixed (byte* pbPlaintextSecret = plaintextSecret)
        {
            try
            {
                secret.WriteSecretIntoBuffer(new ArraySegment<byte>(plaintextSecret));
                fixed (byte* pbPurpose = _purpose)
                {
                    return ProtectWithDpapiCore(pbPlaintextSecret, (uint)plaintextSecret.Length, pbPurpose, (uint)_purpose.Length, fLocalMachine: protectToLocalMachine);
                }
            }
            finally
            {
                // To limit exposure to the GC.
                Array.Clear(plaintextSecret, 0, plaintextSecret.Length);
            }
        }
    }

    internal static byte[] ProtectWithDpapiCore(byte* pbSecret, uint cbSecret, byte* pbOptionalEntropy, uint cbOptionalEntropy, bool fLocalMachine = false)
    {
        byte dummy; // provides a valid memory address if the secret or entropy has zero length

        var dataIn = new DATA_BLOB()
        {
            cbData = cbSecret,
            pbData = (pbSecret != null) ? pbSecret : &dummy
        };
        var entropy = new DATA_BLOB()
        {
            cbData = cbOptionalEntropy,
            pbData = (pbOptionalEntropy != null) ? pbOptionalEntropy : &dummy
        };
        var dataOut = default(DATA_BLOB);

#if NETSTANDARD2_0
        RuntimeHelpers.PrepareConstrainedRegions();
#endif

        try
        {
            var success = UnsafeNativeMethods.CryptProtectData(
                pDataIn: &dataIn,
                szDataDescr: IntPtr.Zero,
                pOptionalEntropy: &entropy,
                pvReserved: IntPtr.Zero,
                pPromptStruct: IntPtr.Zero,
                dwFlags: CRYPTPROTECT_UI_FORBIDDEN | ((fLocalMachine) ? CRYPTPROTECT_LOCAL_MACHINE : 0),
                pDataOut: &dataOut);
            if (!success)
            {
                var errorCode = Marshal.GetLastWin32Error();
                throw new CryptographicException(errorCode);
            }

            var dataLength = checked((int)dataOut.cbData);
            var retVal = new byte[dataLength];
            Marshal.Copy((IntPtr)dataOut.pbData, retVal, 0, dataLength);
            return retVal;
        }
        finally
        {
            // Free memory so that we don't leak.
            // FreeHGlobal actually calls LocalFree.
            if (dataOut.pbData != null)
            {
                Marshal.FreeHGlobal((IntPtr)dataOut.pbData);
            }
        }
    }

    public static byte[] ProtectWithDpapiNG(ISecret secret, NCryptDescriptorHandle protectionDescriptorHandle)
    {
        Debug.Assert(secret != null);
        Debug.Assert(protectionDescriptorHandle != null);

        var plaintextSecret = new byte[secret.Length];
        fixed (byte* pbPlaintextSecret = plaintextSecret)
        {
            try
            {
                secret.WriteSecretIntoBuffer(new ArraySegment<byte>(plaintextSecret));

                byte dummy; // used to provide a valid memory address if secret is zero-length
                return ProtectWithDpapiNGCore(
                    protectionDescriptorHandle: protectionDescriptorHandle,
                    pbData: (pbPlaintextSecret != null) ? pbPlaintextSecret : &dummy,
                    cbData: (uint)plaintextSecret.Length);
            }
            finally
            {
                // Limits secret exposure to garbage collector.
                Array.Clear(plaintextSecret, 0, plaintextSecret.Length);
            }
        }
    }

    private static byte[] ProtectWithDpapiNGCore(NCryptDescriptorHandle protectionDescriptorHandle, byte* pbData, uint cbData)
    {
        Debug.Assert(protectionDescriptorHandle != null);
        Debug.Assert(pbData != null);

        // Perform the encryption operation, putting the protected data into LocalAlloc-allocated memory.
        LocalAllocHandle protectedData;
        uint cbProtectedData;
        var ntstatus = UnsafeNativeMethods.NCryptProtectSecret(
            hDescriptor: protectionDescriptorHandle,
            dwFlags: NCRYPT_SILENT_FLAG,
            pbData: pbData,
            cbData: cbData,
            pMemPara: IntPtr.Zero,
            hWnd: IntPtr.Zero,
            ppbProtectedBlob: out protectedData,
            pcbProtectedBlob: out cbProtectedData);
        UnsafeNativeMethods.ThrowExceptionForNCryptStatus(ntstatus);
        CryptoUtil.AssertSafeHandleIsValid(protectedData);

        // Copy the data from LocalAlloc-allocated memory into a managed memory buffer.
        using (protectedData)
        {
            var retVal = new byte[cbProtectedData];
            if (cbProtectedData > 0)
            {
                fixed (byte* pbRetVal = retVal)
                {
                    var handleAcquired = false;

#if NETSTANDARD2_0
                    RuntimeHelpers.PrepareConstrainedRegions();
#endif

                    try
                    {
                        protectedData.DangerousAddRef(ref handleAcquired);
                        UnsafeBufferUtil.BlockCopy(from: (void*)protectedData.DangerousGetHandle(), to: pbRetVal, byteCount: cbProtectedData);
                    }
                    finally
                    {
                        if (handleAcquired)
                        {
                            protectedData.DangerousRelease();
                        }
                    }
                }
            }
            return retVal;
        }
    }

    public static Secret UnprotectWithDpapi(byte[] protectedSecret)
    {
        Debug.Assert(protectedSecret != null);

        fixed (byte* pbProtectedSecret = protectedSecret)
        {
            fixed (byte* pbPurpose = _purpose)
            {
                return UnprotectWithDpapiCore(pbProtectedSecret, (uint)protectedSecret.Length, pbPurpose, (uint)_purpose.Length);
            }
        }
    }

    internal static Secret UnprotectWithDpapiCore(byte* pbProtectedData, uint cbProtectedData, byte* pbOptionalEntropy, uint cbOptionalEntropy)
    {
        byte dummy; // provides a valid memory address if the secret or entropy has zero length

        var dataIn = new DATA_BLOB()
        {
            cbData = cbProtectedData,
            pbData = (pbProtectedData != null) ? pbProtectedData : &dummy
        };
        var entropy = new DATA_BLOB()
        {
            cbData = cbOptionalEntropy,
            pbData = (pbOptionalEntropy != null) ? pbOptionalEntropy : &dummy
        };
        var dataOut = default(DATA_BLOB);

#if NETSTANDARD2_0
        RuntimeHelpers.PrepareConstrainedRegions();
#endif

        try
        {
            var success = UnsafeNativeMethods.CryptUnprotectData(
                pDataIn: &dataIn,
                ppszDataDescr: IntPtr.Zero,
                pOptionalEntropy: &entropy,
                pvReserved: IntPtr.Zero,
                pPromptStruct: IntPtr.Zero,
                dwFlags: CRYPTPROTECT_UI_FORBIDDEN,
                pDataOut: &dataOut);
            if (!success)
            {
                var errorCode = Marshal.GetLastWin32Error();
                throw new CryptographicException(errorCode);
            }

            return new Secret(dataOut.pbData, checked((int)dataOut.cbData));
        }
        finally
        {
            // Zero and free memory so that we don't leak secrets.
            // FreeHGlobal actually calls LocalFree.
            if (dataOut.pbData != null)
            {
                UnsafeBufferUtil.SecureZeroMemory(dataOut.pbData, dataOut.cbData);
                Marshal.FreeHGlobal((IntPtr)dataOut.pbData);
            }
        }
    }

    public static Secret UnprotectWithDpapiNG(byte[] protectedData)
    {
        Debug.Assert(protectedData != null);

        fixed (byte* pbProtectedData = protectedData)
        {
            byte dummy; // used to provide a valid memory address if protected data is zero-length
            return UnprotectWithDpapiNGCore(
                pbData: (pbProtectedData != null) ? pbProtectedData : &dummy,
                cbData: (uint)protectedData.Length);
        }
    }

    private static Secret UnprotectWithDpapiNGCore(byte* pbData, uint cbData)
    {
        Debug.Assert(pbData != null);

        // First, decrypt the payload into LocalAlloc-allocated memory.
        LocalAllocHandle unencryptedPayloadHandle;
        uint cbUnencryptedPayload;
        var ntstatus = UnsafeNativeMethods.NCryptUnprotectSecret(
            phDescriptor: IntPtr.Zero,
            dwFlags: NCRYPT_SILENT_FLAG,
            pbProtectedBlob: pbData,
            cbProtectedBlob: cbData,
            pMemPara: IntPtr.Zero,
            hWnd: IntPtr.Zero,
            ppbData: out unencryptedPayloadHandle,
            pcbData: out cbUnencryptedPayload);
        UnsafeNativeMethods.ThrowExceptionForNCryptStatus(ntstatus);
        CryptoUtil.AssertSafeHandleIsValid(unencryptedPayloadHandle);

        // Copy the data from LocalAlloc-allocated memory into a CryptProtectMemory-protected buffer.
        // There's a small window between NCryptUnprotectSecret returning and the call to PrepareConstrainedRegions
        // below where the AppDomain could rudely unload. This won't leak memory (due to the SafeHandle), but it
        // will cause the secret not to be zeroed out before the memory is freed. We won't worry about this since
        // the window is extremely small and AppDomain unloads should not happen here in practice.
        using (unencryptedPayloadHandle)
        {
            var handleAcquired = false;

#if NETSTANDARD2_0
            RuntimeHelpers.PrepareConstrainedRegions();
#endif

            try
            {
                unencryptedPayloadHandle.DangerousAddRef(ref handleAcquired);
                return new Secret((byte*)unencryptedPayloadHandle.DangerousGetHandle(), checked((int)cbUnencryptedPayload));
            }
            finally
            {
                if (handleAcquired)
                {
                    UnsafeBufferUtil.SecureZeroMemory((byte*)unencryptedPayloadHandle.DangerousGetHandle(), cbUnencryptedPayload);
                    unencryptedPayloadHandle.DangerousRelease();
                }
            }
        }
    }

    public static string GetRuleFromDpapiNGProtectedPayload(byte[] protectedData)
    {
        Debug.Assert(protectedData != null);

        fixed (byte* pbProtectedData = protectedData)
        {
            byte dummy; // used to provide a valid memory address if protected data is zero-length
            return GetRuleFromDpapiNGProtectedPayloadCore(
                pbData: (pbProtectedData != null) ? pbProtectedData : &dummy,
                cbData: (uint)protectedData.Length);
        }
    }

    private static string GetRuleFromDpapiNGProtectedPayloadCore(byte* pbData, uint cbData)
    {
        // from ncryptprotect.h
        const uint NCRYPT_UNPROTECT_NO_DECRYPT = 0x00000001;

        NCryptDescriptorHandle descriptorHandle;
        LocalAllocHandle unprotectedDataHandle;
        var ntstatus = UnsafeNativeMethods.NCryptUnprotectSecret(
            phDescriptor: out descriptorHandle,
            dwFlags: NCRYPT_UNPROTECT_NO_DECRYPT,
            pbProtectedBlob: pbData,
            cbProtectedBlob: cbData,
            pMemPara: IntPtr.Zero,
            hWnd: IntPtr.Zero,
            ppbData: out unprotectedDataHandle,
            pcbData: out _);
        UnsafeNativeMethods.ThrowExceptionForNCryptStatus(ntstatus);
        CryptoUtil.AssertSafeHandleIsValid(descriptorHandle);

        if (unprotectedDataHandle != null && !unprotectedDataHandle.IsInvalid)
        {
            // we don't care about this value
            unprotectedDataHandle.Dispose();
        }

        using (descriptorHandle)
        {
            return descriptorHandle.GetProtectionDescriptorRuleString();
        }
    }
}

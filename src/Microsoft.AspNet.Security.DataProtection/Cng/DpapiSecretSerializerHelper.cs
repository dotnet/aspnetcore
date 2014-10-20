// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNet.Security.DataProtection.SafeHandles;

namespace Microsoft.AspNet.Security.DataProtection.Cng
{
    internal unsafe static class DpapiSecretSerializerHelper
    {
        // from ncrypt.h
        private const uint NCRYPT_SILENT_FLAG = 0x00000040;

        // from dpapi.h
        private const uint CRYPTPROTECT_UI_FORBIDDEN = 0x1;
        private const uint CRYPTPROTECT_LOCAL_MACHINE = 0x4;

        private static readonly byte[] _purpose = Encoding.UTF8.GetBytes("DPAPI-Protected Secret");

        public static byte[] ProtectWithDpapi(ISecret secret, bool protectToLocalMachine = false)
        {
            Debug.Assert(secret != null);

            byte[] plaintextSecret = new byte[secret.Length];
            fixed (byte* pbPlaintextSecret = plaintextSecret)
            {
                try
                {
                    secret.WriteSecretIntoBuffer(new ArraySegment<byte>(plaintextSecret));
                    fixed (byte* pbPurpose = _purpose)
                    {
                        return ProtectWithDpapiImpl(pbPlaintextSecret, (uint)plaintextSecret.Length, pbPurpose, (uint)_purpose.Length, fLocalMachine: protectToLocalMachine);
                    }
                }
                finally
                {
                    // To limit exposure to the GC.
                    Array.Clear(plaintextSecret, 0, plaintextSecret.Length);
                }
            }
        }

        internal static byte[] ProtectWithDpapiImpl(byte* pbSecret, uint cbSecret, byte* pbOptionalEntropy, uint cbOptionalEntropy, bool fLocalMachine = false)
        {
            byte dummy; // provides a valid memory address if the secret or entropy has zero length

            DATA_BLOB dataIn = new DATA_BLOB()
            {
                cbData = cbSecret,
                pbData = (pbSecret != null) ? pbSecret : &dummy
            };
            DATA_BLOB entropy = new DATA_BLOB()
            {
                cbData = cbOptionalEntropy,
                pbData = (pbOptionalEntropy != null) ? pbOptionalEntropy : &dummy
            };
            DATA_BLOB dataOut = default(DATA_BLOB);

#if !ASPNETCORE50
            RuntimeHelpers.PrepareConstrainedRegions();
#endif
            try
            {
                bool success = UnsafeNativeMethods.CryptProtectData(
                    pDataIn: &dataIn,
                    szDataDescr: IntPtr.Zero,
                    pOptionalEntropy: &entropy,
                    pvReserved: IntPtr.Zero,
                    pPromptStruct: IntPtr.Zero,
                    dwFlags: CRYPTPROTECT_UI_FORBIDDEN | ((fLocalMachine) ? CRYPTPROTECT_LOCAL_MACHINE : 0),
                    pDataOut: out dataOut);
                if (!success)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new CryptographicException(errorCode);
                }

                int dataLength = checked((int)dataOut.cbData);
                byte[] retVal = new byte[dataLength];
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

            byte[] plaintextSecret = new byte[secret.Length];
            fixed (byte* pbPlaintextSecret = plaintextSecret)
            {
                try
                {
                    secret.WriteSecretIntoBuffer(new ArraySegment<byte>(plaintextSecret));

                    byte dummy; // used to provide a valid memory address if secret is zero-length
                    return ProtectWithDpapiNGImpl(
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

        private static byte[] ProtectWithDpapiNGImpl(NCryptDescriptorHandle protectionDescriptorHandle, byte* pbData, uint cbData)
        {
            Debug.Assert(protectionDescriptorHandle != null);
            Debug.Assert(pbData != null);

            // Perform the encryption operation, putting the protected data into LocalAlloc-allocated memory.
            LocalAllocHandle protectedData;
            uint cbProtectedData;
            int ntstatus = UnsafeNativeMethods.NCryptProtectSecret(
                hDescriptor: protectionDescriptorHandle,
                dwFlags: NCRYPT_SILENT_FLAG,
                pbData: pbData,
                cbData: cbData,
                pMemPara: IntPtr.Zero,
                hWnd: IntPtr.Zero,
                ppbProtectedBlob: out protectedData,
                pcbProtectedBlob: out cbProtectedData);
            UnsafeNativeMethods.ThrowExceptionForNCryptStatus(ntstatus);
            CryptoUtil.Assert(protectedData != null && !protectedData.IsInvalid, "protectedData != null && !protectedData.IsInvalid");

            // Copy the data from LocalAlloc-allocated memory into a managed memory buffer.
            using (protectedData)
            {
                byte[] retVal = new byte[cbProtectedData];
                if (cbProtectedData > 0)
                {
                    fixed (byte* pbRetVal = retVal)
                    {
                        bool handleAcquired = false;
#if !ASPNETCORE50
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

        public static ProtectedMemoryBlob UnprotectWithDpapi(byte[] protectedSecret)
        {
            Debug.Assert(protectedSecret != null);

            fixed (byte* pbProtectedSecret = protectedSecret)
            {
                fixed (byte* pbPurpose = _purpose)
                {
                    return UnprotectWithDpapiImpl(pbProtectedSecret, (uint)protectedSecret.Length, pbPurpose, (uint)_purpose.Length);
                }
            }
        }

        internal static ProtectedMemoryBlob UnprotectWithDpapiImpl(byte* pbProtectedData, uint cbProtectedData, byte* pbOptionalEntropy, uint cbOptionalEntropy)
        {
            byte dummy; // provides a valid memory address if the secret or entropy has zero length

            DATA_BLOB dataIn = new DATA_BLOB()
            {
                cbData = cbProtectedData,
                pbData = (pbProtectedData != null) ? pbProtectedData : &dummy
            };
            DATA_BLOB entropy = new DATA_BLOB()
            {
                cbData = cbOptionalEntropy,
                pbData = (pbOptionalEntropy != null) ? pbOptionalEntropy : &dummy
            };
            DATA_BLOB dataOut = default(DATA_BLOB);

#if !ASPNETCORE50
            RuntimeHelpers.PrepareConstrainedRegions();
#endif
            try
            {
                bool success = UnsafeNativeMethods.CryptUnprotectData(
                    pDataIn: &dataIn,
                    ppszDataDescr: IntPtr.Zero,
                    pOptionalEntropy: &entropy,
                    pvReserved: IntPtr.Zero,
                    pPromptStruct: IntPtr.Zero,
                    dwFlags: CRYPTPROTECT_UI_FORBIDDEN,
                    pDataOut: out dataOut);
                if (!success)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new CryptographicException(errorCode);
                }

                return new ProtectedMemoryBlob(dataOut.pbData, checked((int)dataOut.cbData));
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

        public static ProtectedMemoryBlob UnprotectWithDpapiNG(byte[] protectedData)
        {
            Debug.Assert(protectedData != null);

            fixed (byte* pbProtectedData = protectedData)
            {
                byte dummy; // used to provide a valid memory address if protected data is zero-length
                return UnprotectWithDpapiNGImpl(
                    pbData: (pbProtectedData != null) ? pbProtectedData : &dummy,
                    cbData: (uint)protectedData.Length);
            }
        }

        private static ProtectedMemoryBlob UnprotectWithDpapiNGImpl(byte* pbData, uint cbData)
        {
            Debug.Assert(pbData != null);

            // First, decrypt the payload into LocalAlloc-allocated memory.
            LocalAllocHandle unencryptedPayloadHandle;
            uint cbUnencryptedPayload;
            int ntstatus = UnsafeNativeMethods.NCryptUnprotectSecret(
                phDescriptor: IntPtr.Zero,
                dwFlags: NCRYPT_SILENT_FLAG,
                pbProtectedBlob: pbData,
                cbProtectedBlob: cbData,
                pMemPara: IntPtr.Zero,
                hWnd: IntPtr.Zero,
                ppbData: out unencryptedPayloadHandle,
                pcbData: out cbUnencryptedPayload);
            UnsafeNativeMethods.ThrowExceptionForNCryptStatus(ntstatus);
            CryptoUtil.Assert(unencryptedPayloadHandle != null && !unencryptedPayloadHandle.IsInvalid, "unencryptedPayloadHandle != null && !unencryptedPayloadHandle.IsInvalid");

            // Copy the data from LocalAlloc-allocated memory into a CryptProtectMemory-protected buffer.
            // There's a small window between NCryptUnprotectSecret returning and the call to PrepareConstrainedRegions
            // below where the AppDomain could rudely unload. This won't leak memory (due to the SafeHandle), but it
            // will cause the secret not to be zeroed out before the memory is freed. We won't worry about this since
            // the window is extremely small and AppDomain unloads should not happen here in practice.
            using (unencryptedPayloadHandle)
            {
                bool handleAcquired = false;
#if !ASPNETCORE50
                RuntimeHelpers.PrepareConstrainedRegions();
#endif
                try
                {
                    unencryptedPayloadHandle.DangerousAddRef(ref handleAcquired);
                    return new ProtectedMemoryBlob((byte*)unencryptedPayloadHandle.DangerousGetHandle(), checked((int)cbUnencryptedPayload));
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
    }
}

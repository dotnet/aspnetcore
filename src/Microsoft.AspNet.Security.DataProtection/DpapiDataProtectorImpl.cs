// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.AspNet.Security.DataProtection.Util;

namespace Microsoft.AspNet.Security.DataProtection
{
    internal unsafe sealed class DpapiDataProtectorImpl : IDataProtector
    {
        // from dpapi.h
        private const uint CRYPTPROTECT_LOCAL_MACHINE = 0x4;
        private const uint CRYPTPROTECT_UI_FORBIDDEN = 0x1;

        // Used as the 'purposes' parameter to DPAPI operations
        private readonly byte[] _entropy;

        private readonly bool _protectToLocalMachine;

        public DpapiDataProtectorImpl(byte[] entropy, bool protectToLocalMachine)
        {
            Debug.Assert(entropy != null);
            _entropy = entropy;
            _protectToLocalMachine = protectToLocalMachine;
        }

        private static CryptographicException CreateGenericCryptographicException(bool isErrorDueToProfileNotLoaded = false)
        {
            string message = (isErrorDueToProfileNotLoaded) ? Res.DpapiDataProtectorImpl_ProfileNotLoaded : Res.DataProtectorImpl_BadEncryptedData;
            return new CryptographicException(message);
        }

        public IDataProtector CreateSubProtector(string purpose)
        {
            return new DpapiDataProtectorImpl(BCryptUtil.GenerateDpapiSubkey(_entropy, purpose), _protectToLocalMachine);
        }

        public void Dispose()
        {
            // no-op; no unmanaged resources to dispose
        }

        private uint GetCryptProtectUnprotectFlags()
        {
            if (_protectToLocalMachine)
            {
                return CRYPTPROTECT_LOCAL_MACHINE | CRYPTPROTECT_UI_FORBIDDEN;
            }
            else
            {
                return CRYPTPROTECT_UI_FORBIDDEN;
            }
        }

        public byte[] Protect(byte[] unprotectedData)
        {
            if (unprotectedData == null)
            {
                throw new ArgumentNullException("unprotectedData");
            }

            DATA_BLOB dataOut = default(DATA_BLOB);

#if NET45
            RuntimeHelpers.PrepareConstrainedRegions();
#endif
            try
            {
                bool success;
                fixed (byte* pUnprotectedData = unprotectedData.AsFixed())
                {
                    fixed (byte* pEntropy = _entropy)
                    {
                        // no need for checked arithmetic here
                        DATA_BLOB dataIn = new DATA_BLOB() { cbData = (uint)unprotectedData.Length, pbData = pUnprotectedData };
                        DATA_BLOB optionalEntropy = new DATA_BLOB() { cbData = (uint)_entropy.Length, pbData = pEntropy };
                        success = UnsafeNativeMethods.CryptProtectData(&dataIn, IntPtr.Zero, &optionalEntropy, IntPtr.Zero, IntPtr.Zero, GetCryptProtectUnprotectFlags(), out dataOut);
                    }
                }

                // Did a failure occur?
                if (!success)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    bool isErrorDueToProfileNotLoaded = ((errorCode & 0xffff) == 2 /* ERROR_FILE_NOT_FOUND */);
                    throw CreateGenericCryptographicException(isErrorDueToProfileNotLoaded);
                }

                // OOMs may be marked as success but won't return a valid pointer
                if (dataOut.pbData == null)
                {
                    throw new OutOfMemoryException();
                }

                return BufferUtil.ToManagedByteArray(dataOut.pbData, dataOut.cbData);
            }
            finally
            {
                // per MSDN, we need to use LocalFree (implemented by Marshal.FreeHGlobal) to clean up CAPI-allocated memory
                if (dataOut.pbData != null)
                {
                    Marshal.FreeHGlobal((IntPtr)dataOut.pbData);
                }
            }
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            if (protectedData == null)
            {
                throw new ArgumentNullException("protectedData");
            }

            DATA_BLOB dataOut = default(DATA_BLOB);

#if NET45
            RuntimeHelpers.PrepareConstrainedRegions();
#endif
            try
            {
                bool success;
                fixed (byte* pProtectedData = protectedData.AsFixed())
                {
                    fixed (byte* pEntropy = _entropy)
                    {
                        // no need for checked arithmetic here
                        DATA_BLOB dataIn = new DATA_BLOB() { cbData = (uint)protectedData.Length, pbData = pProtectedData };
                        DATA_BLOB optionalEntropy = new DATA_BLOB() { cbData = (uint)_entropy.Length, pbData = pEntropy };
                        success = UnsafeNativeMethods.CryptUnprotectData(&dataIn, IntPtr.Zero, &optionalEntropy, IntPtr.Zero, IntPtr.Zero, GetCryptProtectUnprotectFlags(), out dataOut);
                    }
                }

                // Did a failure occur?
                if (!success)
                {
                    throw CreateGenericCryptographicException();
                }

                // OOMs may be marked as success but won't return a valid pointer
                if (dataOut.pbData == null)
                {
                    throw new OutOfMemoryException();
                }

                return BufferUtil.ToManagedByteArray(dataOut.pbData, dataOut.cbData);
            }
            finally
            {
                // per MSDN, we need to use LocalFree (implemented by Marshal.FreeHGlobal) to clean up CAPI-allocated memory
                if (dataOut.pbData != null)
                {
                    Marshal.FreeHGlobal((IntPtr)dataOut.pbData);
                }
            }
        }
    }
}

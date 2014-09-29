// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Security.DataProtection.SafeHandles;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNet.Security.DataProtection.SP800_108
{
    /// <summary>
    /// Provides an implementation of the SP800-108-CTR-HMACSHA512 key derivation function.
    /// This class assumes at least Windows 7 / Server 2008 R2.
    /// </summary>
    /// <remarks>
    /// More info at http://csrc.nist.gov/publications/nistpubs/800-108/sp800-108.pdf, Sec. 5.1.
    /// </remarks>
    internal unsafe static class SP800_108_CTR_HMACSHA512Util
    {
        private static readonly bool _isWin8OrLater = GetIsRunningWin8OrLater();

        // Creates a provider with an empty key.
        public static ISP800_108_CTR_HMACSHA512Provider CreateEmptyProvider()
        {
            byte dummy;
            return CreateProvider(pbKdk: &dummy, cbKdk: 0);
        }

        // Creates a provider from the given key.
        public static ISP800_108_CTR_HMACSHA512Provider CreateProvider(byte* pbKdk, uint cbKdk)
        {
            return (_isWin8OrLater)
                ? (ISP800_108_CTR_HMACSHA512Provider)new Win8SP800_108_CTR_HMACSHA512Provider(pbKdk, cbKdk)
                : (ISP800_108_CTR_HMACSHA512Provider)new Win7SP800_108_CTR_HMACSHA512Provider(pbKdk, cbKdk);
        }

        // Creates a provider from the given secret.
        public static ISP800_108_CTR_HMACSHA512Provider CreateProvider(ProtectedMemoryBlob kdk)
        {
            uint secretLengthInBytes = checked((uint)kdk.Length);
            if (secretLengthInBytes == 0)
            {
                return CreateEmptyProvider();
            }
            else
            {
                fixed (byte* pbPlaintextSecret = new byte[secretLengthInBytes])
                {
                    try
                    {
                        kdk.WriteSecretIntoBuffer(pbPlaintextSecret, checked((int)secretLengthInBytes));
                        return CreateProvider(pbPlaintextSecret, secretLengthInBytes);
                    }
                    finally
                    {
                        UnsafeBufferUtil.SecureZeroMemory(pbPlaintextSecret, secretLengthInBytes);
                    }
                }
            }
        }

        private static bool GetIsRunningWin8OrLater()
        {
            // In priority order, our three implementations are Win8, Win7, and "other".

            const string BCRYPT_LIB = "bcrypt.dll";

            SafeLibraryHandle bcryptLibHandle = null;
            try
            {
                bcryptLibHandle = SafeLibraryHandle.Open(BCRYPT_LIB);
            }
            catch
            {
                // BCrypt not available? We'll fall back to managed code paths.
            }

            if (bcryptLibHandle != null)
            {
                using (bcryptLibHandle)
                {
                    if (bcryptLibHandle.DoesProcExist("BCryptKeyDerivation"))
                    {
                        // We're running on Win8+.
                        return true;
                    }
                }
            }

            // Not running on Win8+
            return false;
        }
    }
}

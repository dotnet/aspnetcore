// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using Microsoft.AspNet.DataProtection.Cng;

namespace Microsoft.AspNet.DataProtection.Dpapi
{
    internal unsafe sealed class ProtectedDataImpl : IProtectedData
    {
        public byte[] Protect(byte[] userData, byte[] optionalEntropy, DataProtectionScope scope)
        {
#if DNXCORE50
            fixed (byte* pbUserData = userData)
            {
                fixed (byte* pbOptionalEntropy = optionalEntropy)
                {
                    return DpapiSecretSerializerHelper.ProtectWithDpapiImpl(
                        pbSecret: pbUserData,
                        cbSecret: (userData != null) ? (uint)userData.Length : 0,
                        pbOptionalEntropy: pbOptionalEntropy,
                        cbOptionalEntropy: (optionalEntropy != null) ? (uint)optionalEntropy.Length : 0,
                        fLocalMachine: (scope == DataProtectionScope.LocalMachine));
                }
            }
#else
            return ProtectedData.Protect(userData, optionalEntropy, scope);
#endif
        }

        public byte[] Unprotect(byte[] encryptedData, byte[] optionalEntropy, DataProtectionScope scope)
        {
#if DNXCORE50
            Secret blob;
            fixed (byte* pbEncryptedData = encryptedData)
            {
                fixed (byte* pbOptionalEntropy = optionalEntropy)
                {
                    blob = DpapiSecretSerializerHelper.UnprotectWithDpapiImpl(
                        pbProtectedData: pbEncryptedData,
                        cbProtectedData: (encryptedData != null) ? (uint)encryptedData.Length : 0,
                        pbOptionalEntropy: pbOptionalEntropy,
                        cbOptionalEntropy: (optionalEntropy != null) ? (uint)optionalEntropy.Length : 0);
                }
            }
            using (blob)
            {
                byte[] retVal = new byte[blob.Length];
                blob.WriteSecretIntoBuffer(new ArraySegment<byte>(retVal));
                return retVal;
            }
#else
            return ProtectedData.Unprotect(encryptedData, optionalEntropy, scope);
#endif
        }
    }
}

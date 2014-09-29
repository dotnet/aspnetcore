// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Security.DataProtection.Cng;
using Microsoft.AspNet.Security.DataProtection.SafeHandles;

namespace Microsoft.AspNet.Security.DataProtection.AuthenticatedEncryption
{
    internal static class AuthenticatedEncryptorExtensions
    {
        public static byte[] Encrypt(this IAuthenticatedEncryptor encryptor, ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData, uint preBufferSize, uint postBufferSize)
        {
            // Can we call the optimized version?
            IAuthenticatedEncryptor2 optimizedEncryptor = encryptor as IAuthenticatedEncryptor2;
            if (optimizedEncryptor != null)
            {
                return optimizedEncryptor.Encrypt(plaintext, additionalAuthenticatedData, preBufferSize, postBufferSize);
            }

            // Fall back to the unoptimized version
            if (preBufferSize == 0 && postBufferSize == 0)
            {
                // optimization: call through to inner encryptor with no modifications
                return encryptor.Encrypt(plaintext, additionalAuthenticatedData);
            }
            else
            {
                byte[] temp = encryptor.Encrypt(plaintext, additionalAuthenticatedData);
                byte[] retVal = new byte[checked(preBufferSize + temp.Length + postBufferSize)];
                Buffer.BlockCopy(temp, 0, retVal, checked((int)preBufferSize), temp.Length);
                return retVal;
            }
        }
    }
}

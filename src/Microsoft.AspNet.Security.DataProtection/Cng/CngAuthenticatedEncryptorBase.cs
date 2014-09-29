// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using Microsoft.AspNet.Security.DataProtection.AuthenticatedEncryption;

namespace Microsoft.AspNet.Security.DataProtection.Cng
{
    internal unsafe abstract class CngAuthenticatedEncryptorBase : IAuthenticatedEncryptor, IDisposable
    {
        public byte[] Decrypt(ArraySegment<byte> ciphertext, ArraySegment<byte> additionalAuthenticatedData)
        {
            // This wrapper simply converts ArraySegment<byte> to byte* and calls the impl method.

            // Input validation
            ciphertext.Validate();
            additionalAuthenticatedData.Validate();

            byte dummy; // used only if plaintext or AAD is empty, since otherwise 'fixed' returns null pointer
            fixed (byte* pbCiphertextArray = ciphertext.Array)
            {
                fixed (byte* pbAdditionalAuthenticatedDataArray = additionalAuthenticatedData.Array)
                {
                    try
                    {
                        return DecryptImpl(
                            pbCiphertext: (pbCiphertextArray != null) ? &pbCiphertextArray[ciphertext.Offset] : &dummy,
                            cbCiphertext: (uint)ciphertext.Count,
                            pbAdditionalAuthenticatedData: (pbAdditionalAuthenticatedDataArray != null) ? &pbAdditionalAuthenticatedDataArray[additionalAuthenticatedData.Offset] : &dummy,
                            cbAdditionalAuthenticatedData: (uint)additionalAuthenticatedData.Count);
                    }
                    catch (Exception ex) if (!(ex is CryptographicException))
                    {
                        // Homogenize to CryptographicException.
                        throw Error.CryptCommon_GenericError(ex);
                    }
                }
            }
        }

        protected abstract byte[] DecryptImpl(byte* pbCiphertext, uint cbCiphertext, byte* pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData);

        public abstract void Dispose();

        public byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData)
        {
            return Encrypt(plaintext, additionalAuthenticatedData, 0, 0);
        }

        public byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData, uint preBufferSize, uint postBufferSize)
        {
            // This wrapper simply converts ArraySegment<byte> to byte* and calls the impl method.

            // Input validation
            plaintext.Validate();
            additionalAuthenticatedData.Validate();

            byte dummy; // used only if plaintext or AAD is empty, since otherwise 'fixed' returns null pointer
            fixed (byte* pbPlaintextArray = plaintext.Array)
            {
                fixed (byte* pbAdditionalAuthenticatedDataArray = additionalAuthenticatedData.Array)
                {
                    try
                    {
                        return EncryptImpl(
                            pbPlaintext: (pbPlaintextArray != null) ? &pbPlaintextArray[plaintext.Offset] : &dummy,
                            cbPlaintext: (uint)plaintext.Count,
                            pbAdditionalAuthenticatedData: (pbAdditionalAuthenticatedDataArray != null) ? &pbAdditionalAuthenticatedDataArray[additionalAuthenticatedData.Offset] : &dummy,
                            cbAdditionalAuthenticatedData: (uint)additionalAuthenticatedData.Count,
                            cbPreBuffer: preBufferSize,
                            cbPostBuffer: postBufferSize);
                    }
                    catch (Exception ex) if (!(ex is CryptographicException))
                    {
                        // Homogenize to CryptographicException.
                        throw Error.CryptCommon_GenericError(ex);
                    }
                }
            }
        }

        protected abstract byte[] EncryptImpl(byte* pbPlaintext, uint cbPlaintext, byte* pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData, uint cbPreBuffer, uint cbPostBuffer);
    }
}

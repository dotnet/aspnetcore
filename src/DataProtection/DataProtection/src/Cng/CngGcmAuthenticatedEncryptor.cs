// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.Cryptography.SafeHandles;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.SP800_108;

namespace Microsoft.AspNetCore.DataProtection.Cng;

// GCM is defined in NIST SP 800-38D (http://csrc.nist.gov/publications/nistpubs/800-38D/SP-800-38D.pdf).
// Heed closely the uniqueness requirements called out in Sec. 8: the probability that the GCM encryption
// routine is ever invoked on two or more distinct sets of input data with the same (key, IV) shall not
// exceed 2^-32. If we fix the key and use a random 96-bit IV for each invocation, this means that after
// 2^32 encryption operations the odds of reusing any (key, IV) pair is 2^-32 (see Sec. 8.3). This won't
// work for our use since a high-traffic web server can go through 2^32 requests in mere days. Instead,
// we'll use 224 bits of entropy for each operation, with 128 bits going to the KDF and 96 bits
// going to the IV. This means that we'll only hit the 2^-32 probability limit after 2^96 encryption
// operations, which will realistically never happen. (At the absurd rate of one encryption operation
// per nanosecond, it would still take 180 times the age of the universe to hit 2^96 operations.)
internal sealed unsafe class CngGcmAuthenticatedEncryptor : IOptimizedAuthenticatedEncryptor, IDisposable
#if NET
    , ISpanAuthenticatedEncryptor
#endif
{
    // Having a key modifier ensures with overwhelming probability that no two encryption operations
    // will ever derive the same (encryption subkey, MAC subkey) pair. This limits an attacker's
    // ability to mount a key-dependent chosen ciphertext attack. See also the class-level comment
    // for how this is used to overcome GCM's IV limitations.
    private const uint KEY_MODIFIER_SIZE_IN_BYTES = 128 / 8;

    private const uint NONCE_SIZE_IN_BYTES = 96 / 8; // GCM has a fixed 96-bit IV
    private const uint TAG_SIZE_IN_BYTES = 128 / 8; // we're hardcoding a 128-bit authentication tag size

    private readonly byte[] _contextHeader;
    private readonly IBCryptGenRandom _genRandom;
    private readonly ISP800_108_CTR_HMACSHA512Provider _sp800_108_ctr_hmac_provider;
    private readonly BCryptAlgorithmHandle _symmetricAlgorithmHandle;
    private readonly uint _symmetricAlgorithmSubkeyLengthInBytes;

    public CngGcmAuthenticatedEncryptor(Secret keyDerivationKey, BCryptAlgorithmHandle symmetricAlgorithmHandle, uint symmetricAlgorithmKeySizeInBytes, IBCryptGenRandom? genRandom = null)
    {
        // Is the key size appropriate?
        AlgorithmAssert.IsAllowableSymmetricAlgorithmKeySize(checked(symmetricAlgorithmKeySizeInBytes * 8));
        CryptoUtil.Assert(symmetricAlgorithmHandle.GetCipherBlockLength() == 128 / 8, "GCM requires a block cipher algorithm with a 128-bit block size.");

        _genRandom = genRandom ?? BCryptGenRandomImpl.Instance;
        _sp800_108_ctr_hmac_provider = SP800_108_CTR_HMACSHA512Util.CreateProvider(keyDerivationKey);
        _symmetricAlgorithmHandle = symmetricAlgorithmHandle;
        _symmetricAlgorithmSubkeyLengthInBytes = symmetricAlgorithmKeySizeInBytes;
        _contextHeader = CreateContextHeader();
    }

    public void Decrypt<TWriter>(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> additionalAuthenticatedData, ref TWriter destination) where TWriter : IBufferWriter<byte>
#if NET
        , allows ref struct
#endif
    {
        // Argument checking: input must at the absolute minimum contain a key modifier, nonce, and tag
        if (ciphertext.Length < KEY_MODIFIER_SIZE_IN_BYTES + NONCE_SIZE_IN_BYTES + TAG_SIZE_IN_BYTES)
        {
            throw Error.CryptCommon_PayloadInvalid();
        }

        var plaintextLength = checked(ciphertext.Length - (int)(KEY_MODIFIER_SIZE_IN_BYTES + NONCE_SIZE_IN_BYTES + TAG_SIZE_IN_BYTES));

        // Assumption: ciphertext := { keyModifier || nonce || encryptedData || authenticationTag }
        fixed (byte* pbCiphertext = ciphertext)
        fixed (byte* pbAdditionalAuthenticatedData = additionalAuthenticatedData)
        {
            // Calculate offsets
            byte* pbKeyModifier = pbCiphertext;
            byte* pbNonce = &pbKeyModifier[KEY_MODIFIER_SIZE_IN_BYTES];
            byte* pbEncryptedData = &pbNonce[NONCE_SIZE_IN_BYTES];
            byte* pbAuthTag = &pbEncryptedData[plaintextLength];

            // Use the KDF to recreate the symmetric block cipher key
            // We'll need a temporary buffer to hold the symmetric encryption subkey
            byte* pbSymmetricDecryptionSubkey = stackalloc byte[checked((int)_symmetricAlgorithmSubkeyLengthInBytes)];
            try
            {
                _sp800_108_ctr_hmac_provider.DeriveKeyWithContextHeader(
                    pbLabel: pbAdditionalAuthenticatedData,
                    cbLabel: (uint)additionalAuthenticatedData.Length,
                    contextHeader: _contextHeader,
                    pbContext: pbKeyModifier,
                    cbContext: KEY_MODIFIER_SIZE_IN_BYTES,
                    pbDerivedKey: pbSymmetricDecryptionSubkey,
                    cbDerivedKey: _symmetricAlgorithmSubkeyLengthInBytes);

                // Get buffer from writer with the plaintext size
                var buffer = destination.GetSpan(plaintextLength);

                // Perform the decryption operation
                using (var decryptionSubkeyHandle = _symmetricAlgorithmHandle.GenerateSymmetricKey(pbSymmetricDecryptionSubkey, _symmetricAlgorithmSubkeyLengthInBytes))
                {
                    fixed (byte* pbBuffer = buffer)
                    {
                        byte dummy;
                        byte* pbPlaintext = (plaintextLength > 0) ? pbBuffer : &dummy; // CLR doesn't like pinning empty buffers

                        BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO authInfo;
                        BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.Init(out authInfo);
                        authInfo.pbNonce = pbNonce;
                        authInfo.cbNonce = NONCE_SIZE_IN_BYTES;
                        authInfo.pbTag = pbAuthTag;
                        authInfo.cbTag = TAG_SIZE_IN_BYTES;

                        // The call to BCryptDecrypt will also validate the authentication tag
                        uint cbDecryptedBytesWritten;
                        var ntstatus = UnsafeNativeMethods.BCryptDecrypt(
                            hKey: decryptionSubkeyHandle,
                            pbInput: pbEncryptedData,
                            cbInput: (uint)plaintextLength,
                            pPaddingInfo: &authInfo,
                            pbIV: null, // IV not used; nonce provided in pPaddingInfo
                            cbIV: 0,
                            pbOutput: pbPlaintext,
                            cbOutput: (uint)plaintextLength,
                            pcbResult: out cbDecryptedBytesWritten,
                            dwFlags: 0);
                        UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);
                        CryptoUtil.Assert(cbDecryptedBytesWritten == plaintextLength, "cbDecryptedBytesWritten == plaintextLength");

                        // Advance the writer by the number of bytes written
                        destination.Advance((int)cbDecryptedBytesWritten);
                    }
                }
            }
            finally
            {
                // The buffer contains key material, so delete it.
                UnsafeBufferUtil.SecureZeroMemory(pbSymmetricDecryptionSubkey, _symmetricAlgorithmSubkeyLengthInBytes);
            }
        }
    }

    public byte[] Decrypt(ArraySegment<byte> ciphertext, ArraySegment<byte> additionalAuthenticatedData)
    {
        ciphertext.Validate();
        additionalAuthenticatedData.Validate();

        var outputSize = ciphertext.Count - (int)(KEY_MODIFIER_SIZE_IN_BYTES + NONCE_SIZE_IN_BYTES + TAG_SIZE_IN_BYTES);
        if (outputSize <= 0)
        {
            throw Error.CryptCommon_PayloadInvalid();
        }

#if NET
        byte[] rentedBuffer = null!;
        var buffer = outputSize < 256
            ? stackalloc byte[255]
            : (rentedBuffer = ArrayPool<byte>.Shared.Rent(outputSize));

        var refPooledBuffer = new RefPooledArrayBufferWriter<byte>(buffer);
        try
        {
            Decrypt(ciphertext, additionalAuthenticatedData, ref refPooledBuffer);
            return refPooledBuffer.WrittenSpan.ToArray();
        }
        finally
        {
            refPooledBuffer.Dispose();
            if (rentedBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: true);
            }
        }
#else
        var pooledArrayBuffer = new PooledArrayBufferWriter<byte>(outputSize);
        try
        {
            Decrypt(ciphertext, additionalAuthenticatedData, ref pooledArrayBuffer);
            return pooledArrayBuffer.WrittenSpan.ToArray();
        }
        finally
        {
            pooledArrayBuffer.Dispose();
        }
#endif
    }

    // 'pbNonce' must point to a 96-bit buffer.
    // 'pbTag' must point to a 128-bit buffer.
    // 'pbEncryptedData' must point to a buffer the same length as 'pbPlaintextData'.
    private void DoGcmEncrypt(byte* pbKey, uint cbKey, byte* pbNonce, byte* pbPlaintextData, uint cbPlaintextData, byte* pbEncryptedData, byte* pbTag)
    {
        BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO authCipherInfo;
        BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.Init(out authCipherInfo);
        authCipherInfo.pbNonce = pbNonce;
        authCipherInfo.cbNonce = NONCE_SIZE_IN_BYTES;
        authCipherInfo.pbTag = pbTag;
        authCipherInfo.cbTag = TAG_SIZE_IN_BYTES;

        using (var keyHandle = _symmetricAlgorithmHandle.GenerateSymmetricKey(pbKey, cbKey))
        {
            uint cbResult;
            var ntstatus = UnsafeNativeMethods.BCryptEncrypt(
                hKey: keyHandle,
                pbInput: pbPlaintextData,
                cbInput: cbPlaintextData,
                pPaddingInfo: &authCipherInfo,
                pbIV: null,
                cbIV: 0,
                pbOutput: pbEncryptedData,
                cbOutput: cbPlaintextData,
                pcbResult: out cbResult,
                dwFlags: 0);
            UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);
            CryptoUtil.Assert(cbResult == cbPlaintextData, "cbResult == cbPlaintextData");
        }
    }

    public void Encrypt<TWriter>(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> additionalAuthenticatedData, ref TWriter destination) where TWriter : IBufferWriter<byte>
#if NET
    , allows ref struct
#endif
    {
        try
        {
            // Calculate total required size: keyModifier + nonce + plaintext + tag
            // In GCM, ciphertext length equals plaintext length
            var totalRequiredSize = checked((int)(KEY_MODIFIER_SIZE_IN_BYTES + NONCE_SIZE_IN_BYTES + plaintext.Length + TAG_SIZE_IN_BYTES));

            // Get buffer from writer with the required total size
            var buffer = destination.GetSpan(totalRequiredSize);

            fixed (byte* pbBuffer = buffer)
            {
                // Calculate offsets
                byte* pbKeyModifier = pbBuffer;
                byte* pbNonce = &pbKeyModifier[KEY_MODIFIER_SIZE_IN_BYTES];
                byte* pbEncryptedData = &pbNonce[NONCE_SIZE_IN_BYTES];
                byte* pbAuthTag = &pbEncryptedData[plaintext.Length];

                // Randomly generate the key modifier and nonce
                _genRandom.GenRandom(pbKeyModifier, KEY_MODIFIER_SIZE_IN_BYTES + NONCE_SIZE_IN_BYTES);

                // Use the KDF to generate a new symmetric block cipher key
                // We'll need a temporary buffer to hold the symmetric encryption subkey
                byte* pbSymmetricEncryptionSubkey = stackalloc byte[checked((int)_symmetricAlgorithmSubkeyLengthInBytes)];
                try
                {
                    fixed (byte* pbAdditionalAuthenticatedData = additionalAuthenticatedData)
                    {
                        _sp800_108_ctr_hmac_provider.DeriveKeyWithContextHeader(
                            pbLabel: pbAdditionalAuthenticatedData,
                            cbLabel: (uint)additionalAuthenticatedData.Length,
                            contextHeader: _contextHeader,
                            pbContext: pbKeyModifier,
                            cbContext: KEY_MODIFIER_SIZE_IN_BYTES,
                            pbDerivedKey: pbSymmetricEncryptionSubkey,
                            cbDerivedKey: _symmetricAlgorithmSubkeyLengthInBytes);
                    }

                    // Perform the encryption operation
                    byte dummy;
                    fixed (byte* pbPlaintextArray = plaintext)
                    {
                        var pbPlaintext = (pbPlaintextArray != null) ? pbPlaintextArray : &dummy;

                        DoGcmEncrypt(
                            pbKey: pbSymmetricEncryptionSubkey,
                            cbKey: _symmetricAlgorithmSubkeyLengthInBytes,
                            pbNonce: pbNonce,
                            pbPlaintextData: pbPlaintext,
                            cbPlaintextData: (uint)plaintext.Length,
                            pbEncryptedData: pbEncryptedData,
                            pbTag: pbAuthTag);
                    }

                    // Advance the writer by the total bytes written
                    destination.Advance(totalRequiredSize);
                }
                finally
                {
                    // The buffer contains key material, so delete it.
                    UnsafeBufferUtil.SecureZeroMemory(pbSymmetricEncryptionSubkey, _symmetricAlgorithmSubkeyLengthInBytes);
                }
            }
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            throw Error.CryptCommon_GenericError(ex);
        }
    }

    public byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData)
        => Encrypt(plaintext, additionalAuthenticatedData, 0, 0);

    public byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData, uint preBufferSize, uint postBufferSize)
    {
        plaintext.Validate();
        additionalAuthenticatedData.Validate();

        var size = checked((int)(KEY_MODIFIER_SIZE_IN_BYTES + NONCE_SIZE_IN_BYTES + plaintext.Count + TAG_SIZE_IN_BYTES));
        var outputSize = (int)(preBufferSize + size + postBufferSize);
#if NET
        byte[] rentedBuffer = null!;
        var buffer = outputSize < 256
            ? stackalloc byte[255]
            : (rentedBuffer = ArrayPool<byte>.Shared.Rent(outputSize));

        var refPooledBuffer = new RefPooledArrayBufferWriter<byte>(buffer);
        try
        {
            // arrays are pooled. and they MAY contain non-zeros in the pre-buffer and post-buffer regions.
            // we could clean them up, but it's not strictly necessary - the important part is that output array
            // has those pre/post buffer regions, which will be used by the caller.
            refPooledBuffer.Advance((int)preBufferSize);
            Encrypt(plaintext, additionalAuthenticatedData, ref refPooledBuffer);
            refPooledBuffer.Advance((int)postBufferSize);

            var resultSpan = refPooledBuffer.WrittenSpan.ToArray();
            CryptoUtil.Assert(resultSpan.Length == outputSize, "writtenSpan length should equal calculated outputSize");
            return resultSpan;
        }
        finally
        {
            refPooledBuffer.Dispose();
            if (rentedBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: true);
            }
        }
#else
        var pooledArrayBuffer = new PooledArrayBufferWriter<byte>(outputSize);
        try
        {
            pooledArrayBuffer.Advance((int)preBufferSize);
            Encrypt(plaintext, additionalAuthenticatedData, ref pooledArrayBuffer);
            pooledArrayBuffer.Advance((int)postBufferSize);

            var resultSpan = pooledArrayBuffer.WrittenSpan.ToArray();
            CryptoUtil.Assert(resultSpan.Length == outputSize, "writtenSpan length should equal calculated outputSize");
            return resultSpan;
        }
        finally
        {
            pooledArrayBuffer.Dispose();
        }
#endif
    }

    private byte[] CreateContextHeader()
    {
        var retVal = new byte[checked(
            1 /* KDF alg */
            + 1 /* chaining mode */
            + sizeof(uint) /* sym alg key size */
            + sizeof(uint) /* GCM nonce size */
            + sizeof(uint) /* sym alg block size */
            + sizeof(uint) /* GCM tag size */
            + TAG_SIZE_IN_BYTES /* tag of GCM-encrypted empty string */)];

        fixed (byte* pbRetVal = retVal)
        {
            byte* ptr = pbRetVal;

            // First is the two-byte header
            *(ptr++) = 0; // 0x00 = SP800-108 CTR KDF w/ HMACSHA512 PRF
            *(ptr++) = 1; // 0x01 = GCM encryption + authentication

            // Next is information about the symmetric algorithm (key size, nonce size, block size, tag size)
            BitHelpers.WriteTo(ref ptr, _symmetricAlgorithmSubkeyLengthInBytes);
            BitHelpers.WriteTo(ref ptr, NONCE_SIZE_IN_BYTES);
            BitHelpers.WriteTo(ref ptr, TAG_SIZE_IN_BYTES); // block size = tag size
            BitHelpers.WriteTo(ref ptr, TAG_SIZE_IN_BYTES);

            // See the design document for an explanation of the following code.
            var tempKeys = new byte[_symmetricAlgorithmSubkeyLengthInBytes];
            fixed (byte* pbTempKeys = tempKeys)
            {
                byte dummy;

                // Derive temporary key for encryption.
                using (var provider = SP800_108_CTR_HMACSHA512Util.CreateEmptyProvider())
                {
                    provider.DeriveKey(
                        pbLabel: &dummy,
                        cbLabel: 0,
                        pbContext: &dummy,
                        cbContext: 0,
                        pbDerivedKey: pbTempKeys,
                        cbDerivedKey: (uint)tempKeys.Length);
                }

                // Encrypt a zero-length input string with an all-zero nonce and copy the tag to the return buffer.
                byte* pbNonce = stackalloc byte[(int)NONCE_SIZE_IN_BYTES];
                UnsafeBufferUtil.SecureZeroMemory(pbNonce, NONCE_SIZE_IN_BYTES);
                DoGcmEncrypt(
                    pbKey: pbTempKeys,
                    cbKey: _symmetricAlgorithmSubkeyLengthInBytes,
                    pbNonce: pbNonce,
                    pbPlaintextData: &dummy,
                    cbPlaintextData: 0,
                    pbEncryptedData: &dummy,
                    pbTag: ptr);
            }

            ptr += TAG_SIZE_IN_BYTES;
            CryptoUtil.Assert(ptr - pbRetVal == retVal.Length, "ptr - pbRetVal == retVal.Length");
        }

        // retVal := { version || chainingMode || symAlgKeySize || nonceSize || symAlgBlockSize || symAlgTagSize || TAG-of-E("") }.
        return retVal;
    }

    public void Dispose()
    {
        _sp800_108_ctr_hmac_provider.Dispose();

        // We don't want to dispose of the underlying algorithm instances because they
        // might be reused.
    }
}

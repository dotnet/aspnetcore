// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.Cryptography.SafeHandles;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.SP800_108;

namespace Microsoft.AspNetCore.DataProtection.Cng;

// An encryptor which does Encrypt(CBC) + HMAC using the Windows CNG (BCrypt*) APIs.
// The payloads produced by this encryptor should be compatible with the payloads
// produced by the managed Encrypt(CBC) + HMAC encryptor.
internal sealed unsafe class CbcAuthenticatedEncryptor : IOptimizedAuthenticatedEncryptor, IDisposable
#if NET
    , ISpanAuthenticatedEncryptor
#endif
{
    // Even when IVs are chosen randomly, CBC is susceptible to IV collisions within a single
    // key. For a 64-bit block cipher (like 3DES), we'd expect a collision after 2^32 block
    // encryption operations, which a high-traffic web server might perform in mere hours.
    // AES and other 128-bit block ciphers are less susceptible to this due to the larger IV
    // space, but unfortunately some organizations require older 64-bit block ciphers. To address
    // the collision issue, we'll feed 128 bits of entropy to the KDF when performing subkey
    // generation. This creates >= 192 bits total entropy for each operation, so we shouldn't
    // expect a collision until >= 2^96 operations. Even 2^80 operations still maintains a <= 2^-32
    // probability of collision, and this is acceptable for the expected KDK lifetime.
    private const uint KEY_MODIFIER_SIZE_IN_BYTES = 128 / 8;

    private readonly byte[] _contextHeader;
    private readonly IBCryptGenRandom _genRandom;
    private readonly BCryptAlgorithmHandle _hmacAlgorithmHandle;
    private readonly uint _hmacAlgorithmDigestLengthInBytes;
    private readonly uint _hmacAlgorithmSubkeyLengthInBytes;
    private readonly ISP800_108_CTR_HMACSHA512Provider _sp800_108_ctr_hmac_provider;
    private readonly BCryptAlgorithmHandle _symmetricAlgorithmHandle;
    private readonly uint _symmetricAlgorithmBlockSizeInBytes;
    private readonly uint _symmetricAlgorithmSubkeyLengthInBytes;

    public CbcAuthenticatedEncryptor(Secret keyDerivationKey, BCryptAlgorithmHandle symmetricAlgorithmHandle, uint symmetricAlgorithmKeySizeInBytes, BCryptAlgorithmHandle hmacAlgorithmHandle, IBCryptGenRandom? genRandom = null)
    {
        _genRandom = genRandom ?? BCryptGenRandomImpl.Instance;
        _sp800_108_ctr_hmac_provider = SP800_108_CTR_HMACSHA512Util.CreateProvider(keyDerivationKey);
        _symmetricAlgorithmHandle = symmetricAlgorithmHandle;
        _symmetricAlgorithmBlockSizeInBytes = symmetricAlgorithmHandle.GetCipherBlockLength();
        _symmetricAlgorithmSubkeyLengthInBytes = symmetricAlgorithmKeySizeInBytes;
        _hmacAlgorithmHandle = hmacAlgorithmHandle;
        _hmacAlgorithmDigestLengthInBytes = hmacAlgorithmHandle.GetHashDigestLength();
        _hmacAlgorithmSubkeyLengthInBytes = _hmacAlgorithmDigestLengthInBytes; // for simplicity we'll generate HMAC subkeys with a length equal to the digest length

        // Argument checking on the algorithms and lengths passed in to us
        AlgorithmAssert.IsAllowableSymmetricAlgorithmBlockSize(checked(_symmetricAlgorithmBlockSizeInBytes * 8));
        AlgorithmAssert.IsAllowableSymmetricAlgorithmKeySize(checked(_symmetricAlgorithmSubkeyLengthInBytes * 8));
        AlgorithmAssert.IsAllowableValidationAlgorithmDigestSize(checked(_hmacAlgorithmDigestLengthInBytes * 8));

        _contextHeader = CreateContextHeader();
    }

    public void Decrypt<TWriter>(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> additionalAuthenticatedData, ref TWriter destination) where TWriter : IBufferWriter<byte>
#if NET
        , allows ref struct
#endif
    {
        // Argument checking - input must at the absolute minimum contain a key modifier, IV, and MAC
        if (ciphertext.Length < checked(KEY_MODIFIER_SIZE_IN_BYTES + _symmetricAlgorithmBlockSizeInBytes + _hmacAlgorithmDigestLengthInBytes))
        {
            throw Error.CryptCommon_PayloadInvalid();
        }

        var cbEncryptedDataLength = checked(ciphertext.Length - (int)(KEY_MODIFIER_SIZE_IN_BYTES + _symmetricAlgorithmBlockSizeInBytes + _hmacAlgorithmDigestLengthInBytes));

        // Assumption: ciphertext := { keyModifier | IV | encryptedData | MAC(IV | encryptedPayload) }
        fixed (byte* pbCiphertext = ciphertext)
        fixed (byte* pbAdditionalAuthenticatedData = additionalAuthenticatedData)
        {
            // Calculate offsets
            byte* pbKeyModifier = pbCiphertext;
            byte* pbIV = &pbKeyModifier[KEY_MODIFIER_SIZE_IN_BYTES];
            byte* pbEncryptedData = &pbIV[_symmetricAlgorithmBlockSizeInBytes];
            byte* pbActualHmac = &pbEncryptedData[cbEncryptedDataLength];

            // Use the KDF to recreate the symmetric encryption and HMAC subkeys
            // We'll need a temporary buffer to hold them
            var cbTempSubkeys = checked(_symmetricAlgorithmSubkeyLengthInBytes + _hmacAlgorithmSubkeyLengthInBytes);
            byte* pbTempSubkeys = stackalloc byte[checked((int)cbTempSubkeys)];
            try
            {
                _sp800_108_ctr_hmac_provider.DeriveKeyWithContextHeader(
                    pbLabel: pbAdditionalAuthenticatedData,
                    cbLabel: (uint)additionalAuthenticatedData.Length,
                    contextHeader: _contextHeader,
                    pbContext: pbKeyModifier,
                    cbContext: KEY_MODIFIER_SIZE_IN_BYTES,
                    pbDerivedKey: pbTempSubkeys,
                    cbDerivedKey: cbTempSubkeys);

                // Calculate offsets
                byte* pbSymmetricEncryptionSubkey = pbTempSubkeys;
                byte* pbHmacSubkey = &pbTempSubkeys[_symmetricAlgorithmSubkeyLengthInBytes];

                // First, perform an explicit integrity check over (iv | encryptedPayload) to ensure the
                // data hasn't been tampered with. The integrity check is also implicitly performed over
                // keyModifier since that value was provided to the KDF earlier.
                using (var hashHandle = _hmacAlgorithmHandle.CreateHmac(pbHmacSubkey, _hmacAlgorithmSubkeyLengthInBytes))
                {
                    if (!ValidateHash(hashHandle, pbIV, _symmetricAlgorithmBlockSizeInBytes + (uint)cbEncryptedDataLength, pbActualHmac))
                    {
                        throw Error.CryptCommon_PayloadInvalid();
                    }
                }

                // If the integrity check succeeded, decrypt the payload.
                using (var decryptionSubkeyHandle = _symmetricAlgorithmHandle.GenerateSymmetricKey(pbSymmetricEncryptionSubkey, _symmetricAlgorithmSubkeyLengthInBytes))
                {
                    // BCryptDecrypt mutates the provided IV; we need to clone it to prevent mutation of the original value
                    byte* pbClonedIV = stackalloc byte[checked((int)_symmetricAlgorithmBlockSizeInBytes)];
                    UnsafeBufferUtil.BlockCopy(from: pbIV, to: pbClonedIV, byteCount: _symmetricAlgorithmBlockSizeInBytes);

                    // First, query the output size needed
                    var ntstatus = UnsafeNativeMethods.BCryptDecrypt(
                        hKey: decryptionSubkeyHandle,
                        pbInput: pbEncryptedData,
                        cbInput: (uint)cbEncryptedDataLength,
                        pPaddingInfo: null,
                        pbIV: pbClonedIV,
                        cbIV: _symmetricAlgorithmBlockSizeInBytes,
                        pbOutput: null,  // NULL output = size query only
                        cbOutput: 0,
                        pcbResult: out var dwRequiredSize,
                        dwFlags: BCryptEncryptFlags.BCRYPT_BLOCK_PADDING);
                    UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);

                    // Get buffer from writer with the required size
                    var buffer = destination.GetSpan(checked((int)dwRequiredSize));

                    // Clone IV again for the actual decryption call
                    byte* pbClonedIV2 = stackalloc byte[(int)_symmetricAlgorithmBlockSizeInBytes];
                    UnsafeBufferUtil.BlockCopy(from: pbIV, to: pbClonedIV2, byteCount: _symmetricAlgorithmBlockSizeInBytes);

                    // Perform the actual decryption
                    fixed (byte* pbBuffer = buffer)
                    {
                        byte dummy;
                        ntstatus = UnsafeNativeMethods.BCryptDecrypt(
                            hKey: decryptionSubkeyHandle,
                            pbInput: pbEncryptedData,
                            cbInput: (uint)cbEncryptedDataLength,
                            pPaddingInfo: null,
                            pbIV: pbClonedIV2,
                            cbIV: _symmetricAlgorithmBlockSizeInBytes,
                            pbOutput: (buffer.Length > 0) ? pbBuffer : &dummy,
                            cbOutput: (uint)buffer.Length,
                            pcbResult: out var dwActualDecryptedByteCount,
                            dwFlags: BCryptEncryptFlags.BCRYPT_BLOCK_PADDING);

                        UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);

                        // Advance the writer by the number of bytes actually written
                        destination.Advance(checked((int)dwActualDecryptedByteCount));
                    }
                }
            }
            finally
            {
                // Buffer contains sensitive key material; delete.
                UnsafeBufferUtil.SecureZeroMemory(pbTempSubkeys, cbTempSubkeys);
            }
        }
    }

    public byte[] Decrypt(ArraySegment<byte> ciphertext, ArraySegment<byte> additionalAuthenticatedData)
    {
        ciphertext.Validate();
        additionalAuthenticatedData.Validate();

        var outputSize = ciphertext.Count - (int)(KEY_MODIFIER_SIZE_IN_BYTES + _symmetricAlgorithmBlockSizeInBytes + _hmacAlgorithmDigestLengthInBytes);
        if (outputSize <= 0)
        {
            throw Error.CryptCommon_PayloadInvalid();
        }

#if NET
        byte[]? rentedBuffer = null;
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

    // 'pbIV' must be a pointer to a buffer equal in length to the symmetric algorithm block size.
    private void DoCbcEncrypt(BCryptKeyHandle symmetricKeyHandle, byte* pbIV, byte* pbInput, uint cbInput, byte* pbOutput, uint cbOutput)
    {
        // BCryptEncrypt mutates the provided IV; we need to clone it to prevent mutation of the original value
        byte* pbClonedIV = stackalloc byte[checked((int)_symmetricAlgorithmBlockSizeInBytes)];
        UnsafeBufferUtil.BlockCopy(from: pbIV, to: pbClonedIV, byteCount: _symmetricAlgorithmBlockSizeInBytes);

        uint dwEncryptedBytes;
        var ntstatus = UnsafeNativeMethods.BCryptEncrypt(
            hKey: symmetricKeyHandle,
            pbInput: pbInput,
            cbInput: cbInput,
            pPaddingInfo: null,
            pbIV: pbClonedIV,
            cbIV: _symmetricAlgorithmBlockSizeInBytes,
            pbOutput: pbOutput,
            cbOutput: cbOutput,
            pcbResult: out dwEncryptedBytes,
            dwFlags: BCryptEncryptFlags.BCRYPT_BLOCK_PADDING);
        UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);

        // Need to make sure we didn't underrun the buffer - means caller passed a bad value
        CryptoUtil.Assert(dwEncryptedBytes == cbOutput, "dwEncryptedBytes == cbOutput");
    }

    public int GetEncryptedSize(int plainTextLength)
    {
        uint paddedCiphertextLength = GetCbcEncryptedOutputSizeWithPadding((uint)plainTextLength);
        return checked((int)(KEY_MODIFIER_SIZE_IN_BYTES + _symmetricAlgorithmBlockSizeInBytes + paddedCiphertextLength + _hmacAlgorithmDigestLengthInBytes));
    }

    public void Encrypt<TWriter>(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> additionalAuthenticatedData, ref TWriter destination) where TWriter : IBufferWriter<byte>
#if NET
        , allows ref struct
#endif
    {
        // This buffer will be used to hold the symmetric encryption and HMAC subkeys
        // used in the generation of this payload.
        var cbTempSubkeys = checked(_symmetricAlgorithmSubkeyLengthInBytes + _hmacAlgorithmSubkeyLengthInBytes);
        byte* pbTempSubkeys = stackalloc byte[checked((int)cbTempSubkeys)];

        try
        {
            // Randomly generate the key modifier and IV.
            var cbKeyModifierAndIV = checked(KEY_MODIFIER_SIZE_IN_BYTES + _symmetricAlgorithmBlockSizeInBytes);
            byte* pbKeyModifierAndIV = stackalloc byte[checked((int)cbKeyModifierAndIV)];
            _genRandom.GenRandom(pbKeyModifierAndIV, cbKeyModifierAndIV);

            // Calculate offsets
            byte* pbKeyModifier = pbKeyModifierAndIV;
            byte* pbIV = &pbKeyModifierAndIV[KEY_MODIFIER_SIZE_IN_BYTES];

            // Use the KDF to generate a new symmetric encryption and HMAC subkey
            fixed (byte* pbAdditionalAuthenticatedData = additionalAuthenticatedData)
            {
                _sp800_108_ctr_hmac_provider.DeriveKeyWithContextHeader(
                    pbLabel: pbAdditionalAuthenticatedData,
                    cbLabel: (uint)additionalAuthenticatedData.Length,
                    contextHeader: _contextHeader,
                    pbContext: pbKeyModifier,
                    cbContext: KEY_MODIFIER_SIZE_IN_BYTES,
                    pbDerivedKey: pbTempSubkeys,
                    cbDerivedKey: cbTempSubkeys);
            }

            // Calculate offsets
            byte* pbSymmetricEncryptionSubkey = pbTempSubkeys;
            byte* pbHmacSubkey = &pbTempSubkeys[_symmetricAlgorithmSubkeyLengthInBytes];

            using (var symmetricKeyHandle = _symmetricAlgorithmHandle.GenerateSymmetricKey(pbSymmetricEncryptionSubkey, _symmetricAlgorithmSubkeyLengthInBytes))
            {
                // Query the padded ciphertext output size
                byte dummy;
                fixed (byte* pbPlaintextArray = plaintext)
                {
                    var pbPlaintext = (pbPlaintextArray != null) ? pbPlaintextArray : &dummy;

                    // First, query the size needed for ciphertext
                    var ntstatus = UnsafeNativeMethods.BCryptEncrypt(
                        hKey: symmetricKeyHandle,
                        pbInput: pbPlaintext,
                        cbInput: (uint)plaintext.Length,
                        pPaddingInfo: null,
                        pbIV: pbIV,
                        cbIV: _symmetricAlgorithmBlockSizeInBytes,
                        pbOutput: null,  // NULL output = size query only
                        cbOutput: 0,
                        pcbResult: out var dwCiphertextSize,
                        dwFlags: BCryptEncryptFlags.BCRYPT_BLOCK_PADDING);
                    UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);

                    // Calculate total required size
                    var totalRequiredSize = checked((int)(cbKeyModifierAndIV + dwCiphertextSize + _hmacAlgorithmDigestLengthInBytes));

                    // Get buffer from writer with the required total size
                    var buffer = destination.GetSpan(totalRequiredSize);

                    fixed (byte* pbBuffer = buffer)
                    {
                        // Calculate offsets in destination buffer
                        byte* pbOutputKeyModifier = pbBuffer;
                        byte* pbOutputIV = &pbOutputKeyModifier[KEY_MODIFIER_SIZE_IN_BYTES];
                        byte* pbOutputCiphertext = &pbOutputIV[_symmetricAlgorithmBlockSizeInBytes];
                        byte* pbOutputHmac = &pbOutputCiphertext[dwCiphertextSize];

                        // Copy key modifier and IV to output
                        Unsafe.CopyBlock(pbOutputKeyModifier, pbKeyModifierAndIV, cbKeyModifierAndIV);

                        // Clone IV for encryption (BCryptEncrypt mutates it)
                        byte* pbClonedIV = stackalloc byte[checked((int)_symmetricAlgorithmBlockSizeInBytes)];
                        UnsafeBufferUtil.BlockCopy(from: pbIV, to: pbClonedIV, byteCount: _symmetricAlgorithmBlockSizeInBytes);

                        // Perform encryption
                        ntstatus = UnsafeNativeMethods.BCryptEncrypt(
                            hKey: symmetricKeyHandle,
                            pbInput: pbPlaintext,
                            cbInput: (uint)plaintext.Length,
                            pPaddingInfo: null,
                            pbIV: pbClonedIV,
                            cbIV: _symmetricAlgorithmBlockSizeInBytes,
                            pbOutput: pbOutputCiphertext,
                            cbOutput: dwCiphertextSize,
                            pcbResult: out var dwActualCiphertextSize,
                            dwFlags: BCryptEncryptFlags.BCRYPT_BLOCK_PADDING);
                        UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);

                        CryptoUtil.Assert(dwActualCiphertextSize == dwCiphertextSize, "dwActualCiphertextSize == dwCiphertextSize");

                        // Calculate HMAC over (IV | ciphertext)
                        using (var hashHandle = _hmacAlgorithmHandle.CreateHmac(pbHmacSubkey, _hmacAlgorithmSubkeyLengthInBytes))
                        {
                            hashHandle.HashData(
                                pbInput: pbOutputIV,
                                cbInput: checked(_symmetricAlgorithmBlockSizeInBytes + dwCiphertextSize),
                                pbHashDigest: pbOutputHmac,
                                cbHashDigest: _hmacAlgorithmDigestLengthInBytes);
                        }

                        // Advance the writer by the total bytes written
                        destination.Advance(totalRequiredSize);
                    }
                }
            }
        }
        finally
        {
            // Buffer contains sensitive material; delete it.
            UnsafeBufferUtil.SecureZeroMemory(pbTempSubkeys, cbTempSubkeys);
        }
    }

    public byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData)
        => Encrypt(plaintext, additionalAuthenticatedData, 0, 0);

    public byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData, uint preBufferSize, uint postBufferSize)
    {
        plaintext.Validate();
        additionalAuthenticatedData.Validate();

        var size = GetEncryptedSize(plaintext.Count);
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
            pooledArrayBuffer.Advance(preBufferSize);
            Encrypt(plaintext, additionalAuthenticatedData, ref pooledArrayBuffer);
            pooledArrayBuffer.Advance(postBufferSize);

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

    /// <summary>
    /// Should be used only for expected encrypt/decrypt size calculation,
    /// use the other overload <see cref="GetCbcEncryptedOutputSizeWithPadding(BCryptKeyHandle, byte*, uint)"/>
    /// for the actual encryption algorithm
    /// </summary>
    private uint GetCbcEncryptedOutputSizeWithPadding(uint cbInput)
    {
        // Create a temporary key with dummy data for size calculation only
        // The actual key material doesn't matter for size calculation
        byte* pbDummyKey = stackalloc byte[checked((int)_symmetricAlgorithmSubkeyLengthInBytes)];
        // Leave pbDummyKey uninitialized (all zeros) - BCrypt doesn't care for size queries

        using var tempKeyHandle = _symmetricAlgorithmHandle.GenerateSymmetricKey(pbDummyKey, _symmetricAlgorithmSubkeyLengthInBytes);

        // Use uninitialized IV and input data - only the lengths matter
        byte* pbDummyIV = stackalloc byte[checked((int)_symmetricAlgorithmBlockSizeInBytes)];
        byte* pbDummyInput = stackalloc byte[checked((int)cbInput)];

        var ntstatus = UnsafeNativeMethods.BCryptEncrypt(
            hKey: tempKeyHandle,
            pbInput: pbDummyInput,
            cbInput: cbInput,
            pPaddingInfo: null,
            pbIV: pbDummyIV,
            cbIV: _symmetricAlgorithmBlockSizeInBytes,
            pbOutput: null,  // NULL output = size query only
            cbOutput: 0,
            pcbResult: out var dwResult,
            dwFlags: BCryptEncryptFlags.BCRYPT_BLOCK_PADDING);
        UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);

        return dwResult;
    }

    private uint GetCbcEncryptedOutputSizeWithPadding(BCryptKeyHandle symmetricKeyHandle, byte* pbInput, uint cbInput)
    {
        // ok for this memory to remain uninitialized since nobody depends on it
        byte* pbIV = stackalloc byte[checked((int)_symmetricAlgorithmBlockSizeInBytes)];

        // Calling BCryptEncrypt with a null output pointer will cause it to return the total number
        // of bytes required for the output buffer.
        var ntstatus = UnsafeNativeMethods.BCryptEncrypt(
            hKey: symmetricKeyHandle,
            pbInput: pbInput,
            cbInput: cbInput,
            pPaddingInfo: null,
            pbIV: pbIV,
            cbIV: _symmetricAlgorithmBlockSizeInBytes,
            pbOutput: null,
            cbOutput: 0,
            pcbResult: out var dwResult,
            dwFlags: BCryptEncryptFlags.BCRYPT_BLOCK_PADDING);
        UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);

        return dwResult;
    }

    // 'pbExpectedDigest' must point to a '_hmacAlgorithmDigestLengthInBytes'-length buffer
    private bool ValidateHash(BCryptHashHandle hashHandle, byte* pbInput, uint cbInput, byte* pbExpectedDigest)
    {
        byte* pbActualDigest = stackalloc byte[checked((int)_hmacAlgorithmDigestLengthInBytes)];
        hashHandle.HashData(pbInput, cbInput, pbActualDigest, _hmacAlgorithmDigestLengthInBytes);
        return CryptoUtil.TimeConstantBuffersAreEqual(pbExpectedDigest, pbActualDigest, _hmacAlgorithmDigestLengthInBytes);
    }

    private byte[] CreateContextHeader()
    {
        var retVal = new byte[checked(
            1 /* KDF alg */
            + 1 /* chaining mode */
            + sizeof(uint) /* sym alg key size */
            + sizeof(uint) /* sym alg block size */
            + sizeof(uint) /* hmac alg key size */
            + sizeof(uint) /* hmac alg digest size */
            + _symmetricAlgorithmBlockSizeInBytes /* ciphertext of encrypted empty string */
            + _hmacAlgorithmDigestLengthInBytes /* digest of HMACed empty string */)];

        fixed (byte* pbRetVal = retVal)
        {
            byte* ptr = pbRetVal;

            // First is the two-byte header
            *(ptr++) = 0; // 0x00 = SP800-108 CTR KDF w/ HMACSHA512 PRF
            *(ptr++) = 0; // 0x00 = CBC encryption + HMAC authentication

            // Next is information about the symmetric algorithm (key size followed by block size)
            BitHelpers.WriteTo(ref ptr, _symmetricAlgorithmSubkeyLengthInBytes);
            BitHelpers.WriteTo(ref ptr, _symmetricAlgorithmBlockSizeInBytes);

            // Next is information about the HMAC algorithm (key size followed by digest size)
            BitHelpers.WriteTo(ref ptr, _hmacAlgorithmSubkeyLengthInBytes);
            BitHelpers.WriteTo(ref ptr, _hmacAlgorithmDigestLengthInBytes);

            // See the design document for an explanation of the following code.
            var tempKeys = new byte[_symmetricAlgorithmSubkeyLengthInBytes + _hmacAlgorithmSubkeyLengthInBytes];
            fixed (byte* pbTempKeys = tempKeys)
            {
                byte dummy;

                // Derive temporary keys for encryption + HMAC.
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

                // At this point, tempKeys := { K_E || K_H }.
                byte* pbSymmetricEncryptionSubkey = pbTempKeys;
                byte* pbHmacSubkey = &pbTempKeys[_symmetricAlgorithmSubkeyLengthInBytes];

                // Encrypt a zero-length input string with an all-zero IV and copy the ciphertext to the return buffer.
                using (var symmetricKeyHandle = _symmetricAlgorithmHandle.GenerateSymmetricKey(pbSymmetricEncryptionSubkey, _symmetricAlgorithmSubkeyLengthInBytes))
                {
                    fixed (byte* pbIV = new byte[_symmetricAlgorithmBlockSizeInBytes] /* will be zero-initialized */)
                    {
                        DoCbcEncrypt(
                            symmetricKeyHandle: symmetricKeyHandle,
                            pbIV: pbIV,
                            pbInput: &dummy,
                            cbInput: 0,
                            pbOutput: ptr,
                            cbOutput: _symmetricAlgorithmBlockSizeInBytes);
                    }
                }
                ptr += _symmetricAlgorithmBlockSizeInBytes;

                // MAC a zero-length input string and copy the digest to the return buffer.
                using (var hashHandle = _hmacAlgorithmHandle.CreateHmac(pbHmacSubkey, _hmacAlgorithmSubkeyLengthInBytes))
                {
                    hashHandle.HashData(
                        pbInput: &dummy,
                        cbInput: 0,
                        pbHashDigest: ptr,
                        cbHashDigest: _hmacAlgorithmDigestLengthInBytes);
                }

                ptr += _hmacAlgorithmDigestLengthInBytes;
                CryptoUtil.Assert(ptr - pbRetVal == retVal.Length, "ptr - pbRetVal == retVal.Length");
            }
        }

        // retVal := { version || chainingMode || symAlgKeySize || symAlgBlockSize || hmacAlgKeySize || hmacAlgDigestSize || E("") || MAC("") }.
        return retVal;
    }

    public void Dispose()
    {
        _sp800_108_ctr_hmac_provider.Dispose();

        // We don't want to dispose of the underlying algorithm instances because they
        // might be reused.
    }
}

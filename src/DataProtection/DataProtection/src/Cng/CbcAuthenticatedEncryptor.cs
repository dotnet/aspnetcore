// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.Cryptography.SafeHandles;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.Cng.Internal;
using Microsoft.AspNetCore.DataProtection.SP800_108;

namespace Microsoft.AspNetCore.DataProtection.Cng;

// An encryptor which does Encrypt(CBC) + HMAC using the Windows CNG (BCrypt*) APIs.
// The payloads produced by this encryptor should be compatible with the payloads
// produced by the managed Encrypt(CBC) + HMAC encryptor.
internal sealed unsafe class CbcAuthenticatedEncryptor : CngAuthenticatedEncryptorBase
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

    protected override byte[] DecryptImpl(byte* pbCiphertext, uint cbCiphertext, byte* pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData)
    {
        // Argument checking - input must at the absolute minimum contain a key modifier, IV, and MAC
        if (cbCiphertext < checked(KEY_MODIFIER_SIZE_IN_BYTES + _symmetricAlgorithmBlockSizeInBytes + _hmacAlgorithmDigestLengthInBytes))
        {
            throw Error.CryptCommon_PayloadInvalid();
        }

        // Assumption: pbCipherText := { keyModifier | IV | encryptedData | MAC(IV | encryptedPayload) }

        var cbEncryptedData = checked(cbCiphertext - (KEY_MODIFIER_SIZE_IN_BYTES + _symmetricAlgorithmBlockSizeInBytes + _hmacAlgorithmDigestLengthInBytes));

        // Calculate offsets
        byte* pbKeyModifier = pbCiphertext;
        byte* pbIV = &pbKeyModifier[KEY_MODIFIER_SIZE_IN_BYTES];
        byte* pbEncryptedData = &pbIV[_symmetricAlgorithmBlockSizeInBytes];
        byte* pbActualHmac = &pbEncryptedData[cbEncryptedData];

        // Use the KDF to recreate the symmetric encryption and HMAC subkeys
        // We'll need a temporary buffer to hold them
        var cbTempSubkeys = checked(_symmetricAlgorithmSubkeyLengthInBytes + _hmacAlgorithmSubkeyLengthInBytes);
        byte* pbTempSubkeys = stackalloc byte[checked((int)cbTempSubkeys)];
        try
        {
            _sp800_108_ctr_hmac_provider.DeriveKeyWithContextHeader(
                pbLabel: pbAdditionalAuthenticatedData,
                cbLabel: cbAdditionalAuthenticatedData,
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
                if (!ValidateHash(hashHandle, pbIV, _symmetricAlgorithmBlockSizeInBytes + cbEncryptedData, pbActualHmac))
                {
                    throw Error.CryptCommon_PayloadInvalid();
                }
            }

            // If the integrity check succeeded, decrypt the payload.
            using (var decryptionSubkeyHandle = _symmetricAlgorithmHandle.GenerateSymmetricKey(pbSymmetricEncryptionSubkey, _symmetricAlgorithmSubkeyLengthInBytes))
            {
                return DoCbcDecrypt(decryptionSubkeyHandle, pbIV, pbEncryptedData, cbEncryptedData);
            }
        }
        finally
        {
            // Buffer contains sensitive key material; delete.
            UnsafeBufferUtil.SecureZeroMemory(pbTempSubkeys, cbTempSubkeys);
        }
    }

    public override void Dispose()
    {
        _sp800_108_ctr_hmac_provider.Dispose();

        // We don't want to dispose of the underlying algorithm instances because they
        // might be reused.
    }

    // 'pbIV' must be a pointer to a buffer equal in length to the symmetric algorithm block size.
    private byte[] DoCbcDecrypt(BCryptKeyHandle symmetricKeyHandle, byte* pbIV, byte* pbInput, uint cbInput)
    {
        // BCryptDecrypt mutates the provided IV; we need to clone it to prevent mutation of the original value
        byte* pbClonedIV = stackalloc byte[checked((int)_symmetricAlgorithmBlockSizeInBytes)];
        UnsafeBufferUtil.BlockCopy(from: pbIV, to: pbClonedIV, byteCount: _symmetricAlgorithmBlockSizeInBytes);

        // First, figure out how large an output buffer we require.
        // Ideally we'd be able to transform the last block ourselves and strip
        // off the padding before creating the return value array, but we don't
        // know the actual padding scheme being used under the covers (we can't
        // assume PKCS#7). So unfortunately we're stuck with the temporary buffer.
        // (Querying the output size won't mutate the IV.)
        uint dwEstimatedDecryptedByteCount;
        var ntstatus = UnsafeNativeMethods.BCryptDecrypt(
            hKey: symmetricKeyHandle,
            pbInput: pbInput,
            cbInput: cbInput,
            pPaddingInfo: null,
            pbIV: pbClonedIV,
            cbIV: _symmetricAlgorithmBlockSizeInBytes,
            pbOutput: null,
            cbOutput: 0,
            pcbResult: out dwEstimatedDecryptedByteCount,
            dwFlags: BCryptEncryptFlags.BCRYPT_BLOCK_PADDING);
        UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);

        var decryptedPayload = new byte[dwEstimatedDecryptedByteCount];
        uint dwActualDecryptedByteCount;
        fixed (byte* pbDecryptedPayload = decryptedPayload)
        {
            byte dummy;

            // Perform the actual decryption.
            ntstatus = UnsafeNativeMethods.BCryptDecrypt(
                hKey: symmetricKeyHandle,
                pbInput: pbInput,
                cbInput: cbInput,
                pPaddingInfo: null,
                pbIV: pbClonedIV,
                cbIV: _symmetricAlgorithmBlockSizeInBytes,
                pbOutput: (pbDecryptedPayload != null) ? pbDecryptedPayload : &dummy, // CLR won't pin zero-length arrays
                cbOutput: dwEstimatedDecryptedByteCount,
                pcbResult: out dwActualDecryptedByteCount,
                dwFlags: BCryptEncryptFlags.BCRYPT_BLOCK_PADDING);
            UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);
        }

        // Decryption finished!
        CryptoUtil.Assert(dwActualDecryptedByteCount <= dwEstimatedDecryptedByteCount, "dwActualDecryptedByteCount <= dwEstimatedDecryptedByteCount");
        if (dwActualDecryptedByteCount == dwEstimatedDecryptedByteCount)
        {
            // payload takes up the entire buffer
            return decryptedPayload;
        }
        else
        {
            // payload takes up only a partial buffer
            var resizedDecryptedPayload = new byte[dwActualDecryptedByteCount];
            Buffer.BlockCopy(decryptedPayload, 0, resizedDecryptedPayload, 0, resizedDecryptedPayload.Length);
            return resizedDecryptedPayload;
        }
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

    protected override byte[] EncryptImpl(byte* pbPlaintext, uint cbPlaintext, byte* pbAdditionalAuthenticatedData, uint cbAdditionalAuthenticatedData, uint cbPreBuffer, uint cbPostBuffer)
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
            _sp800_108_ctr_hmac_provider.DeriveKeyWithContextHeader(
                pbLabel: pbAdditionalAuthenticatedData,
                cbLabel: cbAdditionalAuthenticatedData,
                contextHeader: _contextHeader,
                pbContext: pbKeyModifier,
                cbContext: KEY_MODIFIER_SIZE_IN_BYTES,
                pbDerivedKey: pbTempSubkeys,
                cbDerivedKey: cbTempSubkeys);

            // Calculate offsets
            byte* pbSymmetricEncryptionSubkey = pbTempSubkeys;
            byte* pbHmacSubkey = &pbTempSubkeys[_symmetricAlgorithmSubkeyLengthInBytes];

            using (var symmetricKeyHandle = _symmetricAlgorithmHandle.GenerateSymmetricKey(pbSymmetricEncryptionSubkey, _symmetricAlgorithmSubkeyLengthInBytes))
            {
                // We can't assume PKCS#7 padding (maybe the underlying provider is really using CTS),
                // so we need to query the padded output size before we can allocate the return value array.
                var cbOutputCiphertext = GetCbcEncryptedOutputSizeWithPadding(symmetricKeyHandle, pbPlaintext, cbPlaintext);

                // Allocate return value array and start copying some data
                var retVal = new byte[checked(cbPreBuffer + KEY_MODIFIER_SIZE_IN_BYTES + _symmetricAlgorithmBlockSizeInBytes + cbOutputCiphertext + _hmacAlgorithmDigestLengthInBytes + cbPostBuffer)];
                fixed (byte* pbRetVal = retVal)
                {
                    // Calculate offsets
                    byte* pbOutputKeyModifier = &pbRetVal[cbPreBuffer];
                    byte* pbOutputIV = &pbOutputKeyModifier[KEY_MODIFIER_SIZE_IN_BYTES];
                    byte* pbOutputCiphertext = &pbOutputIV[_symmetricAlgorithmBlockSizeInBytes];
                    byte* pbOutputHmac = &pbOutputCiphertext[cbOutputCiphertext];

                    UnsafeBufferUtil.BlockCopy(from: pbKeyModifierAndIV, to: pbOutputKeyModifier, byteCount: cbKeyModifierAndIV);

                    // retVal will eventually contain { preBuffer | keyModifier | iv | encryptedData | HMAC(iv | encryptedData) | postBuffer }
                    // At this point, retVal := { preBuffer | keyModifier | iv | _____ | _____ | postBuffer }

                    DoCbcEncrypt(
                        symmetricKeyHandle: symmetricKeyHandle,
                        pbIV: pbIV,
                        pbInput: pbPlaintext,
                        cbInput: cbPlaintext,
                        pbOutput: pbOutputCiphertext,
                        cbOutput: cbOutputCiphertext);

                    // At this point, retVal := { preBuffer | keyModifier | iv | encryptedData | _____ | postBuffer }

                    // Compute the HMAC over the IV and the ciphertext (prevents IV tampering).
                    // The HMAC is already implicitly computed over the key modifier since the key
                    // modifier is used as input to the KDF.
                    using (var hashHandle = _hmacAlgorithmHandle.CreateHmac(pbHmacSubkey, _hmacAlgorithmSubkeyLengthInBytes))
                    {
                        hashHandle.HashData(
                            pbInput: pbOutputIV,
                            cbInput: checked(_symmetricAlgorithmBlockSizeInBytes + cbOutputCiphertext),
                            pbHashDigest: pbOutputHmac,
                            cbHashDigest: _hmacAlgorithmDigestLengthInBytes);
                    }

                    // At this point, retVal := { preBuffer | keyModifier | iv | encryptedData | HMAC(iv | encryptedData) | postBuffer }
                    // And we're done!
                    return retVal;
                }
            }
        }
        finally
        {
            // Buffer contains sensitive material; delete it.
            UnsafeBufferUtil.SecureZeroMemory(pbTempSubkeys, cbTempSubkeys);
        }
    }

    private uint GetCbcEncryptedOutputSizeWithPadding(BCryptKeyHandle symmetricKeyHandle, byte* pbInput, uint cbInput)
    {
        // ok for this memory to remain uninitialized since nobody depends on it
        byte* pbIV = stackalloc byte[checked((int)_symmetricAlgorithmBlockSizeInBytes)];

        // Calling BCryptEncrypt with a null output pointer will cause it to return the total number
        // of bytes required for the output buffer.
        uint dwResult;
        var ntstatus = UnsafeNativeMethods.BCryptEncrypt(
            hKey: symmetricKeyHandle,
            pbInput: pbInput,
            cbInput: cbInput,
            pPaddingInfo: null,
            pbIV: pbIV,
            cbIV: _symmetricAlgorithmBlockSizeInBytes,
            pbOutput: null,
            cbOutput: 0,
            pcbResult: out dwResult,
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
}

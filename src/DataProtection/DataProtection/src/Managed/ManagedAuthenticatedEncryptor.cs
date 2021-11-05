// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.SP800_108;

namespace Microsoft.AspNetCore.DataProtection.Managed;

// An encryptor which does Encrypt(CBC) + HMAC using SymmetricAlgorithm and HashAlgorithm.
// The payloads produced by this encryptor should be compatible with the payloads
// produced by the CNG-based Encrypt(CBC) + HMAC authenticated encryptor.
internal sealed unsafe class ManagedAuthenticatedEncryptor : IAuthenticatedEncryptor, IDisposable
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
    private const int KEY_MODIFIER_SIZE_IN_BYTES = 128 / 8;

    private static readonly Func<byte[], HashAlgorithm> _kdkPrfFactory = key => new HMACSHA512(key); // currently hardcoded to SHA512

    private readonly byte[] _contextHeader;
    private readonly IManagedGenRandom _genRandom;
    private readonly Secret _keyDerivationKey;
    private readonly Func<SymmetricAlgorithm> _symmetricAlgorithmFactory;
    private readonly int _symmetricAlgorithmBlockSizeInBytes;
    private readonly int _symmetricAlgorithmSubkeyLengthInBytes;
    private readonly int _validationAlgorithmDigestLengthInBytes;
    private readonly int _validationAlgorithmSubkeyLengthInBytes;
    private readonly Func<KeyedHashAlgorithm> _validationAlgorithmFactory;

    public ManagedAuthenticatedEncryptor(Secret keyDerivationKey, Func<SymmetricAlgorithm> symmetricAlgorithmFactory, int symmetricAlgorithmKeySizeInBytes, Func<KeyedHashAlgorithm> validationAlgorithmFactory, IManagedGenRandom? genRandom = null)
    {
        _genRandom = genRandom ?? ManagedGenRandomImpl.Instance;
        _keyDerivationKey = keyDerivationKey;

        // Validate that the symmetric algorithm has the properties we require
        using (var symmetricAlgorithm = symmetricAlgorithmFactory())
        {
            _symmetricAlgorithmFactory = symmetricAlgorithmFactory;
            _symmetricAlgorithmBlockSizeInBytes = symmetricAlgorithm.GetBlockSizeInBytes();
            _symmetricAlgorithmSubkeyLengthInBytes = symmetricAlgorithmKeySizeInBytes;
        }

        // Validate that the MAC algorithm has the properties we require
        using (var validationAlgorithm = validationAlgorithmFactory())
        {
            _validationAlgorithmFactory = validationAlgorithmFactory;
            _validationAlgorithmDigestLengthInBytes = validationAlgorithm.GetDigestSizeInBytes();
            _validationAlgorithmSubkeyLengthInBytes = _validationAlgorithmDigestLengthInBytes; // for simplicity we'll generate MAC subkeys with a length equal to the digest length
        }

        // Argument checking on the algorithms and lengths passed in to us
        AlgorithmAssert.IsAllowableSymmetricAlgorithmBlockSize(checked((uint)_symmetricAlgorithmBlockSizeInBytes * 8));
        AlgorithmAssert.IsAllowableSymmetricAlgorithmKeySize(checked((uint)_symmetricAlgorithmSubkeyLengthInBytes * 8));
        AlgorithmAssert.IsAllowableValidationAlgorithmDigestSize(checked((uint)_validationAlgorithmDigestLengthInBytes * 8));

        _contextHeader = CreateContextHeader();
    }

    private byte[] CreateContextHeader()
    {
        var EMPTY_ARRAY = Array.Empty<byte>();
        var EMPTY_ARRAY_SEGMENT = new ArraySegment<byte>(EMPTY_ARRAY);

        var retVal = new byte[checked(
            1 /* KDF alg */
            + 1 /* chaining mode */
            + sizeof(uint) /* sym alg key size */
            + sizeof(uint) /* sym alg block size */
            + sizeof(uint) /* hmac alg key size */
            + sizeof(uint) /* hmac alg digest size */
            + _symmetricAlgorithmBlockSizeInBytes /* ciphertext of encrypted empty string */
            + _validationAlgorithmDigestLengthInBytes /* digest of HMACed empty string */)];

        var idx = 0;

        // First is the two-byte header
        retVal[idx++] = 0; // 0x00 = SP800-108 CTR KDF w/ HMACSHA512 PRF
        retVal[idx++] = 0; // 0x00 = CBC encryption + HMAC authentication

        // Next is information about the symmetric algorithm (key size followed by block size)
        BitHelpers.WriteTo(retVal, ref idx, _symmetricAlgorithmSubkeyLengthInBytes);
        BitHelpers.WriteTo(retVal, ref idx, _symmetricAlgorithmBlockSizeInBytes);

        // Next is information about the keyed hash algorithm (key size followed by digest size)
        BitHelpers.WriteTo(retVal, ref idx, _validationAlgorithmSubkeyLengthInBytes);
        BitHelpers.WriteTo(retVal, ref idx, _validationAlgorithmDigestLengthInBytes);

        // See the design document for an explanation of the following code.
        var tempKeys = new byte[_symmetricAlgorithmSubkeyLengthInBytes + _validationAlgorithmSubkeyLengthInBytes];
        ManagedSP800_108_CTR_HMACSHA512.DeriveKeys(
            kdk: EMPTY_ARRAY,
            label: EMPTY_ARRAY_SEGMENT,
            context: EMPTY_ARRAY_SEGMENT,
            prfFactory: _kdkPrfFactory,
            output: new ArraySegment<byte>(tempKeys));

        // At this point, tempKeys := { K_E || K_H }.

        // Encrypt a zero-length input string with an all-zero IV and copy the ciphertext to the return buffer.
        using (var symmetricAlg = CreateSymmetricAlgorithm())
        {
            using (var cryptoTransform = symmetricAlg.CreateEncryptor(
                rgbKey: new ArraySegment<byte>(tempKeys, 0, _symmetricAlgorithmSubkeyLengthInBytes).AsStandaloneArray(),
                rgbIV: new byte[_symmetricAlgorithmBlockSizeInBytes]))
            {
                var ciphertext = cryptoTransform.TransformFinalBlock(EMPTY_ARRAY, 0, 0);
                CryptoUtil.Assert(ciphertext != null && ciphertext.Length == _symmetricAlgorithmBlockSizeInBytes, "ciphertext != null && ciphertext.Length == _symmetricAlgorithmBlockSizeInBytes");
                Buffer.BlockCopy(ciphertext, 0, retVal, idx, ciphertext.Length);
            }
        }

        idx += _symmetricAlgorithmBlockSizeInBytes;

        // MAC a zero-length input string and copy the digest to the return buffer.
        using (var hashAlg = CreateValidationAlgorithm(new ArraySegment<byte>(tempKeys, _symmetricAlgorithmSubkeyLengthInBytes, _validationAlgorithmSubkeyLengthInBytes).AsStandaloneArray()))
        {
            var digest = hashAlg.ComputeHash(EMPTY_ARRAY);
            CryptoUtil.Assert(digest != null && digest.Length == _validationAlgorithmDigestLengthInBytes, "digest != null && digest.Length == _validationAlgorithmDigestLengthInBytes");
            Buffer.BlockCopy(digest, 0, retVal, idx, digest.Length);
        }

        idx += _validationAlgorithmDigestLengthInBytes;
        CryptoUtil.Assert(idx == retVal.Length, "idx == retVal.Length");

        // retVal := { version || chainingMode || symAlgKeySize || symAlgBlockSize || macAlgKeySize || macAlgDigestSize || E("") || MAC("") }.
        return retVal;
    }

    private SymmetricAlgorithm CreateSymmetricAlgorithm()
    {
        var retVal = _symmetricAlgorithmFactory();
        CryptoUtil.Assert(retVal != null, "retVal != null");

        retVal.Mode = CipherMode.CBC;
        retVal.Padding = PaddingMode.PKCS7;
        return retVal;
    }

    private KeyedHashAlgorithm CreateValidationAlgorithm(byte[] key)
    {
        var retVal = _validationAlgorithmFactory();
        CryptoUtil.Assert(retVal != null, "retVal != null");

        retVal.Key = key;
        return retVal;
    }

    public byte[] Decrypt(ArraySegment<byte> protectedPayload, ArraySegment<byte> additionalAuthenticatedData)
    {
        protectedPayload.Validate();
        additionalAuthenticatedData.Validate();

        // Argument checking - input must at the absolute minimum contain a key modifier, IV, and MAC
        if (protectedPayload.Count < checked(KEY_MODIFIER_SIZE_IN_BYTES + _symmetricAlgorithmBlockSizeInBytes + _validationAlgorithmDigestLengthInBytes))
        {
            throw Error.CryptCommon_PayloadInvalid();
        }

        // Assumption: protectedPayload := { keyModifier | IV | encryptedData | MAC(IV | encryptedPayload) }

        try
        {
            // Step 1: Extract the key modifier and IV from the payload.

            int keyModifierOffset; // position in protectedPayload.Array where key modifier begins
            int ivOffset; // position in protectedPayload.Array where key modifier ends / IV begins
            int ciphertextOffset; // position in protectedPayload.Array where IV ends / ciphertext begins
            int macOffset; // position in protectedPayload.Array where ciphertext ends / MAC begins
            int eofOffset; // position in protectedPayload.Array where MAC ends

            checked
            {
                keyModifierOffset = protectedPayload.Offset;
                ivOffset = keyModifierOffset + KEY_MODIFIER_SIZE_IN_BYTES;
                ciphertextOffset = ivOffset + _symmetricAlgorithmBlockSizeInBytes;
            }

            ArraySegment<byte> keyModifier = new ArraySegment<byte>(protectedPayload.Array!, keyModifierOffset, ivOffset - keyModifierOffset);
            var iv = new byte[_symmetricAlgorithmBlockSizeInBytes];
            Buffer.BlockCopy(protectedPayload.Array!, ivOffset, iv, 0, iv.Length);

            // Step 2: Decrypt the KDK and use it to restore the original encryption and MAC keys.
            // We pin all unencrypted keys to limit their exposure via GC relocation.

            var decryptedKdk = new byte[_keyDerivationKey.Length];
            var decryptionSubkey = new byte[_symmetricAlgorithmSubkeyLengthInBytes];
            var validationSubkey = new byte[_validationAlgorithmSubkeyLengthInBytes];
            var derivedKeysBuffer = new byte[checked(decryptionSubkey.Length + validationSubkey.Length)];

            fixed (byte* __unused__1 = decryptedKdk)
            fixed (byte* __unused__2 = decryptionSubkey)
            fixed (byte* __unused__3 = validationSubkey)
            fixed (byte* __unused__4 = derivedKeysBuffer)
            {
                try
                {
                    _keyDerivationKey.WriteSecretIntoBuffer(new ArraySegment<byte>(decryptedKdk));
                    ManagedSP800_108_CTR_HMACSHA512.DeriveKeysWithContextHeader(
                        kdk: decryptedKdk,
                        label: additionalAuthenticatedData,
                        contextHeader: _contextHeader,
                        context: keyModifier,
                        prfFactory: _kdkPrfFactory,
                        output: new ArraySegment<byte>(derivedKeysBuffer));

                    Buffer.BlockCopy(derivedKeysBuffer, 0, decryptionSubkey, 0, decryptionSubkey.Length);
                    Buffer.BlockCopy(derivedKeysBuffer, decryptionSubkey.Length, validationSubkey, 0, validationSubkey.Length);

                    // Step 3: Calculate the correct MAC for this payload.
                    // correctHash := MAC(IV || ciphertext)
                    byte[] correctHash;

                    using (var hashAlgorithm = CreateValidationAlgorithm(validationSubkey))
                    {
                        checked
                        {
                            eofOffset = protectedPayload.Offset + protectedPayload.Count;
                            macOffset = eofOffset - _validationAlgorithmDigestLengthInBytes;
                        }

                        correctHash = hashAlgorithm.ComputeHash(protectedPayload.Array!, ivOffset, macOffset - ivOffset);
                    }

                    // Step 4: Validate the MAC provided as part of the payload.

                    if (!CryptoUtil.TimeConstantBuffersAreEqual(correctHash, 0, correctHash.Length, protectedPayload.Array!, macOffset, eofOffset - macOffset))
                    {
                        throw Error.CryptCommon_PayloadInvalid(); // integrity check failure
                    }

                    // Step 5: Decipher the ciphertext and return it to the caller.

                    using (var symmetricAlgorithm = CreateSymmetricAlgorithm())
                    using (var cryptoTransform = symmetricAlgorithm.CreateDecryptor(decryptionSubkey, iv))
                    {
                        var outputStream = new MemoryStream();
                        using (var cryptoStream = new CryptoStream(outputStream, cryptoTransform, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(protectedPayload.Array!, ciphertextOffset, macOffset - ciphertextOffset);
                            cryptoStream.FlushFinalBlock();

                            // At this point, outputStream := { plaintext }, and we're done!
                            return outputStream.ToArray();
                        }
                    }
                }
                finally
                {
                    // delete since these contain secret material
                    Array.Clear(decryptedKdk, 0, decryptedKdk.Length);
                    Array.Clear(decryptionSubkey, 0, decryptionSubkey.Length);
                    Array.Clear(validationSubkey, 0, validationSubkey.Length);
                    Array.Clear(derivedKeysBuffer, 0, derivedKeysBuffer.Length);
                }
            }
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // Homogenize all exceptions to CryptographicException.
            throw Error.CryptCommon_GenericError(ex);
        }
    }

    public void Dispose()
    {
        _keyDerivationKey.Dispose();
    }

    public byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData)
    {
        plaintext.Validate();
        additionalAuthenticatedData.Validate();

        try
        {
            var outputStream = new MemoryStream();

            // Step 1: Generate a random key modifier and IV for this operation.
            // Both will be equal to the block size of the block cipher algorithm.

            var keyModifier = _genRandom.GenRandom(KEY_MODIFIER_SIZE_IN_BYTES);
            var iv = _genRandom.GenRandom(_symmetricAlgorithmBlockSizeInBytes);

            // Step 2: Copy the key modifier and the IV to the output stream since they'll act as a header.

            outputStream.Write(keyModifier, 0, keyModifier.Length);
            outputStream.Write(iv, 0, iv.Length);

            // At this point, outputStream := { keyModifier || IV }.

            // Step 3: Decrypt the KDK, and use it to generate new encryption and HMAC keys.
            // We pin all unencrypted keys to limit their exposure via GC relocation.

            var decryptedKdk = new byte[_keyDerivationKey.Length];
            var encryptionSubkey = new byte[_symmetricAlgorithmSubkeyLengthInBytes];
            var validationSubkey = new byte[_validationAlgorithmSubkeyLengthInBytes];
            var derivedKeysBuffer = new byte[checked(encryptionSubkey.Length + validationSubkey.Length)];

            fixed (byte* __unused__1 = decryptedKdk)
            fixed (byte* __unused__2 = encryptionSubkey)
            fixed (byte* __unused__3 = validationSubkey)
            fixed (byte* __unused__4 = derivedKeysBuffer)
            {
                try
                {
                    _keyDerivationKey.WriteSecretIntoBuffer(new ArraySegment<byte>(decryptedKdk));
                    ManagedSP800_108_CTR_HMACSHA512.DeriveKeysWithContextHeader(
                        kdk: decryptedKdk,
                        label: additionalAuthenticatedData,
                        contextHeader: _contextHeader,
                        context: new ArraySegment<byte>(keyModifier),
                        prfFactory: _kdkPrfFactory,
                        output: new ArraySegment<byte>(derivedKeysBuffer));

                    Buffer.BlockCopy(derivedKeysBuffer, 0, encryptionSubkey, 0, encryptionSubkey.Length);
                    Buffer.BlockCopy(derivedKeysBuffer, encryptionSubkey.Length, validationSubkey, 0, validationSubkey.Length);

                    // Step 4: Perform the encryption operation.

                    using (var symmetricAlgorithm = CreateSymmetricAlgorithm())
                    using (var cryptoTransform = symmetricAlgorithm.CreateEncryptor(encryptionSubkey, iv))
                    using (var cryptoStream = new CryptoStream(outputStream, cryptoTransform, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(plaintext.Array!, plaintext.Offset, plaintext.Count);
                        cryptoStream.FlushFinalBlock();

                        // At this point, outputStream := { keyModifier || IV || ciphertext }

                        // Step 5: Calculate the digest over the IV and ciphertext.
                        // We don't need to calculate the digest over the key modifier since that
                        // value has already been mixed into the KDF used to generate the MAC key.

                        using (var validationAlgorithm = CreateValidationAlgorithm(validationSubkey))
                        {
                            // As an optimization, avoid duplicating the underlying buffer
                            var underlyingBuffer = outputStream.GetBuffer();

                            var mac = validationAlgorithm.ComputeHash(underlyingBuffer, KEY_MODIFIER_SIZE_IN_BYTES, checked((int)outputStream.Length - KEY_MODIFIER_SIZE_IN_BYTES));
                            outputStream.Write(mac, 0, mac.Length);

                            // At this point, outputStream := { keyModifier || IV || ciphertext || MAC(IV || ciphertext) }
                            // And we're done!
                            return outputStream.ToArray();
                        }
                    }
                }
                finally
                {
                    // delete since these contain secret material
                    Array.Clear(decryptedKdk, 0, decryptedKdk.Length);
                    Array.Clear(encryptionSubkey, 0, encryptionSubkey.Length);
                    Array.Clear(validationSubkey, 0, validationSubkey.Length);
                    Array.Clear(derivedKeysBuffer, 0, derivedKeysBuffer.Length);
                }
            }
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // Homogenize all exceptions to CryptographicException.
            throw Error.CryptCommon_GenericError(ex);
        }
    }
}

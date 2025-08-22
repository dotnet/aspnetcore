// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.Internal;
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
            contextHeader: EMPTY_ARRAY_SEGMENT,
            contextData: EMPTY_ARRAY_SEGMENT,
            operationSubkey: tempKeys.AsSpan(0, _symmetricAlgorithmSubkeyLengthInBytes),
            validationSubkey: tempKeys.AsSpan(_symmetricAlgorithmSubkeyLengthInBytes, _validationAlgorithmSubkeyLengthInBytes));

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

    private KeyedHashAlgorithm CreateValidationAlgorithm(byte[]? key = null)
    {
        var retVal = _validationAlgorithmFactory();
        CryptoUtil.Assert(retVal != null, "retVal != null");

        if (key is not null)
        {
            retVal.Key = key;
        }
        return retVal;
    }

    public byte[] Decrypt(ArraySegment<byte> protectedPayload, ArraySegment<byte> additionalAuthenticatedData)
    {
        // Assumption: protectedPayload := { keyModifier | IV | encryptedData | MAC(IV | encryptedPayload) }
        protectedPayload.Validate();
        additionalAuthenticatedData.Validate();

        // Argument checking - input must at the absolute minimum contain a key modifier, IV, and MAC
        if (protectedPayload.Count < checked(KEY_MODIFIER_SIZE_IN_BYTES + _symmetricAlgorithmBlockSizeInBytes + _validationAlgorithmDigestLengthInBytes))
        {
            throw Error.CryptCommon_PayloadInvalid();
        }

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

            ReadOnlySpan<byte> keyModifier = protectedPayload.Array!.AsSpan(keyModifierOffset, ivOffset - keyModifierOffset);

            // Step 2: Decrypt the KDK and use it to restore the original encryption and MAC keys.
#if NET10_0_OR_GREATER
            Span<byte> decryptedKdk = _keyDerivationKey.Length <= 256
                ? stackalloc byte[256].Slice(0, _keyDerivationKey.Length)
                : new byte[_keyDerivationKey.Length];
#else
            var decryptedKdk = new byte[_keyDerivationKey.Length];
#endif

            byte[]? validationSubkeyArray = null;
            var validationSubkey = _validationAlgorithmSubkeyLengthInBytes <= 128
                ? stackalloc byte[128].Slice(0, _validationAlgorithmSubkeyLengthInBytes)
                : (validationSubkeyArray = new byte[_validationAlgorithmSubkeyLengthInBytes]);

#if NET10_0_OR_GREATER
            Span<byte> decryptionSubkey =
                _symmetricAlgorithmSubkeyLengthInBytes <= 128
                ? stackalloc byte[128].Slice(0, _symmetricAlgorithmSubkeyLengthInBytes)
                : new byte[_symmetricAlgorithmBlockSizeInBytes];
#else
            byte[] decryptionSubkey = new byte[_symmetricAlgorithmSubkeyLengthInBytes];
#endif

            // calling "fixed" is basically pinning the array, meaning the GC won't move it around. (Also for safety concerns)
            // note: it is safe to call `fixed` on null - it is just a no-op
            fixed (byte* decryptedKdkUnsafe = decryptedKdk)
            fixed (byte* __unused__2 = decryptionSubkey)
            fixed (byte* __unused__3 = validationSubkeyArray)
            {
                try
                {
                    _keyDerivationKey.WriteSecretIntoBuffer(decryptedKdkUnsafe, decryptedKdk.Length);
                    ManagedSP800_108_CTR_HMACSHA512.DeriveKeys(
                        kdk: decryptedKdk,
                        label: additionalAuthenticatedData,
                        contextHeader: _contextHeader,
                        contextData: keyModifier,
                        operationSubkey: decryptionSubkey,
                        validationSubkey: validationSubkey);

                    // Step 3: Calculate the correct MAC for this payload.
                    // correctHash := MAC(IV || ciphertext)
                    checked
                    {
                        eofOffset = protectedPayload.Offset + protectedPayload.Count;
                        macOffset = eofOffset - _validationAlgorithmDigestLengthInBytes;
                    }

                    // Step 4: Validate the MAC provided as part of the payload.
                    CalculateAndValidateMac(protectedPayload.Array!, ivOffset, macOffset, eofOffset, validationSubkey, validationSubkeyArray);

                    // Step 5: Decipher the ciphertext and return it to the caller.
#if NET10_0_OR_GREATER
                    using var symmetricAlgorithm = CreateSymmetricAlgorithm();
                    symmetricAlgorithm.SetKey(decryptionSubkey); // setKey is a single-shot usage of symmetricAlgorithm. Not allocatey

                    // note: here protectedPayload.Array is taken without an offset (can't use AsSpan() on ArraySegment)
                    var ciphertext = protectedPayload.Array.AsSpan(ciphertextOffset, macOffset - ciphertextOffset);
                    var iv = protectedPayload.Array.AsSpan(ivOffset, _symmetricAlgorithmBlockSizeInBytes);

                    // symmetricAlgorithm is created with CBC mode (see CreateSymmetricAlgorithm())
                    return symmetricAlgorithm.DecryptCbc(ciphertext, iv);
#else
                    var iv = protectedPayload.Array!.AsSpan(ivOffset, _symmetricAlgorithmBlockSizeInBytes).ToArray();

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
#endif
                }
                finally
                {
                    // delete since these contain secret material
                    validationSubkey.Clear();

#if NET10_0_OR_GREATER
                    decryptedKdk.Clear();
                    decryptionSubkey.Clear();
#else
                    Array.Clear(decryptedKdk, 0, decryptedKdk.Length);
#endif
                }
            }
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // Homogenize all exceptions to CryptographicException.
            throw Error.CryptCommon_GenericError(ex);
        }
    }

    public byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData)
    {
        plaintext.Validate();
        additionalAuthenticatedData.Validate();
        var plainTextSpan = plaintext.AsSpan();

        try
        {
            var keyModifierLength = KEY_MODIFIER_SIZE_IN_BYTES;
            var ivLength = _symmetricAlgorithmBlockSizeInBytes;

#if NET10_0_OR_GREATER
            Span<byte> decryptedKdk = _keyDerivationKey.Length <= 256
                ? stackalloc byte[256].Slice(0, _keyDerivationKey.Length)
                : new byte[_keyDerivationKey.Length];
#else
            var decryptedKdk = new byte[_keyDerivationKey.Length];
#endif

#if NET10_0_OR_GREATER
            byte[]? validationSubkeyArray = null;
            Span<byte> validationSubkey = _validationAlgorithmSubkeyLengthInBytes <= 128
                ? stackalloc byte[128].Slice(0, _validationAlgorithmSubkeyLengthInBytes)
                : (validationSubkeyArray = new byte[_validationAlgorithmSubkeyLengthInBytes]);
#else
            var validationSubkeyArray = new byte[_validationAlgorithmSubkeyLengthInBytes];
            var validationSubkey = validationSubkeyArray.AsSpan();
#endif

#if NET10_0_OR_GREATER
            Span<byte> encryptionSubkey = _symmetricAlgorithmSubkeyLengthInBytes <= 128
                ? stackalloc byte[128].Slice(0, _symmetricAlgorithmSubkeyLengthInBytes)
                : new byte[_symmetricAlgorithmSubkeyLengthInBytes];
#else
            byte[] encryptionSubkey = new byte[_symmetricAlgorithmSubkeyLengthInBytes];
#endif

            fixed (byte* decryptedKdkUnsafe = decryptedKdk)
            fixed (byte* __unused__1 = encryptionSubkey)
            fixed (byte* __unused__2 = validationSubkeyArray)
            {
                // Step 1: Generate a random key modifier and IV for this operation.
                // Both will be equal to the block size of the block cipher algorithm.
#if NET10_0_OR_GREATER
                Span<byte> keyModifier = keyModifierLength <= 128
                    ? stackalloc byte[128].Slice(0, keyModifierLength)
                    : new byte[keyModifierLength];

                _genRandom.GenRandom(keyModifier);
#else
                var keyModifier = _genRandom.GenRandom(keyModifierLength);
#endif

                try
                {
                    // Step 2: Decrypt the KDK, and use it to generate new encryption and HMAC keys.
                    _keyDerivationKey.WriteSecretIntoBuffer(decryptedKdkUnsafe, decryptedKdk.Length);
                    ManagedSP800_108_CTR_HMACSHA512.DeriveKeys(
                        kdk: decryptedKdk,
                        label: additionalAuthenticatedData,
                        contextHeader: _contextHeader,
                        contextData: keyModifier,
                        operationSubkey: encryptionSubkey,
                        validationSubkey: validationSubkey);

#if NET10_0_OR_GREATER
                    // idea of optimization here is firstly get all the types preset
                    // for calculating length of the output array and allocating it.
                    // then we are filling it with the data directly, without any additional copying

                    using var symmetricAlgorithm = CreateSymmetricAlgorithm();
                    symmetricAlgorithm.SetKey(encryptionSubkey); // setKey is a single-shot usage of symmetricAlgorithm. Not allocatey

                    using var validationAlgorithm = CreateValidationAlgorithm();

                    // Later framework has an API to pre-calculate optimal length of the ciphertext.
                    // That means we can avoid allocating more data than we need.

                    var cipherTextLength = symmetricAlgorithm.GetCiphertextLengthCbc(plainTextSpan.Length); // CBC because symmetricAlgorithm is created with CBC mode
                    var macLength = _validationAlgorithmDigestLengthInBytes;

                    // allocating an array of a specific required length
                    var outputArray = new byte[keyModifierLength + ivLength + cipherTextLength + macLength];
                    var outputSpan = outputArray.AsSpan();
#else
                    var outputStream = new MemoryStream();
#endif

#if NET10_0_OR_GREATER
                    // Step 2: Copy the key modifier to the output stream (part of a header)
                    keyModifier.CopyTo(outputSpan.Slice(start: 0, length: keyModifierLength));

                    // Step 3: Generate IV for this operation right into the output stream (no allocation)
                    // key modifier and IV together act as a header.
                    var iv = outputSpan.Slice(start: keyModifierLength, length: ivLength);
                    _genRandom.GenRandom(iv);
#else
                    // Step 2: Copy the key modifier and the IV to the output stream since they'll act as a header.
                    outputStream.Write(keyModifier, 0, keyModifier.Length);

                    // Step 3: Generate IV for this operation right into the result array
                    var iv = _genRandom.GenRandom(_symmetricAlgorithmBlockSizeInBytes);
                    outputStream.Write(iv, 0, iv.Length);
#endif

#if NET10_0_OR_GREATER
                    // Step 4: Perform the encryption operation.
                    // encrypting plaintext into the target array directly
                    symmetricAlgorithm.EncryptCbc(plainTextSpan, iv, outputSpan.Slice(start: keyModifierLength + ivLength, length: cipherTextLength));

                    // At this point, outputStream := { keyModifier || IV || ciphertext }

                    // Step 5: Calculate the digest over the IV and ciphertext.
                    // We don't need to calculate the digest over the key modifier since that
                    // value has already been mixed into the KDF used to generate the MAC key.

                    var ivAndCipherTextSpan = outputSpan.Slice(start: keyModifierLength, length: ivLength + cipherTextLength);
                    var macDestinationSpan = outputSpan.Slice(keyModifierLength + ivLength + cipherTextLength, macLength);

                    // if we can use an optimized method for specific algorithm - we use it (no extra alloc for subKey)
                    if (validationAlgorithm is HMACSHA256)
                    {
                        HMACSHA256.HashData(key: validationSubkey, source: ivAndCipherTextSpan, destination: macDestinationSpan);
                    }
                    else if (validationAlgorithm is HMACSHA512)
                    {
                        HMACSHA512.HashData(key: validationSubkey, source: ivAndCipherTextSpan, destination: macDestinationSpan);
                    }
                    else
                    {
                        validationAlgorithm.Key = validationSubkeyArray ?? validationSubkey.ToArray();
                        validationAlgorithm.TryComputeHash(source: ivAndCipherTextSpan, destination: macDestinationSpan, bytesWritten: out _);
                    }

                    // At this point, outputArray := { keyModifier || IV || ciphertext || MAC(IV || ciphertext) }
                    return outputArray;
#else
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
                        using (var validationAlgorithm = CreateValidationAlgorithm(validationSubkeyArray))
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
#endif
                }
                finally
                {
#if NET10_0_OR_GREATER
                    keyModifier.Clear();
                    decryptedKdk.Clear();
#else
                    Array.Clear(keyModifier, 0, keyModifierLength);
                    Array.Clear(decryptedKdk, 0, decryptedKdk.Length);
#endif
                }
            }
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // Homogenize all exceptions to CryptographicException.
            throw Error.CryptCommon_GenericError(ex);
        }
    }

    private void CalculateAndValidateMac(
        byte[] payloadArray,
        int ivOffset, int macOffset, int eofOffset, // offsets to slice the payload array
        ReadOnlySpan<byte> validationSubkey,
        byte[]? validationSubkeyArray)
    {
        using var validationAlgorithm = CreateValidationAlgorithm();
        var hashSize = validationAlgorithm.GetDigestSizeInBytes();

        byte[]? correctHashArray = null;
        Span<byte> correctHash = hashSize <= 128
            ? stackalloc byte[128].Slice(0, hashSize)
            : (correctHashArray = new byte[hashSize]);

        try
        {
#if NET10_0_OR_GREATER
            var hashSource = payloadArray!.AsSpan(ivOffset, macOffset - ivOffset);

            int bytesWritten;
            if (validationAlgorithm is HMACSHA256)
            {
                bytesWritten = HMACSHA256.HashData(key: validationSubkey, source: hashSource, destination: correctHash);
            }
            else if (validationAlgorithm is HMACSHA512)
            {
                bytesWritten = HMACSHA512.HashData(key: validationSubkey, source: hashSource, destination: correctHash);
            }
            else
            {
                // if validationSubkey is stackalloc'ed, there is no way we avoid an alloc here
                validationAlgorithm.Key = validationSubkeyArray ?? validationSubkey.ToArray();
                var success = validationAlgorithm.TryComputeHash(hashSource, correctHash, out bytesWritten);
                Debug.Assert(success);
            }

            Debug.Assert(bytesWritten == hashSize);
#else
            // if validationSubkey is stackalloc'ed, there is no way we avoid an alloc here
            validationAlgorithm.Key = validationSubkeyArray ?? validationSubkey.ToArray();
            correctHashArray = validationAlgorithm.ComputeHash(payloadArray, macOffset, eofOffset - macOffset);
#endif

            // Step 4: Validate the MAC provided as part of the payload.
            var payloadMacSpan = payloadArray!.AsSpan(macOffset, eofOffset - macOffset);
            if (!CryptoUtil.TimeConstantBuffersAreEqual(correctHash, payloadMacSpan))
            {
                throw Error.CryptCommon_PayloadInvalid(); // integrity check failure
            }
        }
        finally
        {
            correctHash.Clear();
        }
    }

    public void Dispose()
    {
        _keyDerivationKey.Dispose();
    }
}

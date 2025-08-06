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
#if NET10_0_OR_GREATER
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

#if NET10_0_OR_GREATER
    public int GetDecryptedSize(int cipherTextLength)
    {
        // Argument checking - input must at the absolute minimum contain a key modifier, IV, and MAC
        if (cipherTextLength < checked(KEY_MODIFIER_SIZE_IN_BYTES + _symmetricAlgorithmBlockSizeInBytes + _validationAlgorithmDigestLengthInBytes))
        {
            throw Error.CryptCommon_PayloadInvalid();
        }

        // For CBC mode with padding, the decrypted size is at most the encrypted data size
        // We return an over-estimation since we can't know the exact padding without decrypting
        return checked(cipherTextLength - (KEY_MODIFIER_SIZE_IN_BYTES + _symmetricAlgorithmBlockSizeInBytes + _validationAlgorithmDigestLengthInBytes));
    }

    public bool TryDecrypt(ReadOnlySpan<byte> cipherText, ReadOnlySpan<byte> additionalAuthenticatedData, Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;

        try
        {
            // Argument checking - input must at the absolute minimum contain a key modifier, IV, and MAC
            if (cipherText.Length < checked(KEY_MODIFIER_SIZE_IN_BYTES + _symmetricAlgorithmBlockSizeInBytes + _validationAlgorithmDigestLengthInBytes))
            {
                throw Error.CryptCommon_PayloadInvalid();
            }

            // Calculate the maximum possible plaintext size and check destination buffer
            var estimatedDecryptedSize = GetDecryptedSize(cipherText.Length);
            if (destination.Length < estimatedDecryptedSize)
            {
                return false;
            }

            // Calculate offsets in the cipherText
            var keyModifierOffset = 0;
            var ivOffset = keyModifierOffset + KEY_MODIFIER_SIZE_IN_BYTES;
            var ciphertextOffset = ivOffset + _symmetricAlgorithmBlockSizeInBytes;
            var macOffset = cipherText.Length - _validationAlgorithmDigestLengthInBytes;

            // Extract spans for each component
            var keyModifier = cipherText.Slice(keyModifierOffset, KEY_MODIFIER_SIZE_IN_BYTES);

            // Decrypt the KDK and use it to restore the original encryption and MAC keys
            Span<byte> decryptedKdk = _keyDerivationKey.Length <= 256
                ? stackalloc byte[256].Slice(0, _keyDerivationKey.Length)
                : new byte[_keyDerivationKey.Length];

            byte[]? validationSubkeyArray = null;
            var validationSubkey = _validationAlgorithmSubkeyLengthInBytes <= 128
                ? stackalloc byte[128].Slice(0, _validationAlgorithmSubkeyLengthInBytes)
                : (validationSubkeyArray = new byte[_validationAlgorithmSubkeyLengthInBytes]);

            Span<byte> decryptionSubkey = _symmetricAlgorithmSubkeyLengthInBytes <= 128
                ? stackalloc byte[128].Slice(0, _symmetricAlgorithmSubkeyLengthInBytes)
                : new byte[_symmetricAlgorithmSubkeyLengthInBytes];

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

                    // Validate the MAC provided as part of the payload
                    var ivAndCiphertextSpan = cipherText.Slice(ivOffset, macOffset - ivOffset);
                    var providedMac = cipherText.Slice(macOffset, _validationAlgorithmDigestLengthInBytes);

                    if (!ValidateMac(ivAndCiphertextSpan, providedMac, validationSubkey, validationSubkeyArray))
                    {
                        throw Error.CryptCommon_PayloadInvalid();
                    }

                    // If the integrity check succeeded, decrypt the payload directly into destination
                    var ciphertextSpan = cipherText.Slice(ciphertextOffset, macOffset - ciphertextOffset);
                    var iv = cipherText.Slice(ivOffset, _symmetricAlgorithmBlockSizeInBytes);


                    using var symmetricAlgorithm = CreateSymmetricAlgorithm();
                    symmetricAlgorithm.SetKey(decryptionSubkey);

                    // Decrypt directly into destination
                    var actualDecryptedBytes = symmetricAlgorithm.DecryptCbc(ciphertextSpan, iv, destination);
                    bytesWritten = actualDecryptedBytes;
                    return true;
                }
                finally
                {
                    // delete since these contain secret material
                    validationSubkey.Clear();

                    decryptedKdk.Clear();
                    decryptionSubkey.Clear();
                }
            }
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // Homogenize all exceptions to CryptographicException.
            throw Error.CryptCommon_GenericError(ex);
        }
    }

    public int GetEncryptedSize(int plainTextLength)
    {
        var symmetricAlgorithm = CreateSymmetricAlgorithm();
        var cipherTextLength = symmetricAlgorithm.GetCiphertextLengthCbc(plainTextLength);
        return KEY_MODIFIER_SIZE_IN_BYTES + _symmetricAlgorithmBlockSizeInBytes /* IV */ + cipherTextLength + _validationAlgorithmDigestLengthInBytes /* MAC */;
    }

    public bool TryEncrypt(ReadOnlySpan<byte> plainText, ReadOnlySpan<byte> additionalAuthenticatedData, Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;

        try
        {
            var keyModifierLength = KEY_MODIFIER_SIZE_IN_BYTES;
            var ivLength = _symmetricAlgorithmBlockSizeInBytes;

            Span<byte> decryptedKdk = _keyDerivationKey.Length <= 256
                ? stackalloc byte[256].Slice(0, _keyDerivationKey.Length)
                : new byte[_keyDerivationKey.Length];

            byte[]? validationSubkeyArray = null;
            Span<byte> validationSubkey = _validationAlgorithmSubkeyLengthInBytes <= 128
                ? stackalloc byte[128].Slice(0, _validationAlgorithmSubkeyLengthInBytes)
                : (validationSubkeyArray = new byte[_validationAlgorithmSubkeyLengthInBytes]);

            Span<byte> encryptionSubkey = _symmetricAlgorithmSubkeyLengthInBytes <= 128
                ? stackalloc byte[128].Slice(0, _symmetricAlgorithmSubkeyLengthInBytes)
                : new byte[_symmetricAlgorithmSubkeyLengthInBytes];

            fixed (byte* decryptedKdkUnsafe = decryptedKdk)
            fixed (byte* __unused__1 = encryptionSubkey)
            fixed (byte* __unused__2 = validationSubkeyArray)
            {
                // Step 1: Generate a random key modifier and IV for this operation.
                Span<byte> keyModifier = keyModifierLength <= 128
                    ? stackalloc byte[128].Slice(0, keyModifierLength)
                    : new byte[keyModifierLength];

                _genRandom.GenRandom(keyModifier);

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

                    using var symmetricAlgorithm = CreateSymmetricAlgorithm();
                    symmetricAlgorithm.SetKey(encryptionSubkey);

                    using var validationAlgorithm = CreateValidationAlgorithm();

                    // Calculate ciphertext length for CBC mode
                    var cipherTextLength = symmetricAlgorithm.GetCiphertextLengthCbc(plainText.Length);
                    var macLength = _validationAlgorithmDigestLengthInBytes;

                    // Step 3: Copy the key modifier to the destination
                    keyModifier.CopyTo(destination.Slice(bytesWritten, keyModifierLength));
                    bytesWritten += keyModifierLength;

                    // Step 4: Generate IV directly into the destination
                    var iv = destination.Slice(bytesWritten, ivLength);
                    _genRandom.GenRandom(iv);
                    bytesWritten += ivLength;

                    // Step 5: Perform the encryption operation
                    var ciphertextDestination = destination.Slice(bytesWritten, cipherTextLength);
                    symmetricAlgorithm.EncryptCbc(plainText, iv, ciphertextDestination);
                    bytesWritten += cipherTextLength;

                    // Step 6: Calculate the digest over the IV and ciphertext
                    var ivAndCipherTextSpan = destination.Slice(keyModifierLength, ivLength + cipherTextLength);
                    var macDestinationSpan = destination.Slice(bytesWritten, macLength);

                    // Use optimized method for specific algorithms when possible
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
                        if (!validationAlgorithm.TryComputeHash(source: ivAndCipherTextSpan, destination: macDestinationSpan, out _))
                        {
                            return false;
                        }
                    }
                    bytesWritten += macLength;

                    return true;
                }
                finally
                {
                    keyModifier.Clear();
                    decryptedKdk.Clear();
                }
            }
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // Homogenize all exceptions to CryptographicException.
            throw Error.CryptCommon_GenericError(ex);
        }
    }
#endif

    public byte[] Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData)
    {
        plaintext.Validate();
        additionalAuthenticatedData.Validate();
        var plainTextSpan = plaintext.AsSpan();

#if NET10_0_OR_GREATER
        var size = GetEncryptedSize(plainTextSpan.Length);
        var retVal = new byte[size];

        if (!TryEncrypt(plainTextSpan, additionalAuthenticatedData, retVal, out var bytesWritten))
        {
            throw Error.CryptCommon_GenericError(new ArgumentException("Not enough space in destination array."));
        }

        return retVal;
#else
        try
        {
            var keyModifierLength = KEY_MODIFIER_SIZE_IN_BYTES;
            var ivLength = _symmetricAlgorithmBlockSizeInBytes;

            var decryptedKdk = new byte[_keyDerivationKey.Length];
            var validationSubkeyArray = new byte[_validationAlgorithmSubkeyLengthInBytes];
            var validationSubkey = validationSubkeyArray.AsSpan();
            byte[] encryptionSubkey = new byte[_symmetricAlgorithmSubkeyLengthInBytes];

            fixed (byte* decryptedKdkUnsafe = decryptedKdk)
            fixed (byte* __unused__1 = encryptionSubkey)
            fixed (byte* __unused__2 = validationSubkeyArray)
            {
                // Step 1: Generate a random key modifier and IV for this operation.
                // Both will be equal to the block size of the block cipher algorithm.
                var keyModifier = _genRandom.GenRandom(keyModifierLength);

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

                    var outputStream = new MemoryStream();

                    // Step 2: Copy the key modifier and the IV to the output stream since they'll act as a header.
                    outputStream.Write(keyModifier, 0, keyModifier.Length);

                    // Step 3: Generate IV for this operation right into the result array
                    var iv = _genRandom.GenRandom(_symmetricAlgorithmBlockSizeInBytes);
                    outputStream.Write(iv, 0, iv.Length);

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
                }
                finally
                {
                    Array.Clear(keyModifier, 0, keyModifierLength);
                    Array.Clear(decryptedKdk, 0, decryptedKdk.Length);
                }
            }
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // Homogenize all exceptions to CryptographicException.
            throw Error.CryptCommon_GenericError(ex);
        }
#endif
    }

#if NET10_0_OR_GREATER
    private bool ValidateMac(ReadOnlySpan<byte> dataToValidate, ReadOnlySpan<byte> providedMac, ReadOnlySpan<byte> validationSubkey, byte[]? validationSubkeyArray)
    {
        using var validationAlgorithm = CreateValidationAlgorithm();
        var hashSize = validationAlgorithm.GetDigestSizeInBytes();

        byte[]? correctHashArray = null;
        Span<byte> correctHash = hashSize <= 128
            ? stackalloc byte[128].Slice(0, hashSize)
            : (correctHashArray = new byte[hashSize]);

        try
        {
            int bytesWritten;
            if (validationAlgorithm is HMACSHA256)
            {
                bytesWritten = HMACSHA256.HashData(key: validationSubkey, source: dataToValidate, destination: correctHash);
            }
            else if (validationAlgorithm is HMACSHA512)
            {
                bytesWritten = HMACSHA512.HashData(key: validationSubkey, source: dataToValidate, destination: correctHash);
            }
            else
            {
                // if validationSubkey is stackalloc'ed, there is no way we avoid an alloc here
                validationAlgorithm.Key = validationSubkeyArray ?? validationSubkey.ToArray();
                var success = validationAlgorithm.TryComputeHash(dataToValidate, correctHash, out bytesWritten);
                Debug.Assert(success);
            }
            Debug.Assert(bytesWritten == hashSize);

            return CryptoUtil.TimeConstantBuffersAreEqual(correctHash, providedMac);
        }
        finally
        {
            correctHash.Clear();
        }
    }
#else
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
            // if validationSubkey is stackalloc'ed, there is no way we avoid an alloc here
            validationAlgorithm.Key = validationSubkeyArray ?? validationSubkey.ToArray();
            correctHashArray = validationAlgorithm.ComputeHash(payloadArray, macOffset, eofOffset - macOffset);

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
#endif

    public byte[] Decrypt(ArraySegment<byte> protectedPayload, ArraySegment<byte> additionalAuthenticatedData)
    {
        // Assumption: protectedPayload := { keyModifier | IV | encryptedData | MAC(IV | encryptedPayload) }
        protectedPayload.Validate();
        additionalAuthenticatedData.Validate();

#if NET10_0_OR_GREATER
        var size = GetDecryptedSize(protectedPayload.Count);
        var rentedArray = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            var destination = rentedArray.AsSpan(0, size);

            if (!TryDecrypt(
                cipherText: protectedPayload,
                additionalAuthenticatedData: additionalAuthenticatedData,
                destination: destination,
                out var bytesWritten))
            {
                throw Error.CryptCommon_GenericError(new ArgumentException("Not enough space in destination array"));
            }

            // we don't know the exact size of the decrypted data beforehand,
            // so we firstly use rented array and then allocate with the exact size
            var result = destination.Slice(0, bytesWritten).ToArray();
            return result;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedArray);
        }
#else
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
            var decryptedKdk = new byte[_keyDerivationKey.Length];

            byte[]? validationSubkeyArray = null;
            var validationSubkey = _validationAlgorithmSubkeyLengthInBytes <= 128
                ? stackalloc byte[128].Slice(0, _validationAlgorithmSubkeyLengthInBytes)
                : (validationSubkeyArray = new byte[_validationAlgorithmSubkeyLengthInBytes]);

            byte[] decryptionSubkey = new byte[_symmetricAlgorithmSubkeyLengthInBytes];
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
                }
                finally
                {
                    // delete since these contain secret material
                    validationSubkey.Clear();
                    Array.Clear(decryptedKdk, 0, decryptedKdk.Length);
                }
            }
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // Homogenize all exceptions to CryptographicException.
            throw Error.CryptCommon_GenericError(ex);
        }
#endif
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

    public void Dispose()
    {
        _keyDerivationKey.Dispose();
    }
}

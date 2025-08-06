// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

internal unsafe class KeyRingBasedSpanDataProtector : KeyRingBasedDataProtector, ISpanDataProtector, IPersistedDataProtector
{
    public KeyRingBasedSpanDataProtector(IKeyRingProvider keyRingProvider, ILogger? logger, string[]? originalPurposes, string newPurpose)
        : base(keyRingProvider, logger, originalPurposes, newPurpose)
    {
    }

    public int GetProtectedSize(int plainTextLength)
    {
        // Get the current key ring to access the encryptor
        var currentKeyRing = _keyRingProvider.GetCurrentKeyRing();
        var defaultEncryptor = (ISpanAuthenticatedEncryptor)currentKeyRing.DefaultAuthenticatedEncryptor!;
        CryptoUtil.Assert(defaultEncryptor != null, "DefaultAuthenticatedEncryptor != null");

        // We allocate a 20-byte pre-buffer so that we can inject the magic header and key id into the return value.
        // See Protect() / TryProtect() for details
        return _magicHeaderKeyIdSize + defaultEncryptor.GetEncryptedSize(plainTextLength);
    }

    public bool TryProtect(ReadOnlySpan<byte> plaintext, Span<byte> destination, out int bytesWritten)
    {
        try
        {
            // Perform the encryption operation using the current default encryptor.
            var currentKeyRing = _keyRingProvider.GetCurrentKeyRing();
            var defaultKeyId = currentKeyRing.DefaultKeyId;
            var defaultEncryptor = (ISpanAuthenticatedEncryptor)currentKeyRing.DefaultAuthenticatedEncryptor!;
            CryptoUtil.Assert(defaultEncryptor != null, "DefaultAuthenticatedEncryptor != null");

            if (_logger.IsDebugLevelEnabled())
            {
                _logger.PerformingProtectOperationToKeyWithPurposes(defaultKeyId, JoinPurposesForLog(Purposes));
            }

            // We'll need to apply the default key id to the template if it hasn't already been applied.
            // If the default key id has been updated since the last call to Protect, also write back the updated template.
            var aad = _aadTemplate.GetAadForKey(defaultKeyId, isProtecting: true);

            var preBufferSize = _magicHeaderKeyIdSize;
            var postBufferSize = 0;
            var destinationBufferOffsets = destination.Slice(preBufferSize, destination.Length - (preBufferSize + postBufferSize));
            var success = defaultEncryptor.TryEncrypt(plaintext, aad, destinationBufferOffsets, out bytesWritten);

            // At this point: destination := { 000..000 || encryptorSpecificProtectedPayload },
            // where 000..000 is a placeholder for our magic header and key id.

            // Write out the magic header and key id
#if NET10_0_OR_GREATER
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(0, sizeof(uint)), MAGIC_HEADER_V0);
            var writeKeyIdResult = defaultKeyId.TryWriteBytes(destination.Slice(sizeof(uint), sizeof(Guid)));
            Debug.Assert(writeKeyIdResult, "Failed to write Guid to destination.");
#else
            fixed (byte* pbRetVal = destination)
            {
                WriteBigEndianInteger(pbRetVal, MAGIC_HEADER_V0);
                WriteGuid(&pbRetVal[sizeof(uint)], defaultKeyId);
            }
#endif

            bytesWritten += _magicHeaderKeyIdSize;

            // At this point, destination := { magicHeader || keyId || encryptorSpecificProtectedPayload }
            // And we're done!
            return success;
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // homogenize all errors to CryptographicException
            throw Error.Common_EncryptionFailed(ex);
        }
    }

    public int GetUnprotectedSize(int cipherTextLength)
    {
        // The ciphertext includes the magic header and key id, so we need to subtract those
        if (cipherTextLength < _magicHeaderKeyIdSize)
        {
            throw Error.ProtectionProvider_BadMagicHeader();
        }

        var currentKeyRing = _keyRingProvider.GetCurrentKeyRing();
        var defaultEncryptor = (ISpanAuthenticatedEncryptor)currentKeyRing.DefaultAuthenticatedEncryptor!;
        CryptoUtil.Assert(defaultEncryptor != null, "DefaultAuthenticatedEncryptor != null");

        return defaultEncryptor.GetDecryptedSize(cipherTextLength - _magicHeaderKeyIdSize);
    }

    public bool TryUnprotect(ReadOnlySpan<byte> cipherText, Span<byte> destination, out int bytesWritten)
    {
        try
        {
            if (cipherText.Length < _magicHeaderKeyIdSize)
            {
                // payload must contain at least the magic header and key id
                throw Error.ProtectionProvider_BadMagicHeader();
            }

            // Parse the payload version number and key id.
            var magicHeaderFromPayload = BinaryPrimitives.ReadUInt32BigEndian(cipherText.Slice(0, sizeof(uint)));
#if NET10_0_OR_GREATER
            var keyIdFromPayload = new Guid(cipherText.Slice(sizeof(uint), sizeof(Guid)));
#else
            Guid keyIdFromPayload;
            fixed (byte* pbCipherText = cipherText)
            {
                keyIdFromPayload = ReadGuid(&pbCipherText[sizeof(uint)]);
            }
#endif

            // Are the magic header and version information correct?
            if (!TryGetVersionFromMagicHeader(magicHeaderFromPayload, out var payloadVersion))
            {
                throw Error.ProtectionProvider_BadMagicHeader();
            }
            else if (payloadVersion != 0)
            {
                throw Error.ProtectionProvider_BadVersion();
            }

            if (_logger.IsDebugLevelEnabled())
            {
                _logger.PerformingUnprotectOperationToKeyWithPurposes(keyIdFromPayload, JoinPurposesForLog(Purposes));
            }

            // Find the correct encryptor in the keyring.
            var currentKeyRing = _keyRingProvider.GetCurrentKeyRing();
            var requestedEncryptor = currentKeyRing.GetAuthenticatedEncryptorByKeyId(keyIdFromPayload, out bool keyWasRevoked);
            if (requestedEncryptor is null)
            {
                if (_keyRingProvider is KeyRingProvider provider && provider.InAutoRefreshWindow())
                {
                    currentKeyRing = provider.RefreshCurrentKeyRing();
                    requestedEncryptor = currentKeyRing.GetAuthenticatedEncryptorByKeyId(keyIdFromPayload, out keyWasRevoked);
                }

                if (requestedEncryptor is null)
                {
                    if (_logger.IsTraceLevelEnabled())
                    {
                        _logger.KeyWasNotFoundInTheKeyRingUnprotectOperationCannotProceed(keyIdFromPayload);
                    }
                    bytesWritten = 0;
                    return false;
                }
            }

            // Check if key was revoked - for simplified version, we disallow revoked keys
            if (keyWasRevoked)
            {
                if (_logger.IsDebugLevelEnabled())
                {
                    _logger.KeyWasRevokedUnprotectOperationCannotProceed(keyIdFromPayload);
                }
                bytesWritten = 0;
                return false;
            }

            // Perform the decryption operation.
            ReadOnlySpan<byte> actualCiphertext = cipherText.Slice(sizeof(uint) + sizeof(Guid)); // chop off magic header + encryptor id
            ReadOnlySpan<byte> aad = _aadTemplate.GetAadForKey(keyIdFromPayload, isProtecting: false);

            // At this point, actualCiphertext := { encryptorSpecificPayload },
            // so all that's left is to invoke the decryption routine directly.
            var spanEncryptor = (ISpanAuthenticatedEncryptor)requestedEncryptor;
            return spanEncryptor.TryDecrypt(actualCiphertext, aad, destination, out bytesWritten);

        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // homogenize all errors to CryptographicException
            throw Error.DecryptionFailed(ex);
        }
    }
}

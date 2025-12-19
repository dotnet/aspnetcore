// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

internal unsafe class KeyRingBasedSpanDataProtector : KeyRingBasedDataProtector, ISpanDataProtector
{
    public KeyRingBasedSpanDataProtector(IKeyRingProvider keyRingProvider, ILogger? logger, string[]? originalPurposes, string newPurpose)
        : base(keyRingProvider, logger, originalPurposes, newPurpose)
    {
    }

    public void Protect<TWriter>(ReadOnlySpan<byte> plaintext, ref TWriter destination) where TWriter : IBufferWriter<byte>
#if NET
        , allows ref struct
#endif
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

            // Step 1: Write the magic header and key id
            var headerBuffer = destination.GetSpan(preBufferSize);
#if NET
            BinaryPrimitives.WriteUInt32BigEndian(headerBuffer.Slice(0, sizeof(uint)), MAGIC_HEADER_V0);
            var writeKeyIdResult = defaultKeyId.TryWriteBytes(headerBuffer.Slice(sizeof(uint), sizeof(Guid)));
            Debug.Assert(writeKeyIdResult, "Failed to write Guid to destination.");
#else
            fixed (byte* pbBuffer = headerBuffer)
            {
                WriteBigEndianInteger(pbBuffer, MAGIC_HEADER_V0);
                WriteGuid(&pbBuffer[sizeof(uint)], defaultKeyId);
            }
#endif
            destination.Advance(preBufferSize);

            // Step 2: Perform encryption into the destination writer
            defaultEncryptor.Encrypt(plaintext, aad, ref destination);

            // At this point, destination := { magicHeader || keyId || encryptorSpecificProtectedPayload }
            // And we're done!
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // homogenize all errors to CryptographicException
            throw Error.Common_EncryptionFailed(ex);
        }
    }

    public void Unprotect<TWriter>(ReadOnlySpan<byte> protectedData, ref TWriter destination) where TWriter : IBufferWriter<byte>
#if NET
        , allows ref struct
#endif
    {
        try
        {
            if (protectedData.Length < _magicHeaderKeyIdSize)
            {
                // payload must contain at least the magic header and key id
                throw Error.ProtectionProvider_BadMagicHeader();
            }

            // Parse the payload version number and key id.
            var magicHeaderFromPayload = BinaryPrimitives.ReadUInt32BigEndian(protectedData.Slice(0, sizeof(uint)));
#if NET
            var keyIdFromPayload = new Guid(protectedData.Slice(sizeof(uint), sizeof(Guid)));
#else
            Guid keyIdFromPayload;
            fixed (byte* pbProtectedData = protectedData)
            {
                keyIdFromPayload = ReadGuid(&pbProtectedData[sizeof(uint)]);
            }
#endif

            // Are the magic header and version information correct?
            if (!TryGetVersionFromMagicHeader(magicHeaderFromPayload, out var payloadVersion))
            {
                throw Error.ProtectionProvider_BadMagicHeader();
            }

            if (payloadVersion != 0)
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

                    throw Error.Common_KeyNotFound(keyIdFromPayload);
                }
            }

            // Check if key was revoked - for simplified version, we disallow revoked keys
            if (keyWasRevoked)
            {
                if (_logger.IsDebugLevelEnabled())
                {
                    _logger.KeyWasRevokedUnprotectOperationCannotProceed(keyIdFromPayload);
                }

                throw Error.Common_KeyRevoked(keyIdFromPayload);
            }

            // Perform the decryption operation.
            ReadOnlySpan<byte> actualCiphertext = protectedData.Slice(sizeof(uint) + sizeof(Guid)); // chop off magic header + key id
            ReadOnlySpan<byte> aad = _aadTemplate.GetAadForKey(keyIdFromPayload, isProtecting: false);

            // At this point, actualCiphertext := { encryptorSpecificPayload },
            // so all that's left is to invoke the decryption routine directly.
            var spanEncryptor = (ISpanAuthenticatedEncryptor)requestedEncryptor;
            spanEncryptor.Decrypt(actualCiphertext, aad, ref destination);

            // At this point, destination contains the decrypted plaintext
            // And we're done!
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // homogenize all errors to CryptographicException
            throw Error.DecryptionFailed(ex);
        }
    }
}

#endif

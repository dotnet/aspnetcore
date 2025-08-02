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

    public int GetProtectedSize(ReadOnlySpan<byte> plainText)
    {
        // Get the current key ring to access the encryptor
        var currentKeyRing = _keyRingProvider.GetCurrentKeyRing();
        var defaultEncryptor = (ISpanAuthenticatedEncryptor)currentKeyRing.DefaultAuthenticatedEncryptor!;
        CryptoUtil.Assert(defaultEncryptor != null, "DefaultAuthenticatedEncryptor != null");

        // We allocate a 20-byte pre-buffer so that we can inject the magic header and key id into the return value.
        // See Protect() / TryProtect() for details
        return _magicHeaderKeyIdSize + defaultEncryptor.GetEncryptedSize(plainText.Length);
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
}

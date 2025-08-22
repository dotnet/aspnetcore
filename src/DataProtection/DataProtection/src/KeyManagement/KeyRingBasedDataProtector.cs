// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;
using System.Buffers.Text;
using Microsoft.AspNetCore.DataProtection.Internal;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

internal sealed unsafe class KeyRingBasedDataProtector : IDataProtector, IPersistedDataProtector
{
    // This magic header identifies a v0 protected data blob. It's the high 28 bits of the SHA1 hash of
    // "Microsoft.AspNet.DataProtection.KeyManagement.KeyRingBasedDataProtector" [US-ASCII], big-endian.
    // The last nibble reserved for version information. There's also the nice property that "F0 C9"
    // can never appear in a well-formed UTF8 sequence, so attempts to treat a protected payload as a
    // UTF8-encoded string will fail, and devs can catch the mistake early.
    private const uint MAGIC_HEADER_V0 = 0x09F0C9F0;

    private AdditionalAuthenticatedDataTemplate _aadTemplate;
    private readonly IKeyRingProvider _keyRingProvider;
    private readonly ILogger? _logger;

    public KeyRingBasedDataProtector(IKeyRingProvider keyRingProvider, ILogger? logger, string[]? originalPurposes, string newPurpose)
    {
        Debug.Assert(keyRingProvider != null);

        Purposes = ConcatPurposes(originalPurposes, newPurpose);
        _logger = logger; // can be null
        _keyRingProvider = keyRingProvider;
        _aadTemplate = new AdditionalAuthenticatedDataTemplate(Purposes);
    }

    internal string[] Purposes { get; }

    private static string[] ConcatPurposes(string[]? originalPurposes, string newPurpose)
    {
        if (originalPurposes != null && originalPurposes.Length > 0)
        {
            var newPurposes = new string[originalPurposes.Length + 1];
            Array.Copy(originalPurposes, 0, newPurposes, 0, originalPurposes.Length);
            newPurposes[originalPurposes.Length] = newPurpose;
            return newPurposes;
        }
        else
        {
            return new string[] { newPurpose };
        }
    }

    public IDataProtector CreateProtector(string purpose)
    {
        ArgumentNullThrowHelper.ThrowIfNull(purpose);

        return new KeyRingBasedDataProtector(
            logger: _logger,
            keyRingProvider: _keyRingProvider,
            originalPurposes: Purposes,
            newPurpose: purpose);
    }

    private static string JoinPurposesForLog(IEnumerable<string> purposes)
    {
        return "(" + String.Join(", ", purposes.Select(p => "'" + p + "'")) + ")";
    }

    // allows decrypting payloads whose keys have been revoked
    public byte[] DangerousUnprotect(byte[] protectedData, bool ignoreRevocationErrors, out bool requiresMigration, out bool wasRevoked)
    {
        // argument & state checking
        ArgumentNullThrowHelper.ThrowIfNull(protectedData);

        UnprotectStatus status;
        var retVal = UnprotectCore(protectedData, ignoreRevocationErrors, status: out status);
        requiresMigration = (status != UnprotectStatus.Ok);
        wasRevoked = (status == UnprotectStatus.DecryptionKeyWasRevoked);
        return retVal;
    }

    public byte[] Protect(byte[] plaintext)
    {
        ArgumentNullThrowHelper.ThrowIfNull(plaintext);

        try
        {
            // Perform the encryption operation using the current default encryptor.
            var currentKeyRing = _keyRingProvider.GetCurrentKeyRing();
            var defaultKeyId = currentKeyRing.DefaultKeyId;
            var defaultEncryptorInstance = currentKeyRing.DefaultAuthenticatedEncryptor;
            CryptoUtil.Assert(defaultEncryptorInstance != null, "defaultEncryptorInstance != null");

            if (_logger.IsDebugLevelEnabled())
            {
                _logger.PerformingProtectOperationToKeyWithPurposes(defaultKeyId, JoinPurposesForLog(Purposes));
            }

            // We'll need to apply the default key id to the template if it hasn't already been applied.
            // If the default key id has been updated since the last call to Protect, also write back the updated template.
            var aad = _aadTemplate.GetAadForKey(defaultKeyId, isProtecting: true);

            // We allocate a 20-byte pre-buffer so that we can inject the magic header and key id into the return value.
            var retVal = defaultEncryptorInstance.Encrypt(
                plaintext: new ArraySegment<byte>(plaintext),
                additionalAuthenticatedData: new ArraySegment<byte>(aad),
                preBufferSize: (uint)(sizeof(uint) + sizeof(Guid)),
                postBufferSize: 0);
            CryptoUtil.Assert(retVal != null && retVal.Length >= sizeof(uint) + sizeof(Guid), "retVal != null && retVal.Length >= sizeof(uint) + sizeof(Guid)");

            // At this point: retVal := { 000..000 || encryptorSpecificProtectedPayload },
            // where 000..000 is a placeholder for our magic header and key id.

            // Write out the magic header and key id
            fixed (byte* pbRetVal = retVal)
            {
                WriteBigEndianInteger(pbRetVal, MAGIC_HEADER_V0);
                WriteGuid(&pbRetVal[sizeof(uint)], defaultKeyId);
            }

            // At this point, retVal := { magicHeader || keyId || encryptorSpecificProtectedPayload }
            // And we're done!
            return retVal;
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // homogenize all errors to CryptographicException
            throw Error.Common_EncryptionFailed(ex);
        }
    }

    private static Guid ReadGuid(void* ptr)
    {
#if NETCOREAPP
        // Performs appropriate endianness fixups
        return new Guid(new ReadOnlySpan<byte>(ptr, sizeof(Guid)));
#elif NETSTANDARD2_0 || NETFRAMEWORK
        Debug.Assert(BitConverter.IsLittleEndian);
        return Unsafe.ReadUnaligned<Guid>(ptr);
#else
#error Update target frameworks
#endif
    }

    private static uint ReadBigEndian32BitInteger(byte* ptr)
    {
        return ((uint)ptr[0] << 24)
            | ((uint)ptr[1] << 16)
            | ((uint)ptr[2] << 8)
            | ((uint)ptr[3]);
    }

    private static bool TryGetVersionFromMagicHeader(uint magicHeader, out int version)
    {
        const uint MAGIC_HEADER_VERSION_MASK = 0xFU;
        if ((magicHeader & ~MAGIC_HEADER_VERSION_MASK) == MAGIC_HEADER_V0)
        {
            version = (int)(magicHeader & MAGIC_HEADER_VERSION_MASK);
            return true;
        }
        else
        {
            version = default(int);
            return false;
        }
    }

    public byte[] Unprotect(byte[] protectedData)
    {
        ArgumentNullThrowHelper.ThrowIfNull(protectedData);

        // Argument checking will be done by the callee
        return DangerousUnprotect(protectedData,
            ignoreRevocationErrors: false,
            requiresMigration: out _,
            wasRevoked: out _);
    }

    private byte[] UnprotectCore(byte[] protectedData, bool allowOperationsOnRevokedKeys, out UnprotectStatus status)
    {
        Debug.Assert(protectedData != null);

        try
        {
            // argument & state checking
            if (protectedData.Length < sizeof(uint) /* magic header */ + sizeof(Guid) /* key id */)
            {
                // payload must contain at least the magic header and key id
                throw Error.ProtectionProvider_BadMagicHeader();
            }

            // Need to check that protectedData := { magicHeader || keyId || encryptorSpecificProtectedPayload }

            // Parse the payload version number and key id.
            uint magicHeaderFromPayload;
            Guid keyIdFromPayload;
            fixed (byte* pbInput = protectedData)
            {
                magicHeaderFromPayload = ReadBigEndian32BitInteger(pbInput);
                keyIdFromPayload = ReadGuid(&pbInput[sizeof(uint)]);
            }

            // Are the magic header and version information correct?
            int payloadVersion;
            if (!TryGetVersionFromMagicHeader(magicHeaderFromPayload, out payloadVersion))
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
            bool keyWasRevoked;
            var currentKeyRing = _keyRingProvider.GetCurrentKeyRing();
            var requestedEncryptor = currentKeyRing.GetAuthenticatedEncryptorByKeyId(keyIdFromPayload, out keyWasRevoked);
            if (requestedEncryptor == null)
            {
                if (_keyRingProvider is KeyRingProvider provider && provider.InAutoRefreshWindow())
                {
                    currentKeyRing = provider.RefreshCurrentKeyRing();
                    requestedEncryptor = currentKeyRing.GetAuthenticatedEncryptorByKeyId(keyIdFromPayload, out keyWasRevoked);
                }

                if (requestedEncryptor == null)
                {
                    if (_logger.IsTraceLevelEnabled())
                    {
                        _logger.KeyWasNotFoundInTheKeyRingUnprotectOperationCannotProceed(keyIdFromPayload);
                    }
                    throw Error.Common_KeyNotFound(keyIdFromPayload);
                }
            }

            // Do we need to notify the caller that they should reprotect the data?
            status = UnprotectStatus.Ok;
            if (keyIdFromPayload != currentKeyRing.DefaultKeyId)
            {
                status = UnprotectStatus.DefaultEncryptionKeyChanged;
            }

            // Do we need to notify the caller that this key was revoked?
            if (keyWasRevoked)
            {
                if (allowOperationsOnRevokedKeys)
                {
                    if (_logger.IsDebugLevelEnabled())
                    {
                        _logger.KeyWasRevokedCallerRequestedUnprotectOperationProceedRegardless(keyIdFromPayload);
                    }
                    status = UnprotectStatus.DecryptionKeyWasRevoked;
                }
                else
                {
                    if (_logger.IsDebugLevelEnabled())
                    {
                        _logger.KeyWasRevokedUnprotectOperationCannotProceed(keyIdFromPayload);
                    }
                    throw Error.Common_KeyRevoked(keyIdFromPayload);
                }
            }

            // Perform the decryption operation.
            ArraySegment<byte> ciphertext = new ArraySegment<byte>(protectedData, sizeof(uint) + sizeof(Guid), protectedData.Length - (sizeof(uint) + sizeof(Guid))); // chop off magic header + encryptor id
            ArraySegment<byte> additionalAuthenticatedData = new ArraySegment<byte>(_aadTemplate.GetAadForKey(keyIdFromPayload, isProtecting: false));

            // At this point, cipherText := { encryptorSpecificPayload },
            // so all that's left is to invoke the decryption routine directly.
            return requestedEncryptor.Decrypt(ciphertext, additionalAuthenticatedData)
                ?? CryptoUtil.Fail<byte[]>("IAuthenticatedEncryptor.Decrypt returned null.");
        }
        catch (Exception ex) when (ex.RequiresHomogenization())
        {
            // homogenize all failures to CryptographicException
            throw Error.DecryptionFailed(ex);
        }
    }

    private static void WriteGuid(void* ptr, Guid value)
    {
#if NETCOREAPP
        var span = new Span<byte>(ptr, sizeof(Guid));

        // Performs appropriate endianness fixups
        var success = value.TryWriteBytes(span);
        Debug.Assert(success, "Failed to write Guid.");
#elif NETSTANDARD2_0 || NETFRAMEWORK
        Debug.Assert(BitConverter.IsLittleEndian);
        Unsafe.WriteUnaligned<Guid>(ptr, value);
#else
#error Update target frameworks
#endif
    }

    private static void WriteBigEndianInteger(byte* ptr, uint value)
    {
        ptr[0] = (byte)(value >> 24);
        ptr[1] = (byte)(value >> 16);
        ptr[2] = (byte)(value >> 8);
        ptr[3] = (byte)(value);
    }

    internal struct AdditionalAuthenticatedDataTemplate
    {
        private byte[] _aadTemplate;

        public AdditionalAuthenticatedDataTemplate(string[] purposes)
        {
            _aadTemplate = BuildAadTemplateBytes(purposes);
        }

        public byte[] GetAadForKey(Guid keyId, bool isProtecting)
        {
            // Multiple threads might be trying to read and write the _aadTemplate field
            // simultaneously. We need to make sure all accesses to it are thread-safe.
            var existingTemplate = Volatile.Read(ref _aadTemplate);
            Debug.Assert(existingTemplate.Length >= sizeof(uint) /* MAGIC_HEADER */ + sizeof(Guid) /* keyId */);

            // If the template is already initialized to this key id, return it.
            // The caller will not mutate it.
            fixed (byte* pExistingTemplate = existingTemplate)
            {
                if (ReadGuid(&pExistingTemplate[sizeof(uint)]) == keyId)
                {
                    return existingTemplate;
                }
            }

            // Clone since we're about to make modifications.
            // If this is an encryption operation, we only ever encrypt to the default key,
            // so we should replace the existing template. This could occur after the protector
            // has already been created, such as when the underlying key ring has been modified.
            byte[] newTemplate = (byte[])existingTemplate.Clone();
            fixed (byte* pNewTemplate = newTemplate)
            {
                WriteGuid(&pNewTemplate[sizeof(uint)], keyId);
                if (isProtecting)
                {
                    Volatile.Write(ref _aadTemplate, newTemplate);
                }
                return newTemplate;
            }
        }

        internal static byte[] BuildAadTemplateBytes(string[] purposes)
        {
            // additionalAuthenticatedData := { magicHeader (32-bit) || keyId || purposeCount (32-bit) || (purpose)* }
            // purpose := { utf8ByteCount (7-bit encoded) || utf8Text }

            var keySize = sizeof(Guid);
            int totalPurposeLen = 4 + keySize + 4;

            int[]? lease = null;
            var targetLength = purposes.Length;
            Span<int> purposeLengthsPool = targetLength <= 32 ? stackalloc int[targetLength] : (lease = ArrayPool<int>.Shared.Rent(targetLength)).AsSpan(0, targetLength);
            for (int i = 0; i < targetLength; i++)
            {
                string purpose = purposes[i];

                int purposeLength = EncodingUtil.SecureUtf8Encoding.GetByteCount(purpose);
                purposeLengthsPool[i] = purposeLength;

                var encoded7BitUIntLength = purposeLength.Measure7BitEncodedUIntLength();
                totalPurposeLen += purposeLength /* length of actual string */ + encoded7BitUIntLength /* length of 'string length' 7-bit encoded int */;
            }

            byte[] targetArr = new byte[totalPurposeLen];
            var targetSpan = targetArr.AsSpan();

            // index 0: magic header
            BinaryPrimitives.WriteUInt32BigEndian(targetSpan.Slice(0), MAGIC_HEADER_V0);
            // index 4: key (skipped for now, will be populated in `GetAadForKey()`)
            // index 4 + keySize: purposeCount
            BinaryPrimitives.WriteInt32BigEndian(targetSpan.Slice(4 + keySize), targetLength);

            int index = 4 /* MAGIC_HEADER_V0 */ + keySize + 4 /* purposeLength */; // starting from first purpose
            for (int i = 0; i < targetLength; i++)
            {
                string purpose = purposes[i];

                // writing `utf8ByteCount (7-bit encoded integer)`
                // we have already calculated the lengths of the purpose strings, so just get it from the pool
                index += targetSpan.Slice(index).Write7BitEncodedInt(purposeLengthsPool[i]);

                // write the utf8text for the purpose
                index += EncodingUtil.SecureUtf8Encoding.GetBytes(purpose, charIndex: 0, charCount: purpose.Length, bytes: targetArr, byteIndex: index);
            }

            if (lease is not null)
            {
                ArrayPool<int>.Shared.Return(lease);
            }
            Debug.Assert(index == targetArr.Length);

            return targetArr;
        }
    }

    private enum UnprotectStatus
    {
        Ok,
        DefaultEncryptionKeyChanged,
        DecryptionKeyWasRevoked
    }
}

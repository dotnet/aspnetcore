// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.AspNet.Cryptography;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption;

namespace Microsoft.AspNet.DataProtection.KeyManagement
{
    internal unsafe sealed class KeyRingBasedDataProtector : IDataProtector
    {
        // This magic header identifies a v0 protected data blob.
        // It's the high 28 bits of the SHA1 hash of "Microsoft.AspNet.DataProtection.MultiplexingDataProtector" [US-ASCII].
        // The last nibble reserved for version information.
        // There's also the nice property that "F0 C9" can never appear in a well-formed UTF8 sequence, so attempts to
        // treat a protected payload as a UTF8-encoded string will fail, and devs can catch the mistake early.
        private const uint MAGIC_HEADER_V0 = 0x09F0C9F0;

        private byte[] _additionalAuthenticatedDataTemplate;
        private readonly IKeyRingProvider _keyringProvider;
        private readonly string[] _purposes;

        public KeyRingBasedDataProtector(IKeyRingProvider keyringProvider, string[] purposes)
        {
            _additionalAuthenticatedDataTemplate = GenerateAdditionalAuthenticatedDataTemplateFromPurposes(purposes);
            _keyringProvider = keyringProvider;
            _purposes = purposes;
        }

        private static byte[] ApplyEncryptorIdToAdditionalAuthenticatedDataTemplate(Guid encryptorId, byte[] additionalAuthenticatedDataTemplate)
        {
            CryptoUtil.Assert(additionalAuthenticatedDataTemplate.Length >= sizeof(uint) + sizeof(Guid), "additionalAuthenticatedDataTemplate.Length >= sizeof(uint) + sizeof(Guid)");

            // Optimization: just return the original template if the GUID already matches.
            fixed (byte* pbOriginal = additionalAuthenticatedDataTemplate)
            {
                if (Read32bitAlignedGuid(&pbOriginal[sizeof(uint)]) == encryptorId)
                {
                    return additionalAuthenticatedDataTemplate;
                }
            }

            // Clone the template since the input is immutable, then inject the encryptor ID into the new template
            byte[] cloned = (byte[])additionalAuthenticatedDataTemplate.Clone();
            fixed (byte* pbCloned = cloned)
            {
                Write32bitAlignedGuid(&pbCloned[sizeof(uint)], encryptorId);
            }
            return cloned;
        }

        public IDataProtector CreateProtector([NotNull] string purpose)
        {
            // Append the incoming purpose to the end of the original array to form a hierarchy
            string[] newPurposes = new string[_purposes.Length + 1];
            Array.Copy(_purposes, 0, newPurposes, 0, _purposes.Length);
            newPurposes[newPurposes.Length - 1] = purpose;

            // Use the same keyring as the current instance
            return new KeyRingBasedDataProtector(_keyringProvider, newPurposes);
        }

        private static byte[] GenerateAdditionalAuthenticatedDataTemplateFromPurposes(string[] purposes)
        {
            const int MEMORYSTREAM_DEFAULT_CAPACITY = 0x100; // matches MemoryStream.EnsureCapacity
            var ms = new MemoryStream(MEMORYSTREAM_DEFAULT_CAPACITY);

            // additionalAuthenticatedData := { magicHeader || encryptor-GUID || purposeCount || (purpose)* }
            // purpose := { utf8ByteCount || utf8Text }
            using (var writer = new PurposeBinaryWriter(ms))
            {
                writer.WriteBigEndian(MAGIC_HEADER_V0);
                Debug.Assert(ms.Position == sizeof(uint));
                writer.Seek(sizeof(Guid), SeekOrigin.Current); // skip over where the encryptor GUID will be stored; we'll fill it in later
                if (purposes != null)
                {
                    writer.Write7BitEncodedInt(purposes.Length);
                    foreach (var purpose in purposes)
                    {
                        if (String.IsNullOrEmpty(purpose))
                        {
                            writer.Write7BitEncodedInt(0); // blank purpose
                        }
                        else
                        {
                            writer.Write(purpose);
                        }
                    }
                }
                else
                {
                    writer.Write7BitEncodedInt(0); // empty purposes array
                }
            }

            return ms.ToArray();
        }

        public byte[] Protect(byte[] unprotectedData)
        {
            // argument & state checking
            if (unprotectedData == null)
            {
                throw new ArgumentNullException("unprotectedData");
            }

            // Perform the encryption operation using the current default encryptor.
            var currentKeyRing = _keyringProvider.GetCurrentKeyRing();
            var defaultKeyId = currentKeyRing.DefaultKeyId;
            var defaultEncryptorInstance = currentKeyRing.DefaultAuthenticatedEncryptor;
            CryptoUtil.Assert(defaultEncryptorInstance != null, "defaultEncryptorInstance != null");

            // We'll need to apply the default encryptor ID to the template if it hasn't already been applied.
            // If the default encryptor ID has been updated since the last call to Protect, also write back the updated template.
            byte[] aadTemplate = Volatile.Read(ref _additionalAuthenticatedDataTemplate);
            byte[] aadForInvocation = ApplyEncryptorIdToAdditionalAuthenticatedDataTemplate(defaultKeyId, aadTemplate);
            if (aadTemplate != aadForInvocation)
            {
                Volatile.Write(ref _additionalAuthenticatedDataTemplate, aadForInvocation);
            }

            // We allocate a 20-byte pre-buffer so that we can inject the magic header and encryptor id into the return value.
            byte[] retVal;
            try
            {
                retVal = defaultEncryptorInstance.Encrypt(
                    plaintext: new ArraySegment<byte>(unprotectedData),
                    additionalAuthenticatedData: new ArraySegment<byte>(aadForInvocation),
                    preBufferSize: (uint)(sizeof(uint) + sizeof(Guid)),
                    postBufferSize: 0);
                CryptoUtil.Assert(retVal != null && retVal.Length >= sizeof(uint) + sizeof(Guid), "retVal != null && retVal.Length >= sizeof(uint) + sizeof(Guid)");
            }
            catch (Exception ex) when (ex.RequiresHomogenization())
            {
                // homogenize all errors to CryptographicException
                throw Error.Common_EncryptionFailed(ex);
            }

            // At this point: retVal := { 000..000 || encryptorSpecificProtectedPayload },
            // where 000..000 is a placeholder for our magic header and encryptor ID.

            // Write out the magic header and encryptor ID
            fixed (byte* pbRetVal = retVal)
            {
                WriteBigEndianInteger(pbRetVal, MAGIC_HEADER_V0);
                Write32bitAlignedGuid(&pbRetVal[sizeof(uint)], defaultKeyId);
            }

            // At this point, retVal := { magicHeader || encryptor-GUID || encryptorSpecificProtectedPayload }
            // And we're done!
            return retVal;
        }

        // Helper function to read a GUID from a 32-bit alignment; useful on ARM where unaligned reads
        // can result in weird behaviors at runtime.
        private static Guid Read32bitAlignedGuid(void* ptr)
        {
            Debug.Assert((long)ptr % 4 == 0);

            Guid retVal;
            ((int*)&retVal)[0] = ((int*)ptr)[0];
            ((int*)&retVal)[1] = ((int*)ptr)[1];
            ((int*)&retVal)[2] = ((int*)ptr)[2];
            ((int*)&retVal)[3] = ((int*)ptr)[3];
            return retVal;
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
            // argument & state checking
            if (protectedData == null)
            {
                throw new ArgumentNullException("protectedData");
            }
            if (protectedData.Length < sizeof(uint) /* magic header */ + sizeof(Guid) /* key id */)
            {
                throw Error.Common_NotAValidProtectedPayload();
            }

            // Need to check that protectedData := { magicHeader || encryptor-GUID || encryptorSpecificProtectedPayload }

            // Parse the payload version number and encryptor ID.
            uint payloadMagicHeader;
            Guid payloadEncryptorId;
            fixed (byte* pbInput = protectedData)
            {
                payloadMagicHeader = ReadBigEndian32BitInteger(pbInput);
                payloadEncryptorId = Read32bitAlignedGuid(&pbInput[sizeof(uint)]);
            }

            // Are the magic header and version information correct?
            int payloadVersion;
            if (!TryGetVersionFromMagicHeader(payloadMagicHeader, out payloadVersion))
            {
                throw Error.Common_NotAValidProtectedPayload();
            }
            else if (payloadVersion != 0)
            {
                throw Error.Common_PayloadProducedByNewerVersion();
            }

            // Find the correct encryptor in the keyring.
            bool keyWasRevoked;
            var requestedEncryptor = _keyringProvider.GetCurrentKeyRing().GetAuthenticatedEncryptorByKeyId(payloadEncryptorId, out keyWasRevoked);
            if (requestedEncryptor == null)
            {
                throw Error.Common_KeyNotFound(payloadEncryptorId);
            }
            if (keyWasRevoked)
            {
                throw Error.Common_KeyRevoked(payloadEncryptorId);
            }

            // Perform the decryption operation.
            ArraySegment<byte> ciphertext = new ArraySegment<byte>(protectedData, sizeof(uint) + sizeof(Guid), protectedData.Length - (sizeof(uint) + sizeof(Guid))); // chop off magic header + encryptor id
            ArraySegment<byte> additionalAuthenticatedData = new ArraySegment<byte>(ApplyEncryptorIdToAdditionalAuthenticatedDataTemplate(payloadEncryptorId, Volatile.Read(ref _additionalAuthenticatedDataTemplate)));

            try
            {
                // At this point, cipherText := { encryptorSpecificPayload },
                // so all that's left is to invoke the decryption routine directly.
                byte[] retVal = requestedEncryptor.Decrypt(ciphertext, additionalAuthenticatedData);
                CryptoUtil.Assert(retVal != null, "retVal != null");
                return retVal;
            }
            catch (Exception ex) when (ex.RequiresHomogenization())
            {
                // homogenize all failures to CryptographicException
                throw Error.DecryptionFailed(ex);
            }
        }

        // Helper function to write a GUID to a 32-bit alignment; useful on ARM where unaligned reads
        // can result in weird behaviors at runtime.
        private static void Write32bitAlignedGuid(void* ptr, Guid value)
        {
            Debug.Assert((long)ptr % 4 == 0);

            ((int*)ptr)[0] = ((int*)&value)[0];
            ((int*)ptr)[1] = ((int*)&value)[1];
            ((int*)ptr)[2] = ((int*)&value)[2];
            ((int*)ptr)[3] = ((int*)&value)[3];
        }

        private static void WriteBigEndianInteger(byte* ptr, uint value)
        {
            ptr[0] = (byte)(value >> 24);
            ptr[1] = (byte)(value >> 16);
            ptr[2] = (byte)(value >> 8);
            ptr[3] = (byte)(value);
        }

        private sealed class PurposeBinaryWriter : BinaryWriter
        {
            // Strings should never contain invalid UTF16 chars, so we'll use a secure encoding.
            private static readonly byte[] _guidBuffer = new byte[sizeof(Guid)];

            public PurposeBinaryWriter(MemoryStream stream) : base(stream, EncodingUtil.SecureUtf8Encoding, leaveOpen: true) { }

            public new void Write7BitEncodedInt(int value)
            {
                base.Write7BitEncodedInt(value);
            }

            // Writes a big-endian 32-bit integer to the underlying stream.
            public void WriteBigEndian(uint value)
            {
                var outStream = BaseStream; // property accessor also performs a flush
                outStream.WriteByte((byte)(value >> 24));
                outStream.WriteByte((byte)(value >> 16));
                outStream.WriteByte((byte)(value >> 8));
                outStream.WriteByte((byte)(value));
            }
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement
{
    public class KeyRingBasedDataProtectorTests
    {
        [Fact]
        public void Protect_NullPlaintext_Throws()
        {
            // Arrange
            IDataProtector protector = new KeyRingBasedDataProtector(
                keyRingProvider: new Mock<IKeyRingProvider>().Object,
                logger: GetLogger(),
                originalPurposes: null,
                newPurpose: "purpose");

            // Act & assert
            ExceptionAssert.ThrowsArgumentNull(() => protector.Protect(plaintext: null), "plaintext");
        }

        [Fact]
        public void Protect_EncryptsToDefaultProtector_MultiplePurposes()
        {
            // Arrange
            Guid defaultKey = new Guid("ba73c9ce-d322-4e45-af90-341307e11c38");
            byte[] expectedPlaintext = new byte[] { 0x03, 0x05, 0x07, 0x11, 0x13, 0x17, 0x19 };
            byte[] expectedAad = BuildAadFromPurposeStrings(defaultKey, "purpose1", "purpose2", "yet another purpose");
            byte[] expectedProtectedData = BuildProtectedDataFromCiphertext(defaultKey, new byte[] { 0x23, 0x29, 0x31, 0x37 });

            var mockEncryptor = new Mock<IAuthenticatedEncryptor>();
            mockEncryptor
                .Setup(o => o.Encrypt(It.IsAny<ArraySegment<byte>>(), It.IsAny<ArraySegment<byte>>()))
                .Returns<ArraySegment<byte>, ArraySegment<byte>>((actualPlaintext, actualAad) =>
                {
                    Assert.Equal(expectedPlaintext, actualPlaintext);
                    Assert.Equal(expectedAad, actualAad);
                    return new byte[] { 0x23, 0x29, 0x31, 0x37 }; // ciphertext + tag
                });

            var mockKeyRing = new Mock<IKeyRing>(MockBehavior.Strict);
            mockKeyRing.Setup(o => o.DefaultKeyId).Returns(defaultKey);
            mockKeyRing.Setup(o => o.DefaultAuthenticatedEncryptor).Returns(mockEncryptor.Object);
            var mockKeyRingProvider = new Mock<IKeyRingProvider>();
            mockKeyRingProvider.Setup(o => o.GetCurrentKeyRing()).Returns(mockKeyRing.Object);

            IDataProtector protector = new KeyRingBasedDataProtector(
                keyRingProvider: mockKeyRingProvider.Object,
                logger: GetLogger(),
                originalPurposes: new[] { "purpose1", "purpose2" },
                newPurpose: "yet another purpose");

            // Act
            byte[] retVal = protector.Protect(expectedPlaintext);

            // Assert
            Assert.Equal(expectedProtectedData, retVal);
        }

        [Fact]
        public void Protect_EncryptsToDefaultProtector_SinglePurpose()
        {
            // Arrange
            Guid defaultKey = new Guid("ba73c9ce-d322-4e45-af90-341307e11c38");
            byte[] expectedPlaintext = new byte[] { 0x03, 0x05, 0x07, 0x11, 0x13, 0x17, 0x19 };
            byte[] expectedAad = BuildAadFromPurposeStrings(defaultKey, "single purpose");
            byte[] expectedProtectedData = BuildProtectedDataFromCiphertext(defaultKey, new byte[] { 0x23, 0x29, 0x31, 0x37 });

            var mockEncryptor = new Mock<IAuthenticatedEncryptor>();
            mockEncryptor
                .Setup(o => o.Encrypt(It.IsAny<ArraySegment<byte>>(), It.IsAny<ArraySegment<byte>>()))
                .Returns<ArraySegment<byte>, ArraySegment<byte>>((actualPlaintext, actualAad) =>
                {
                    Assert.Equal(expectedPlaintext, actualPlaintext);
                    Assert.Equal(expectedAad, actualAad);
                    return new byte[] { 0x23, 0x29, 0x31, 0x37 }; // ciphertext + tag
                });

            var mockKeyRing = new Mock<IKeyRing>(MockBehavior.Strict);
            mockKeyRing.Setup(o => o.DefaultKeyId).Returns(defaultKey);
            mockKeyRing.Setup(o => o.DefaultAuthenticatedEncryptor).Returns(mockEncryptor.Object);
            var mockKeyRingProvider = new Mock<IKeyRingProvider>();
            mockKeyRingProvider.Setup(o => o.GetCurrentKeyRing()).Returns(mockKeyRing.Object);

            IDataProtector protector = new KeyRingBasedDataProtector(
                keyRingProvider: mockKeyRingProvider.Object,
                logger: GetLogger(),
                originalPurposes: new string[0],
                newPurpose: "single purpose");

            // Act
            byte[] retVal = protector.Protect(expectedPlaintext);

            // Assert
            Assert.Equal(expectedProtectedData, retVal);
        }

        [Fact]
        public void Protect_HomogenizesExceptionsToCryptographicException()
        {
            // Arrange
            IDataProtector protector = new KeyRingBasedDataProtector(
                keyRingProvider: new Mock<IKeyRingProvider>(MockBehavior.Strict).Object,
                logger: GetLogger(),
                originalPurposes: null,
                newPurpose: "purpose");

            // Act & assert
            var ex = ExceptionAssert2.ThrowsCryptographicException(() => protector.Protect(new byte[0]));
            Assert.IsAssignableFrom<MockException>(ex.InnerException);
        }

        [Fact]
        public void Unprotect_NullProtectedData_Throws()
        {
            // Arrange
            IDataProtector protector = new KeyRingBasedDataProtector(
                keyRingProvider: new Mock<IKeyRingProvider>().Object,
                logger: GetLogger(),
                originalPurposes: null,
                newPurpose: "purpose");

            // Act & assert
            ExceptionAssert.ThrowsArgumentNull(() => protector.Unprotect(protectedData: null), "protectedData");
        }

        [Fact]
        public void Unprotect_PayloadTooShort_ThrowsBadMagicHeader()
        {
            // Arrange
            IDataProtector protector = new KeyRingBasedDataProtector(
                keyRingProvider: new Mock<IKeyRingProvider>().Object,
                logger: GetLogger(),
                originalPurposes: null,
                newPurpose: "purpose");

            byte[] badProtectedPayload = BuildProtectedDataFromCiphertext(Guid.NewGuid(), new byte[0]);
            badProtectedPayload = badProtectedPayload.Take(badProtectedPayload.Length - 1).ToArray(); // chop off the last byte

            // Act & assert
            var ex = ExceptionAssert2.ThrowsCryptographicException(() => protector.Unprotect(badProtectedPayload));
            Assert.Equal(Resources.ProtectionProvider_BadMagicHeader, ex.Message);
        }

        [Fact]
        public void Unprotect_PayloadHasBadMagicHeader_ThrowsBadMagicHeader()
        {
            // Arrange
            IDataProtector protector = new KeyRingBasedDataProtector(
                keyRingProvider: new Mock<IKeyRingProvider>().Object,
                logger: GetLogger(),
                originalPurposes: null,
                newPurpose: "purpose");

            byte[] badProtectedPayload = BuildProtectedDataFromCiphertext(Guid.NewGuid(), new byte[0]);
            badProtectedPayload[0]++; // corrupt the magic header

            // Act & assert
            var ex = ExceptionAssert2.ThrowsCryptographicException(() => protector.Unprotect(badProtectedPayload));
            Assert.Equal(Resources.ProtectionProvider_BadMagicHeader, ex.Message);
        }

        [Fact]
        public void Unprotect_PayloadHasIncorrectVersionMarker_ThrowsNewerVersion()
        {
            // Arrange
            IDataProtector protector = new KeyRingBasedDataProtector(
                keyRingProvider: new Mock<IKeyRingProvider>().Object,
                logger: GetLogger(),
                originalPurposes: null,
                newPurpose: "purpose");

            byte[] badProtectedPayload = BuildProtectedDataFromCiphertext(Guid.NewGuid(), new byte[0]);
            badProtectedPayload[3]++; // bump the version payload

            // Act & assert
            var ex = ExceptionAssert2.ThrowsCryptographicException(() => protector.Unprotect(badProtectedPayload));
            Assert.Equal(Resources.ProtectionProvider_BadVersion, ex.Message);
        }

        [Fact]
        public void Unprotect_KeyNotFound_ThrowsKeyNotFound()
        {
            // Arrange
            Guid notFoundKeyId = new Guid("654057ab-2491-4471-a72a-b3b114afda38");
            byte[] protectedData = BuildProtectedDataFromCiphertext(
                keyId: notFoundKeyId,
                ciphertext: new byte[0]);

            var mockDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>();
            var mockEncryptorFactory = new Mock<IAuthenticatedEncryptorFactory>();
            mockEncryptorFactory.Setup(o => o.CreateEncryptorInstance(It.IsAny<IKey>())).Returns(new Mock<IAuthenticatedEncryptor>().Object);
            var encryptorFactory = new AuthenticatedEncryptorFactory(NullLoggerFactory.Instance);

            // the keyring has only one key
            Key key = new Key(Guid.Empty, DateTimeOffset.Now, DateTimeOffset.Now, DateTimeOffset.Now, mockDescriptor.Object, new[] { mockEncryptorFactory.Object });
            var keyRing = new KeyRing(key, new[] { key });
            var mockKeyRingProvider = new Mock<IKeyRingProvider>();
            mockKeyRingProvider.Setup(o => o.GetCurrentKeyRing()).Returns(keyRing);

            IDataProtector protector = new KeyRingBasedDataProtector(
                keyRingProvider: mockKeyRingProvider.Object,
                logger: GetLogger(),
                originalPurposes: null,
                newPurpose: "purpose");

            // Act & assert
            var ex = ExceptionAssert2.ThrowsCryptographicException(() => protector.Unprotect(protectedData));
            Assert.Equal(Error.Common_KeyNotFound(notFoundKeyId).Message, ex.Message);
        }

        private static DateTime StringToDateTime(string input)
        {
            return DateTimeOffset.ParseExact(input, "u", CultureInfo.InvariantCulture).UtcDateTime;
        }

        private static KeyRingProvider CreateKeyRingProvider(ICacheableKeyRingProvider cacheableKeyRingProvider)
        {
            var mockEncryptorFactory = new Mock<IAuthenticatedEncryptorFactory>();
            mockEncryptorFactory.Setup(m => m.CreateEncryptorInstance(It.IsAny<IKey>())).Returns(new Mock<IAuthenticatedEncryptor>().Object);
            var options = new KeyManagementOptions();
            options.AuthenticatedEncryptorFactories.Add(mockEncryptorFactory.Object);

            return new KeyRingProvider(
                keyManager: null,
                keyManagementOptions: Options.Create(options),
                defaultKeyResolver: null,
                loggerFactory: NullLoggerFactory.Instance)
            {
                CacheableKeyRingProvider = cacheableKeyRingProvider
            };
        }

        [Fact]
        public void Unprotect_KeyNotFound_RefreshOnce_ThrowsKeyNotFound()
        {
            // Arrange
            Guid notFoundKeyId = new Guid("654057ab-2491-4471-a72a-b3b114afda38");
            byte[] protectedData = BuildProtectedDataFromCiphertext(
                keyId: notFoundKeyId,
                ciphertext: new byte[0]);

            var mockDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>();
            var mockEncryptorFactory = new Mock<IAuthenticatedEncryptorFactory>();
            mockEncryptorFactory.Setup(o => o.CreateEncryptorInstance(It.IsAny<IKey>())).Returns(new Mock<IAuthenticatedEncryptor>().Object);
            var encryptorFactory = new AuthenticatedEncryptorFactory(NullLoggerFactory.Instance);

            // the keyring has only one key
            Key key = new Key(Guid.Empty, DateTimeOffset.Now, DateTimeOffset.Now, DateTimeOffset.Now, mockDescriptor.Object, new[] { mockEncryptorFactory.Object });
            var keyRing = new CacheableKeyRing(CancellationToken.None, DateTimeOffset.MaxValue, key, new[] { key });

            var keyRingProvider = CreateKeyRingProvider(new TestKeyRingProvider(keyRing));

            IDataProtector protector = new KeyRingBasedDataProtector(
                keyRingProvider: keyRingProvider,
                logger: GetLogger(),
                originalPurposes: null,
                newPurpose: "purpose");

            // Act & assert
            var ex = ExceptionAssert2.ThrowsCryptographicException(() => protector.Unprotect(protectedData));
            Assert.Equal(Error.Common_KeyNotFound(notFoundKeyId).Message, ex.Message);
        }

        [Fact]
        public void Unprotect_KeyNotFound_WontRefreshOnce_AfterTooLong()
        {
            // Arrange
            Guid notFoundKeyId = new Guid("654057ab-2491-4471-a72a-b3b114afda38");
            byte[] protectedData = BuildProtectedDataFromCiphertext(
                keyId: notFoundKeyId,
                ciphertext: new byte[0]);

            var mockDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>();
            var mockEncryptorFactory = new Mock<IAuthenticatedEncryptorFactory>();
            mockEncryptorFactory.Setup(o => o.CreateEncryptorInstance(It.IsAny<IKey>())).Returns(new Mock<IAuthenticatedEncryptor>().Object);
            var encryptorFactory = new AuthenticatedEncryptorFactory(NullLoggerFactory.Instance);

            // the keyring has only one key
            Key key = new Key(Guid.Empty, DateTimeOffset.Now, DateTimeOffset.Now, DateTimeOffset.Now, mockDescriptor.Object, new[] { mockEncryptorFactory.Object });
            var keyRing = new CacheableKeyRing(CancellationToken.None, DateTimeOffset.MaxValue, key, new[] { key });

            // the refresh keyring has the notfound key
            Key key2 = new Key(notFoundKeyId, DateTimeOffset.Now, DateTimeOffset.Now, DateTimeOffset.Now, mockDescriptor.Object, new[] { mockEncryptorFactory.Object });
            var keyRing2 = new CacheableKeyRing(CancellationToken.None, DateTimeOffset.MaxValue, key, new[] { key2 });

            var keyRingProvider = CreateKeyRingProvider(new RefreshTestKeyRingProvider(keyRing, keyRing2));
            keyRingProvider.AutoRefreshWindowEnd = DateTime.UtcNow;

            IDataProtector protector = new KeyRingBasedDataProtector(
                keyRingProvider: keyRingProvider,
                logger: GetLogger(),
                originalPurposes: null,
                newPurpose: "purpose");

            // Act & assert
            var ex = ExceptionAssert2.ThrowsCryptographicException(() => protector.Unprotect(protectedData));
            Assert.Equal(Error.Common_KeyNotFound(notFoundKeyId).Message, ex.Message);
        }

        [Fact]
        public void Unprotect_KeyNotFound_RefreshOnce_CanFindKey()
        {
            // Arrange
            Guid notFoundKeyId = new Guid("654057ab-2491-4471-a72a-b3b114afda38");
            byte[] protectedData = BuildProtectedDataFromCiphertext(
                keyId: notFoundKeyId,
                ciphertext: new byte[0]);

            var mockDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>();
            var mockEncryptorFactory = new Mock<IAuthenticatedEncryptorFactory>();
            mockEncryptorFactory.Setup(o => o.CreateEncryptorInstance(It.IsAny<IKey>())).Returns(new Mock<IAuthenticatedEncryptor>().Object);
            var encryptorFactory = new AuthenticatedEncryptorFactory(NullLoggerFactory.Instance);

            // the keyring has only one key
            Key key = new Key(Guid.Empty, DateTimeOffset.Now, DateTimeOffset.Now, DateTimeOffset.Now, mockDescriptor.Object, new[] { mockEncryptorFactory.Object });
            var keyRing = new CacheableKeyRing(CancellationToken.None, DateTimeOffset.MaxValue, key, new[] { key });

            // the refresh keyring has the notfound key
            Key key2 = new Key(notFoundKeyId, DateTimeOffset.Now, DateTimeOffset.Now, DateTimeOffset.Now, mockDescriptor.Object, new[] { mockEncryptorFactory.Object });
            var keyRing2 = new CacheableKeyRing(CancellationToken.None, DateTimeOffset.MaxValue, key, new[] { key2 });

            var keyRingProvider = CreateKeyRingProvider(new RefreshTestKeyRingProvider(keyRing, keyRing2));

            IDataProtector protector = new KeyRingBasedDataProtector(
                keyRingProvider: keyRingProvider,
                logger: GetLogger(),
                originalPurposes: null,
                newPurpose: "purpose");

            // Act & assert
            var result = protector.Unprotect(protectedData);
            Assert.Empty(result);
        }

        private class TestKeyRingProvider : ICacheableKeyRingProvider
        {
            private CacheableKeyRing _keyRing;

            public TestKeyRingProvider(CacheableKeyRing keys) => _keyRing = keys;

            public CacheableKeyRing GetCacheableKeyRing(DateTimeOffset now) => _keyRing;
        }

        private class RefreshTestKeyRingProvider : ICacheableKeyRingProvider
        {
            private CacheableKeyRing _keyRing;
            private CacheableKeyRing _refreshKeyRing;
            private bool _called;

            public RefreshTestKeyRingProvider(CacheableKeyRing keys, CacheableKeyRing refreshKeys)
            {
                _keyRing = keys;
                _refreshKeyRing = refreshKeys;
            }

            public CacheableKeyRing GetCacheableKeyRing(DateTimeOffset now)
            {
                if (!_called)
                {
                    _called = true;
                    return _keyRing;
                }
                return _refreshKeyRing;
            }
        }

        [Fact]
        public void Unprotect_KeyRevoked_RevocationDisallowed_ThrowsKeyRevoked()
        {
            // Arrange
            Guid keyId = new Guid("654057ab-2491-4471-a72a-b3b114afda38");
            byte[] protectedData = BuildProtectedDataFromCiphertext(
                keyId: keyId,
                ciphertext: new byte[0]);

            var mockDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>();
            var mockEncryptorFactory = new Mock<IAuthenticatedEncryptorFactory>();
            mockEncryptorFactory.Setup(o => o.CreateEncryptorInstance(It.IsAny<IKey>())).Returns(new Mock<IAuthenticatedEncryptor>().Object);

            // the keyring has only one key
            Key key = new Key(keyId, DateTimeOffset.Now, DateTimeOffset.Now, DateTimeOffset.Now, mockDescriptor.Object, new[] { mockEncryptorFactory.Object });
            key.SetRevoked();
            var keyRing = new KeyRing(key, new[] { key });
            var mockKeyRingProvider = new Mock<IKeyRingProvider>();
            mockKeyRingProvider.Setup(o => o.GetCurrentKeyRing()).Returns(keyRing);

            IDataProtector protector = new KeyRingBasedDataProtector(
                keyRingProvider: mockKeyRingProvider.Object,
                logger: GetLogger(),
                originalPurposes: null,
                newPurpose: "purpose");

            // Act & assert
            var ex = ExceptionAssert2.ThrowsCryptographicException(() => protector.Unprotect(protectedData));
            Assert.Equal(Error.Common_KeyRevoked(keyId).Message, ex.Message);
        }

        [Fact]
        public void Unprotect_KeyRevoked_RevocationAllowed_ReturnsOriginalData_SetsRevokedAndMigrationFlags()
        {
            // Arrange
            Guid defaultKeyId = new Guid("ba73c9ce-d322-4e45-af90-341307e11c38");
            byte[] expectedCiphertext = new byte[] { 0x03, 0x05, 0x07, 0x11, 0x13, 0x17, 0x19 };
            byte[] protectedData = BuildProtectedDataFromCiphertext(defaultKeyId, expectedCiphertext);
            byte[] expectedAad = BuildAadFromPurposeStrings(defaultKeyId, "purpose");
            byte[] expectedPlaintext = new byte[] { 0x23, 0x29, 0x31, 0x37 };

            var mockEncryptor = new Mock<IAuthenticatedEncryptor>();
            mockEncryptor
                .Setup(o => o.Decrypt(It.IsAny<ArraySegment<byte>>(), It.IsAny<ArraySegment<byte>>()))
                .Returns<ArraySegment<byte>, ArraySegment<byte>>((actualCiphertext, actualAad) =>
                {
                    Assert.Equal(expectedCiphertext, actualCiphertext);
                    Assert.Equal(expectedAad, actualAad);
                    return expectedPlaintext;
                });
            var mockDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>();
            var mockEncryptorFactory = new Mock<IAuthenticatedEncryptorFactory>();
            mockEncryptorFactory.Setup(o => o.CreateEncryptorInstance(It.IsAny<IKey>())).Returns(mockEncryptor.Object);

            Key defaultKey = new Key(defaultKeyId, DateTimeOffset.Now, DateTimeOffset.Now, DateTimeOffset.Now, mockDescriptor.Object, new[] { mockEncryptorFactory.Object });
            defaultKey.SetRevoked();
            var keyRing = new KeyRing(defaultKey, new[] { defaultKey });
            var mockKeyRingProvider = new Mock<IKeyRingProvider>();
            mockKeyRingProvider.Setup(o => o.GetCurrentKeyRing()).Returns(keyRing);

            IDataProtector protector = new KeyRingBasedDataProtector(
                keyRingProvider: mockKeyRingProvider.Object,
                logger: GetLogger(),
                originalPurposes: null,
                newPurpose: "purpose");

            // Act
            byte[] retVal = ((IPersistedDataProtector)protector).DangerousUnprotect(protectedData,
                ignoreRevocationErrors: true,
                requiresMigration: out var requiresMigration,
                wasRevoked: out var wasRevoked);

            // Assert
            Assert.Equal(expectedPlaintext, retVal);
            Assert.True(requiresMigration);
            Assert.True(wasRevoked);
        }

        [Fact]
        public void Unprotect_IsAlsoDefaultKey_Success_NoMigrationRequired()
        {
            // Arrange
            Guid defaultKeyId = new Guid("ba73c9ce-d322-4e45-af90-341307e11c38");
            byte[] expectedCiphertext = new byte[] { 0x03, 0x05, 0x07, 0x11, 0x13, 0x17, 0x19 };
            byte[] protectedData = BuildProtectedDataFromCiphertext(defaultKeyId, expectedCiphertext);
            byte[] expectedAad = BuildAadFromPurposeStrings(defaultKeyId, "purpose");
            byte[] expectedPlaintext = new byte[] { 0x23, 0x29, 0x31, 0x37 };

            var mockEncryptor = new Mock<IAuthenticatedEncryptor>();
            mockEncryptor
                .Setup(o => o.Decrypt(It.IsAny<ArraySegment<byte>>(), It.IsAny<ArraySegment<byte>>()))
                .Returns<ArraySegment<byte>, ArraySegment<byte>>((actualCiphertext, actualAad) =>
                {
                    Assert.Equal(expectedCiphertext, actualCiphertext);
                    Assert.Equal(expectedAad, actualAad);
                    return expectedPlaintext;
                });
            var mockDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>();
            var mockEncryptorFactory = new Mock<IAuthenticatedEncryptorFactory>();
            mockEncryptorFactory.Setup(o => o.CreateEncryptorInstance(It.IsAny<IKey>())).Returns(mockEncryptor.Object);

            Key defaultKey = new Key(defaultKeyId, DateTimeOffset.Now, DateTimeOffset.Now, DateTimeOffset.Now, mockDescriptor.Object, new[] { mockEncryptorFactory.Object });
            var keyRing = new KeyRing(defaultKey, new[] { defaultKey });
            var mockKeyRingProvider = new Mock<IKeyRingProvider>();
            mockKeyRingProvider.Setup(o => o.GetCurrentKeyRing()).Returns(keyRing);

            IDataProtector protector = new KeyRingBasedDataProtector(
                keyRingProvider: mockKeyRingProvider.Object,
                logger: GetLogger(),
                originalPurposes: null,
                newPurpose: "purpose");

            // Act & assert - IDataProtector
            byte[] retVal = protector.Unprotect(protectedData);
            Assert.Equal(expectedPlaintext, retVal);

            // Act & assert - IPersistedDataProtector
            retVal = ((IPersistedDataProtector)protector).DangerousUnprotect(protectedData,
                ignoreRevocationErrors: false,
                requiresMigration: out var requiresMigration,
                wasRevoked: out var wasRevoked);
            Assert.Equal(expectedPlaintext, retVal);
            Assert.False(requiresMigration);
            Assert.False(wasRevoked);
        }

        [Fact]
        public void Unprotect_IsNotDefaultKey_Success_RequiresMigration()
        {
            // Arrange
            Guid defaultKeyId = new Guid("ba73c9ce-d322-4e45-af90-341307e11c38");
            Guid embeddedKeyId = new Guid("9b5d2db3-299f-4eac-89e9-e9067a5c1853");
            byte[] expectedCiphertext = new byte[] { 0x03, 0x05, 0x07, 0x11, 0x13, 0x17, 0x19 };
            byte[] protectedData = BuildProtectedDataFromCiphertext(embeddedKeyId, expectedCiphertext);
            byte[] expectedAad = BuildAadFromPurposeStrings(embeddedKeyId, "purpose");
            byte[] expectedPlaintext = new byte[] { 0x23, 0x29, 0x31, 0x37 };

            var mockEncryptor = new Mock<IAuthenticatedEncryptor>();
            mockEncryptor
                .Setup(o => o.Decrypt(It.IsAny<ArraySegment<byte>>(), It.IsAny<ArraySegment<byte>>()))
                .Returns<ArraySegment<byte>, ArraySegment<byte>>((actualCiphertext, actualAad) =>
                {
                    Assert.Equal(expectedCiphertext, actualCiphertext);
                    Assert.Equal(expectedAad, actualAad);
                    return expectedPlaintext;
                });
            var mockDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>();
            var mockEncryptorFactory = new Mock<IAuthenticatedEncryptorFactory>();
            mockEncryptorFactory.Setup(o => o.CreateEncryptorInstance(It.IsAny<IKey>())).Returns(mockEncryptor.Object);

            Key defaultKey = new Key(defaultKeyId, DateTimeOffset.Now, DateTimeOffset.Now, DateTimeOffset.Now, new Mock<IAuthenticatedEncryptorDescriptor>().Object, new[] { mockEncryptorFactory.Object });
            Key embeddedKey = new Key(embeddedKeyId, DateTimeOffset.Now, DateTimeOffset.Now, DateTimeOffset.Now, mockDescriptor.Object, new[] { mockEncryptorFactory.Object });
            var keyRing = new KeyRing(defaultKey, new[] { defaultKey, embeddedKey });
            var mockKeyRingProvider = new Mock<IKeyRingProvider>();
            mockKeyRingProvider.Setup(o => o.GetCurrentKeyRing()).Returns(keyRing);

            IDataProtector protector = new KeyRingBasedDataProtector(
                keyRingProvider: mockKeyRingProvider.Object,
                logger: GetLogger(),
                originalPurposes: null,
                newPurpose: "purpose");

            // Act & assert - IDataProtector
            byte[] retVal = protector.Unprotect(protectedData);
            Assert.Equal(expectedPlaintext, retVal);

            // Act & assert - IPersistedDataProtector
            retVal = ((IPersistedDataProtector)protector).DangerousUnprotect(protectedData,
                ignoreRevocationErrors: false,
                requiresMigration: out var requiresMigration,
                wasRevoked: out var wasRevoked);
            Assert.Equal(expectedPlaintext, retVal);
            Assert.True(requiresMigration);
            Assert.False(wasRevoked);
        }

        [Fact]
        public void Protect_Unprotect_RoundTripsProperly()
        {
            // Arrange
            byte[] plaintext = new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50 };
            var encryptorFactory = new AuthenticatedEncryptorFactory(NullLoggerFactory.Instance);
            Key key = new Key(Guid.NewGuid(), DateTimeOffset.Now, DateTimeOffset.Now, DateTimeOffset.Now, new AuthenticatedEncryptorConfiguration().CreateNewDescriptor(), new[] { encryptorFactory });
            var keyRing = new KeyRing(key, new[] { key });
            var mockKeyRingProvider = new Mock<IKeyRingProvider>();
            mockKeyRingProvider.Setup(o => o.GetCurrentKeyRing()).Returns(keyRing);

            var protector = new KeyRingBasedDataProtector(
                keyRingProvider: mockKeyRingProvider.Object,
                logger: GetLogger(),
                originalPurposes: null,
                newPurpose: "purpose");

            // Act - protect
            byte[] protectedData = protector.Protect(plaintext);
            Assert.NotNull(protectedData);
            Assert.NotEqual(plaintext, protectedData);

            // Act - unprotect
            byte[] roundTrippedPlaintext = protector.Unprotect(protectedData);
            Assert.Equal(plaintext, roundTrippedPlaintext);
        }

        [Fact]
        public void CreateProtector_ChainsPurposes()
        {
            // Arrange
            Guid defaultKey = new Guid("ba73c9ce-d322-4e45-af90-341307e11c38");
            byte[] expectedPlaintext = new byte[] { 0x03, 0x05, 0x07, 0x11, 0x13, 0x17, 0x19 };
            byte[] expectedAad = BuildAadFromPurposeStrings(defaultKey, "purpose1", "purpose2");
            byte[] expectedProtectedData = BuildProtectedDataFromCiphertext(defaultKey, new byte[] { 0x23, 0x29, 0x31, 0x37 });

            var mockEncryptor = new Mock<IAuthenticatedEncryptor>();
            mockEncryptor
                .Setup(o => o.Encrypt(It.IsAny<ArraySegment<byte>>(), It.IsAny<ArraySegment<byte>>()))
                .Returns<ArraySegment<byte>, ArraySegment<byte>>((actualPlaintext, actualAad) =>
                {
                    Assert.Equal(expectedPlaintext, actualPlaintext);
                    Assert.Equal(expectedAad, actualAad);
                    return new byte[] { 0x23, 0x29, 0x31, 0x37 }; // ciphertext + tag
                });

            var mockKeyRing = new Mock<IKeyRing>(MockBehavior.Strict);
            mockKeyRing.Setup(o => o.DefaultKeyId).Returns(defaultKey);
            mockKeyRing.Setup(o => o.DefaultAuthenticatedEncryptor).Returns(mockEncryptor.Object);
            var mockKeyRingProvider = new Mock<IKeyRingProvider>();
            mockKeyRingProvider.Setup(o => o.GetCurrentKeyRing()).Returns(mockKeyRing.Object);

            IDataProtector protector = new KeyRingBasedDataProtector(
                keyRingProvider: mockKeyRingProvider.Object,
                logger: GetLogger(),
                originalPurposes: null,
                newPurpose: "purpose1").CreateProtector("purpose2");

            // Act
            byte[] retVal = protector.Protect(expectedPlaintext);

            // Assert
            Assert.Equal(expectedProtectedData, retVal);
        }

        private static byte[] BuildAadFromPurposeStrings(Guid keyId, params string[] purposes)
        {
            var expectedAad = new byte[] { 0x09, 0xF0, 0xC9, 0xF0 } // magic header
                .Concat(keyId.ToByteArray()) // key id
                .Concat(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(purposes.Length))); // purposeCount

            foreach (string purpose in purposes)
            {
                var memStream = new MemoryStream();
                var writer = new BinaryWriter(memStream, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);
                writer.Write(purpose); // also writes 7-bit encoded int length
                writer.Dispose();
                expectedAad = expectedAad.Concat(memStream.ToArray());
            }

            return expectedAad.ToArray();
        }

        private static byte[] BuildProtectedDataFromCiphertext(Guid keyId, byte[] ciphertext)
        {
            return new byte[] { 0x09, 0xF0, 0xC9, 0xF0 } // magic header
              .Concat(keyId.ToByteArray()) // key id
              .Concat(ciphertext).ToArray();

        }

        private static ILogger GetLogger()
        {
            var loggerFactory = NullLoggerFactory.Instance;
            return loggerFactory.CreateLogger(typeof(KeyRingBasedDataProtector));
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement
{
    public class KeyRingTests
    {
        [Fact]
        public void DefaultAuthenticatedEncryptor_Prop_InstantiationIsDeferred()
        {
            // Arrange
            var expectedEncryptorInstance = new Mock<IAuthenticatedEncryptor>().Object;
            var encryptorFactory = new MyEncryptorFactory(expectedEncryptorInstance);

            var key1 = new MyKey();
            var key2 = new MyKey();

            // Act
            var keyRing = new KeyRing(key1, new[] { key1, key2 }, new[] { encryptorFactory });

            // Assert
            Assert.Equal(0, encryptorFactory.NumTimesCreateEncryptorInstanceCalled);
            Assert.Same(expectedEncryptorInstance, keyRing.DefaultAuthenticatedEncryptor);
            Assert.Equal(1, encryptorFactory.NumTimesCreateEncryptorInstanceCalled);
            Assert.Same(expectedEncryptorInstance, keyRing.DefaultAuthenticatedEncryptor);
            Assert.Equal(1, encryptorFactory.NumTimesCreateEncryptorInstanceCalled); // should've been cached
        }

        [Fact]
        public void DefaultKeyId_Prop()
        {
            // Arrange
            var key1 = new MyKey();
            var key2 = new MyKey();
            var encryptorFactory = new MyEncryptorFactory();

            // Act
            var keyRing = new KeyRing(key2, new[] { key1, key2 }, new[] { encryptorFactory });

            // Assert
            Assert.Equal(key2.KeyId, keyRing.DefaultKeyId);
        }

        [Fact]
        public void DefaultKeyIdAndEncryptor_IfDefaultKeyNotPresentInAllKeys()
        {
            // Arrange
            var key1 = new MyKey();
            var key2 = new MyKey();
            var key3 = new MyKey();
            var encryptorFactory = new MyEncryptorFactory(expectedEncryptorInstance: new Mock<IAuthenticatedEncryptor>().Object);

            // Act
            var keyRing = new KeyRing(key3, new[] { key1, key2 }, new[] { encryptorFactory });

            // Assert
            bool unused;
            Assert.Equal(key3.KeyId, keyRing.DefaultKeyId);
            Assert.Equal(encryptorFactory.CreateEncryptorInstance(key3), keyRing.GetAuthenticatedEncryptorByKeyId(key3.KeyId, out unused));
        }

        [Fact]
        public void GetAuthenticatedEncryptorByKeyId_DefersInstantiation_AndReturnsRevocationInfo()
        {
            // Arrange
            var expectedEncryptorInstance1 = new Mock<IAuthenticatedEncryptor>().Object;
            var expectedEncryptorInstance2 = new Mock<IAuthenticatedEncryptor>().Object;

            var key1 = new MyKey(isRevoked: true);
            var key2 = new MyKey();

            var encryptorFactory1 = new MyEncryptorFactory(expectedEncryptorInstance: expectedEncryptorInstance1, associatedKey: key1);
            var encryptorFactory2 = new MyEncryptorFactory(expectedEncryptorInstance: expectedEncryptorInstance2, associatedKey: key2);

            // Act
            var keyRing = new KeyRing(key2, new[] { key1, key2 }, new[] { encryptorFactory1, encryptorFactory2 });

            // Assert
            bool isRevoked;
            Assert.Equal(0, encryptorFactory1.NumTimesCreateEncryptorInstanceCalled);
            Assert.Same(expectedEncryptorInstance1, keyRing.GetAuthenticatedEncryptorByKeyId(key1.KeyId, out isRevoked));
            Assert.True(isRevoked);
            Assert.Equal(1, encryptorFactory1.NumTimesCreateEncryptorInstanceCalled);
            Assert.Same(expectedEncryptorInstance1, keyRing.GetAuthenticatedEncryptorByKeyId(key1.KeyId, out isRevoked));
            Assert.True(isRevoked);
            Assert.Equal(1, encryptorFactory1.NumTimesCreateEncryptorInstanceCalled);
            Assert.Equal(0, encryptorFactory2.NumTimesCreateEncryptorInstanceCalled);
            Assert.Same(expectedEncryptorInstance2, keyRing.GetAuthenticatedEncryptorByKeyId(key2.KeyId, out isRevoked));
            Assert.False(isRevoked);
            Assert.Equal(1, encryptorFactory2.NumTimesCreateEncryptorInstanceCalled);
            Assert.Same(expectedEncryptorInstance2, keyRing.GetAuthenticatedEncryptorByKeyId(key2.KeyId, out isRevoked));
            Assert.False(isRevoked);
            Assert.Equal(1, encryptorFactory2.NumTimesCreateEncryptorInstanceCalled);
            Assert.Same(expectedEncryptorInstance2, keyRing.DefaultAuthenticatedEncryptor);
            Assert.Equal(1, encryptorFactory2.NumTimesCreateEncryptorInstanceCalled);
        }

        private sealed class MyKey : IKey
        {
            public MyKey(bool isRevoked = false)
            {
                CreationDate = DateTimeOffset.Now;
                ActivationDate = CreationDate + TimeSpan.FromHours(1);
                ExpirationDate = CreationDate + TimeSpan.FromDays(30);
                IsRevoked = isRevoked;
                KeyId = Guid.NewGuid();
            }

            public DateTimeOffset ActivationDate { get; }
            public DateTimeOffset CreationDate { get; }
            public DateTimeOffset ExpirationDate { get; }
            public bool IsRevoked { get; }
            public Guid KeyId { get; }
            public IAuthenticatedEncryptorDescriptor Descriptor => throw new NotImplementedException();
        }

        private sealed class MyEncryptorFactory : IAuthenticatedEncryptorFactory
        {
            public int NumTimesCreateEncryptorInstanceCalled;
            private IAuthenticatedEncryptor _expectedEncryptorInstance;
            private IKey _associatedKey;

            public MyEncryptorFactory(IAuthenticatedEncryptor expectedEncryptorInstance = null, IKey associatedKey = null)
            {
                _expectedEncryptorInstance = expectedEncryptorInstance;
                _associatedKey = associatedKey;
            }

            public IAuthenticatedEncryptor CreateEncryptorInstance(IKey key)
            {
                if (_associatedKey != null && key != _associatedKey)
                {
                    return null;
                }

                NumTimesCreateEncryptorInstanceCalled++;

                return _expectedEncryptorInstance ?? new Mock<IAuthenticatedEncryptor>().Object;
            }
        }
    }
}

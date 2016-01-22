// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement
{
    public class DeferredKeyTests
    {
        [Fact]
        public void Ctor_Properties()
        {
            // Arrange
            var keyId = Guid.NewGuid();
            var creationDate = DateTimeOffset.Now;
            var activationDate = creationDate.AddDays(2);
            var expirationDate = creationDate.AddDays(90);

            // Act
            var key = new DeferredKey(keyId, creationDate, activationDate, expirationDate, new Mock<IInternalXmlKeyManager>().Object, XElement.Parse(@"<node />"));

            // Assert
            Assert.Equal(keyId, key.KeyId);
            Assert.Equal(creationDate, key.CreationDate);
            Assert.Equal(activationDate, key.ActivationDate);
            Assert.Equal(expirationDate, key.ExpirationDate);
        }

        [Fact]
        public void SetRevoked_Respected()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            var key = new DeferredKey(Guid.Empty, now, now, now, new Mock<IInternalXmlKeyManager>().Object, XElement.Parse(@"<node />"));

            // Act & assert
            Assert.False(key.IsRevoked);
            key.SetRevoked();
            Assert.True(key.IsRevoked);
        }

        [Fact]
        public void CreateEncryptorInstance_Success()
        {
            // Arrange
            var expectedEncryptor = new Mock<IAuthenticatedEncryptor>().Object;
            var mockDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>();
            mockDescriptor.Setup(o => o.CreateEncryptorInstance()).Returns(expectedEncryptor);
            var mockKeyManager = new Mock<IInternalXmlKeyManager>();
            mockKeyManager.Setup(o => o.DeserializeDescriptorFromKeyElement(It.IsAny<XElement>()))
                .Returns<XElement>(element =>
                {
                    XmlAssert.Equal(@"<node />", element);
                    return mockDescriptor.Object;
                });

            var now = DateTimeOffset.UtcNow;
            var key = new DeferredKey(Guid.Empty, now, now, now, mockKeyManager.Object, XElement.Parse(@"<node />"));

            // Act
            var actual = key.CreateEncryptorInstance();

            // Assert
            Assert.Same(expectedEncryptor, actual);
        }

        [Fact]
        public void CreateEncryptorInstance_CachesFailures()
        {
            // Arrange
            int numTimesCalled = 0;
            var mockKeyManager = new Mock<IInternalXmlKeyManager>();
            mockKeyManager.Setup(o => o.DeserializeDescriptorFromKeyElement(It.IsAny<XElement>()))
                .Returns<XElement>(element =>
                {
                    numTimesCalled++;
                    throw new Exception("How exceptional.");
                });

            var now = DateTimeOffset.UtcNow;
            var key = new DeferredKey(Guid.Empty, now, now, now, mockKeyManager.Object, XElement.Parse(@"<node />"));

            // Act & assert
            ExceptionAssert.Throws<Exception>(() => key.CreateEncryptorInstance(), "How exceptional.");
            ExceptionAssert.Throws<Exception>(() => key.CreateEncryptorInstance(), "How exceptional.");
            Assert.Equal(1, numTimesCalled);
        }
    }
}

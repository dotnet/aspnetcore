// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Moq;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

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
        var mockDescriptor = Mock.Of<IAuthenticatedEncryptorDescriptor>();
        var mockInternalKeyManager = new Mock<IInternalXmlKeyManager>();
        mockInternalKeyManager.Setup(o => o.DeserializeDescriptorFromKeyElement(It.IsAny<XElement>()))
            .Returns<XElement>(element =>
            {
                XmlAssert.Equal(@"<node />", element);
                return mockDescriptor;
            });
        var encryptorFactory = Mock.Of<IAuthenticatedEncryptorFactory>();

        // Act
        var key = new Key(keyId, creationDate, activationDate, expirationDate, mockInternalKeyManager.Object, XElement.Parse(@"<node />"), new[] { encryptorFactory });

        // Assert
        Assert.Equal(keyId, key.KeyId);
        Assert.Equal(creationDate, key.CreationDate);
        Assert.Equal(activationDate, key.ActivationDate);
        Assert.Equal(expirationDate, key.ExpirationDate);
        Assert.Same(mockDescriptor, key.Descriptor);
    }

    [Fact]
    public void SetRevoked_Respected()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var encryptorFactory = Mock.Of<IAuthenticatedEncryptorFactory>();
        var key = new Key(Guid.Empty, now, now, now, new Mock<IInternalXmlKeyManager>().Object, XElement.Parse(@"<node />"), new[] { encryptorFactory });

        // Act & assert
        Assert.False(key.IsRevoked);
        key.SetRevoked();
        Assert.True(key.IsRevoked);
    }

    [Fact]
    public void Get_Descriptor_CachesFailures()
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
        var encryptorFactory = Mock.Of<IAuthenticatedEncryptorFactory>();
        var key = new Key(Guid.Empty, now, now, now, mockKeyManager.Object, XElement.Parse(@"<node />"), new[] { encryptorFactory });

        // Act & assert
        ExceptionAssert.Throws<Exception>(() => key.Descriptor, "How exceptional.");
        ExceptionAssert.Throws<Exception>(() => key.Descriptor, "How exceptional.");
        Assert.Equal(1, numTimesCalled);
    }
}

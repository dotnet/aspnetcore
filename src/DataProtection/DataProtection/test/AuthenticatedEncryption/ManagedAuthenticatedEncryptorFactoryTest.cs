// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Managed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;

public class ManagedAuthenticatedEncryptorFactoryTest
{
    [Fact]
    public void CreateEncrptorInstance_UnknownDescriptorType_ReturnsNull()
    {
        // Arrange
        var key = new Mock<IKey>();
        key.Setup(k => k.Descriptor).Returns(new Mock<IAuthenticatedEncryptorDescriptor>().Object);

        var factory = new ManagedAuthenticatedEncryptorFactory(NullLoggerFactory.Instance);

        // Act
        var encryptor = factory.CreateEncryptorInstance(key.Object);

        // Assert
        Assert.Null(encryptor);
    }

    [Fact]
    public void CreateEncrptorInstance_ExpectedDescriptorType_ReturnsEncryptor()
    {
        // Arrange
        var descriptor = new ManagedAuthenticatedEncryptorConfiguration().CreateNewDescriptor();
        var key = new Mock<IKey>();
        key.Setup(k => k.Descriptor).Returns(descriptor);

        var factory = new ManagedAuthenticatedEncryptorFactory(NullLoggerFactory.Instance);

        // Act
        var encryptor = factory.CreateEncryptorInstance(key.Object);

        // Assert
        Assert.NotNull(encryptor);
        Assert.IsType<ManagedAuthenticatedEncryptor>(encryptor);
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.Cng;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Test.Shared;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption
{
    public class CngCbcAuthenticatedEncryptorFactoryTest
    {
        [Fact]
        public void CreateEncrptorInstance_UnknownDescriptorType_ReturnsNull()
        {
            // Arrange
            var key = new Mock<IKey>();
            key.Setup(k => k.Descriptor).Returns(new Mock<IAuthenticatedEncryptorDescriptor>().Object);

            var factory = new CngCbcAuthenticatedEncryptorFactory(NullLoggerFactory.Instance);

            // Act
            var encryptor = factory.CreateEncryptorInstance(key.Object);

            // Assert
            Assert.Null(encryptor);
        }

        [ConditionalFact]
        [ConditionalRunTestOnlyOnWindows]
        public void CreateEncrptorInstance_ExpectedDescriptorType_ReturnsEncryptor()
        {
            // Arrange
            var descriptor = new CngCbcAuthenticatedEncryptorConfiguration().CreateNewDescriptor();
            var key = new Mock<IKey>();
            key.Setup(k => k.Descriptor).Returns(descriptor);

            var factory = new CngCbcAuthenticatedEncryptorFactory(NullLoggerFactory.Instance);

            // Act
            var encryptor = factory.CreateEncryptorInstance(key.Object);

            // Assert
            Assert.NotNull(encryptor);
            Assert.IsType<CbcAuthenticatedEncryptor>(encryptor);
        }
    }
}

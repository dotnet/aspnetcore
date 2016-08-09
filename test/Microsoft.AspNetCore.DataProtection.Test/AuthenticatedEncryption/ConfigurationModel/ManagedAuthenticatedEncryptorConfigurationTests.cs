// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    public class ManagedAuthenticatedEncryptorConfigurationTests
    {
        [Fact]
        public void CreateNewDescriptor_CreatesUniqueCorrectlySizedMasterKey()
        {
            // Arrange
            var configuration = new ManagedAuthenticatedEncryptorConfiguration();

            // Act
            var masterKey1 = ((ManagedAuthenticatedEncryptorDescriptor)configuration.CreateNewDescriptor()).MasterKey;
            var masterKey2 = ((ManagedAuthenticatedEncryptorDescriptor)configuration.CreateNewDescriptor()).MasterKey;

            // Assert
            SecretAssert.NotEqual(masterKey1, masterKey2);
            SecretAssert.LengthIs(512 /* bits */, masterKey1);
            SecretAssert.LengthIs(512 /* bits */, masterKey2);
        }

        [Fact]
        public void CreateNewDescriptor_PropagatesOptions()
        {
            // Arrange
            var configuration = new ManagedAuthenticatedEncryptorConfiguration();

            // Act
            var descriptor = (ManagedAuthenticatedEncryptorDescriptor)configuration.CreateNewDescriptor();

            // Assert
            Assert.Equal(configuration, descriptor.Configuration);
        }
    }
}

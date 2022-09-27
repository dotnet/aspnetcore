// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

public class CngGcmAuthenticatedEncryptorConfigurationTests
{
    [Fact]
    public void CreateNewDescriptor_CreatesUniqueCorrectlySizedMasterKey()
    {
        // Arrange
        var configuration = new CngGcmAuthenticatedEncryptorConfiguration();

        // Act
        var masterKey1 = ((CngGcmAuthenticatedEncryptorDescriptor)configuration.CreateNewDescriptor()).MasterKey;
        var masterKey2 = ((CngGcmAuthenticatedEncryptorDescriptor)configuration.CreateNewDescriptor()).MasterKey;

        // Assert
        SecretAssert.NotEqual(masterKey1, masterKey2);
        SecretAssert.LengthIs(512 /* bits */, masterKey1);
        SecretAssert.LengthIs(512 /* bits */, masterKey2);
    }

    [Fact]
    public void CreateNewDescriptor_PropagatesOptions()
    {
        // Arrange
        var configuration = new CngGcmAuthenticatedEncryptorConfiguration();

        // Act
        var descriptor = (CngGcmAuthenticatedEncryptorDescriptor)configuration.CreateNewDescriptor();

        // Assert
        Assert.Equal(configuration, descriptor.Configuration);
    }
}

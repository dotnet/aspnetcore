// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

public class CngCbcAuthenticatedEncryptorConfigurationTests
{
    [Fact]
    public void CreateNewDescriptor_CreatesUniqueCorrectlySizedMasterKey()
    {
        // Arrange
        var configuration = new CngCbcAuthenticatedEncryptorConfiguration();

        // Act
        var masterKey1 = ((CngCbcAuthenticatedEncryptorDescriptor)configuration.CreateNewDescriptor()).MasterKey;
        var masterKey2 = ((CngCbcAuthenticatedEncryptorDescriptor)configuration.CreateNewDescriptor()).MasterKey;

        // Assert
        SecretAssert.NotEqual(masterKey1, masterKey2);
        SecretAssert.LengthIs(512 /* bits */, masterKey1);
        SecretAssert.LengthIs(512 /* bits */, masterKey2);
    }

    [Fact]
    public void CreateNewDescriptor_PropagatesOptions()
    {
        // Arrange
        var configuration = new CngCbcAuthenticatedEncryptorConfiguration();

        // Act
        var descriptor = (CngCbcAuthenticatedEncryptorDescriptor)configuration.CreateNewDescriptor();

        // Assert
        Assert.Equal(configuration, descriptor.Configuration);
    }
}

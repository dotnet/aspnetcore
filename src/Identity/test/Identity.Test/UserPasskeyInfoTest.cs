// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Test;

public class UserPasskeyInfoTest
{
    [Fact]
    public void Ctor_WithName_SetsAllProperties()
    {
        // Arrange
        var credentialId = new byte[] { 1, 2, 3 };
        var publicKey = new byte[] { 4, 5, 6 };
        var createdAt = DateTimeOffset.UtcNow;
        var transports = new[] { "usb", "nfc" };
        var attestationObject = new byte[] { 7, 8, 9 };
        var clientDataJson = new byte[] { 10, 11 };
        var name = "My Security Key";

        // Act
        var passkey = new UserPasskeyInfo(
            credentialId,
            publicKey,
            createdAt,
            signCount: 42,
            transports,
            isUserVerified: true,
            isBackupEligible: true,
            isBackedUp: false,
            attestationObject,
            clientDataJson,
            name);

        // Assert
        Assert.Equal(credentialId, passkey.CredentialId);
        Assert.Equal(publicKey, passkey.PublicKey);
        Assert.Equal(createdAt, passkey.CreatedAt);
        Assert.Equal(42u, passkey.SignCount);
        Assert.Equal(transports, passkey.Transports);
        Assert.True(passkey.IsUserVerified);
        Assert.True(passkey.IsBackupEligible);
        Assert.False(passkey.IsBackedUp);
        Assert.Equal(attestationObject, passkey.AttestationObject);
        Assert.Equal(clientDataJson, passkey.ClientDataJson);
        Assert.Equal("My Security Key", passkey.Name);
    }

    [Fact]
    public void Ctor_WithNullName_LeavesNameNull()
    {
        var passkey = new UserPasskeyInfo(
            new byte[] { 1 },
            new byte[] { 2 },
            DateTimeOffset.UtcNow,
            signCount: 0,
            transports: null,
            isUserVerified: false,
            isBackupEligible: false,
            isBackedUp: false,
            new byte[] { 3 },
            new byte[] { 4 },
            name: null);

        Assert.Null(passkey.Name);
    }

    [Fact]
    public void Ctor_WithoutName_LeavesNameNull()
    {
        // The original constructor should still work and leave Name unset.
        var passkey = new UserPasskeyInfo(
            new byte[] { 1 },
            new byte[] { 2 },
            DateTimeOffset.UtcNow,
            signCount: 0,
            transports: null,
            isUserVerified: false,
            isBackupEligible: false,
            isBackedUp: false,
            new byte[] { 3 },
            new byte[] { 4 });

        Assert.Null(passkey.Name);
    }
}

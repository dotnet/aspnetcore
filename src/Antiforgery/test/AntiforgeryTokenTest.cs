// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery.Internal;

public class AntiforgeryTokenTest
{
    [Fact]
    public void AdditionalDataProperty()
    {
        // Arrange
        var token = new AntiforgeryToken();

        // Act & assert - 1
        Assert.Equal("", token.AdditionalData);

        // Act & assert - 2
        token.AdditionalData = "additional data";
        Assert.Equal("additional data", token.AdditionalData);

        // Act & assert - 3
        token.AdditionalData = null!;
        Assert.Equal("", token.AdditionalData);
    }

    [Fact]
    public void ClaimUidProperty()
    {
        // Arrange
        var token = new AntiforgeryToken();

        // Act & assert - 1
        Assert.Null(token.ClaimUid);

        // Act & assert - 2
        BinaryBlob blob = new BinaryBlob(32);
        token.ClaimUid = blob;
        Assert.Equal(blob, token.ClaimUid);

        // Act & assert - 3
        token.ClaimUid = null;
        Assert.Null(token.ClaimUid);
    }

    [Fact]
    public void IsCookieTokenProperty()
    {
        // Arrange
        var token = new AntiforgeryToken();

        // Act & assert - 1
        Assert.False(token.IsCookieToken);

        // Act & assert - 2
        token.IsCookieToken = true;
        Assert.True(token.IsCookieToken);

        // Act & assert - 3
        token.IsCookieToken = false;
        Assert.False(token.IsCookieToken);
    }

    [Fact]
    public void UsernameProperty()
    {
        // Arrange
        var token = new AntiforgeryToken();

        // Act & assert - 1
        Assert.Equal("", token.Username);

        // Act & assert - 2
        token.Username = "my username";
        Assert.Equal("my username", token.Username);

        // Act & assert - 3
        token.Username = null;
        Assert.Equal("", token.Username);
    }

    [Fact]
    public void SecurityTokenProperty_GetsAutopopulated()
    {
        // Arrange
        var token = new AntiforgeryToken();

        // Act
        var securityToken = token.SecurityToken;

        // Assert
        Assert.NotNull(securityToken);
        Assert.Equal(AntiforgeryToken.SecurityTokenBitLength, securityToken!.BitLength);

        // check that we're not making a new one each property call
        Assert.Equal(securityToken, token.SecurityToken);
    }

    [Fact]
    public void SecurityTokenProperty_PropertySetter_DoesNotUseDefaults()
    {
        // Arrange
        var token = new AntiforgeryToken();

        // Act
        var securityToken = new BinaryBlob(64);
        token.SecurityToken = securityToken;

        // Assert
        Assert.Equal(securityToken, token.SecurityToken);
    }

    [Fact]
    public void SecurityTokenProperty_PropertySetter_DoesNotAllowNulls()
    {
        // Arrange
        var token = new AntiforgeryToken();

        // Act
        token.SecurityToken = null;
        var securityToken = token.SecurityToken;

        // Assert
        Assert.NotNull(securityToken);
        Assert.Equal(AntiforgeryToken.SecurityTokenBitLength, securityToken!.BitLength);

        // check that we're not making a new one each property call
        Assert.Equal(securityToken, token.SecurityToken);
    }
}

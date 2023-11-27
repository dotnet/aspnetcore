// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Moq;

namespace Microsoft.AspNetCore.Antiforgery.Internal;

public class DefaultAntiforgeryTokenGeneratorProviderTest
{
    [Fact]
    public void GenerateCookieToken()
    {
        // Arrange
        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: null,
            additionalDataProvider: null);

        // Act
        var token = tokenProvider.GenerateCookieToken();

        // Assert
        Assert.NotNull(token);
    }

    [Fact]
    public void GenerateRequestToken_InvalidCookieToken()
    {
        // Arrange
        var cookieToken = new AntiforgeryToken() { IsCookieToken = false };
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        Assert.False(httpContext.User.Identity.IsAuthenticated);

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: null,
            additionalDataProvider: null);

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => tokenProvider.GenerateRequestToken(httpContext, cookieToken),
            "cookieToken",
            "The antiforgery cookie token is invalid.");
    }

    [Fact]
    public void GenerateRequestToken_AnonymousUser()
    {
        // Arrange
        var cookieToken = new AntiforgeryToken() { IsCookieToken = true };
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        Assert.False(httpContext.User.Identity.IsAuthenticated);

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: null,
            additionalDataProvider: null);

        // Act
        var fieldToken = tokenProvider.GenerateRequestToken(httpContext, cookieToken);

        // Assert
        Assert.NotNull(fieldToken);
        Assert.Equal(cookieToken.SecurityToken, fieldToken.SecurityToken);
        Assert.False(fieldToken.IsCookieToken);
        Assert.Empty(fieldToken.Username);
        Assert.Null(fieldToken.ClaimUid);
        Assert.Empty(fieldToken.AdditionalData);
    }

    [Fact]
    public void GenerateRequestToken_AuthenticatedWithoutUsernameAndNoAdditionalData_NoAdditionalData()
    {
        // Arrange
        var cookieToken = new AntiforgeryToken()
        {
            IsCookieToken = true
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new MyAuthenticatedIdentityWithoutUsername());

        var options = new AntiforgeryOptions();
        var claimUidExtractor = new Mock<IClaimUidExtractor>().Object;

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: claimUidExtractor,
            additionalDataProvider: null);

        // Act & assert
        var exception = Assert.Throws<InvalidOperationException>(
                () => tokenProvider.GenerateRequestToken(httpContext, cookieToken));
        Assert.Equal(
            "The provided identity of type " +
            $"'{typeof(MyAuthenticatedIdentityWithoutUsername).FullName}' " +
            "is marked IsAuthenticated = true but does not have a value for Name. " +
            "By default, the antiforgery system requires that all authenticated identities have a unique Name. " +
            "If it is not possible to provide a unique Name for this identity, " +
            "consider extending IAntiforgeryAdditionalDataProvider by overriding the " +
            "DefaultAntiforgeryAdditionalDataProvider " +
            "or a custom type that can provide some form of unique identifier for the current user.",
            exception.Message);
    }

    [Fact]
    public void GenerateRequestToken_AuthenticatedWithoutUsername_WithAdditionalData()
    {
        // Arrange
        var cookieToken = new AntiforgeryToken() { IsCookieToken = true };

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new MyAuthenticatedIdentityWithoutUsername());

        var mockAdditionalDataProvider = new Mock<IAntiforgeryAdditionalDataProvider>();
        mockAdditionalDataProvider.Setup(o => o.GetAdditionalData(httpContext))
                                  .Returns("additional-data");

        var claimUidExtractor = new Mock<IClaimUidExtractor>().Object;

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: claimUidExtractor,
            additionalDataProvider: mockAdditionalDataProvider.Object);

        // Act
        var fieldToken = tokenProvider.GenerateRequestToken(httpContext, cookieToken);

        // Assert
        Assert.NotNull(fieldToken);
        Assert.Equal(cookieToken.SecurityToken, fieldToken.SecurityToken);
        Assert.False(fieldToken.IsCookieToken);
        Assert.Empty(fieldToken.Username);
        Assert.Null(fieldToken.ClaimUid);
        Assert.Equal("additional-data", fieldToken.AdditionalData);
    }

    [Fact]
    public void GenerateRequestToken_ClaimsBasedIdentity()
    {
        // Arrange
        var cookieToken = new AntiforgeryToken() { IsCookieToken = true };

        var identity = GetAuthenticatedIdentity("some-identity");
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(identity);

        byte[] data = new byte[256 / 8];
        RandomNumberGenerator.Fill(data);
        var base64ClaimUId = Convert.ToBase64String(data);
        var expectedClaimUid = new BinaryBlob(256, data);

        var mockClaimUidExtractor = new Mock<IClaimUidExtractor>();
        mockClaimUidExtractor.Setup(o => o.ExtractClaimUid(It.Is<ClaimsPrincipal>(c => c.Identity == identity)))
                             .Returns(base64ClaimUId);

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: mockClaimUidExtractor.Object,
            additionalDataProvider: null);

        // Act
        var fieldToken = tokenProvider.GenerateRequestToken(httpContext, cookieToken);

        // Assert
        Assert.NotNull(fieldToken);
        Assert.Equal(cookieToken.SecurityToken, fieldToken.SecurityToken);
        Assert.False(fieldToken.IsCookieToken);
        Assert.Equal("", fieldToken.Username);
        Assert.Equal(expectedClaimUid, fieldToken.ClaimUid);
        Assert.Equal("", fieldToken.AdditionalData);
    }

    [Fact]
    public void GenerateRequestToken_RegularUserWithUsername()
    {
        // Arrange
        var cookieToken = new AntiforgeryToken() { IsCookieToken = true };

        var httpContext = new DefaultHttpContext();
        var mockIdentity = new Mock<ClaimsIdentity>();
        mockIdentity.Setup(o => o.IsAuthenticated)
                    .Returns(true);
        mockIdentity.Setup(o => o.Name)
                    .Returns("my-username");

        httpContext.User = new ClaimsPrincipal(mockIdentity.Object);

        var claimUidExtractor = new Mock<IClaimUidExtractor>().Object;

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: claimUidExtractor,
            additionalDataProvider: null);

        // Act
        var fieldToken = tokenProvider.GenerateRequestToken(httpContext, cookieToken);

        // Assert
        Assert.NotNull(fieldToken);
        Assert.Equal(cookieToken.SecurityToken, fieldToken.SecurityToken);
        Assert.False(fieldToken.IsCookieToken);
        Assert.Equal("my-username", fieldToken.Username);
        Assert.Null(fieldToken.ClaimUid);
        Assert.Empty(fieldToken.AdditionalData);
    }

    [Fact]
    public void IsCookieTokenValid_FieldToken_ReturnsFalse()
    {
        // Arrange
        var cookieToken = new AntiforgeryToken()
        {
            IsCookieToken = false
        };

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: null,
            additionalDataProvider: null);

        // Act
        var isValid = tokenProvider.IsCookieTokenValid(cookieToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsCookieTokenValid_NullToken_ReturnsFalse()
    {
        // Arrange
        AntiforgeryToken cookieToken = null;
        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: null,
            additionalDataProvider: null);

        // Act
        var isValid = tokenProvider.IsCookieTokenValid(cookieToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsCookieTokenValid_ValidToken_ReturnsTrue()
    {
        // Arrange
        var cookieToken = new AntiforgeryToken()
        {
            IsCookieToken = true
        };

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: null,
            additionalDataProvider: null);

        // Act
        var isValid = tokenProvider.IsCookieTokenValid(cookieToken);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void TryValidateTokenSet_CookieTokenMissing()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var fieldtoken = new AntiforgeryToken() { IsCookieToken = false };

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: null,
            additionalDataProvider: null);

        // Act & Assert
        string message;
        var ex = Assert.Throws<ArgumentNullException>(
            () => tokenProvider.TryValidateTokenSet(httpContext, null, fieldtoken, out message));

        Assert.StartsWith(@"The required antiforgery cookie token must be provided.", ex.Message);
    }

    [Fact]
    public void TryValidateTokenSet_FieldTokenMissing()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var cookieToken = new AntiforgeryToken() { IsCookieToken = true };

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: null,
            additionalDataProvider: null);

        // Act & Assert
        string message;
        var ex = Assert.Throws<ArgumentNullException>(
            () => tokenProvider.TryValidateTokenSet(httpContext, cookieToken, null, out message));

        Assert.StartsWith("The required antiforgery request token must be provided.", ex.Message);
    }

    [Fact]
    public void TryValidateTokenSet_FieldAndCookieTokensSwapped_FieldTokenDuplicated()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var cookieToken = new AntiforgeryToken() { IsCookieToken = true };
        var fieldtoken = new AntiforgeryToken() { IsCookieToken = false };

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: null,
            additionalDataProvider: null);

        string expectedMessage =
            "Validation of the provided antiforgery token failed. " +
            "The cookie token and the request token were swapped.";

        // Act
        string message;
        var result = tokenProvider.TryValidateTokenSet(httpContext, fieldtoken, fieldtoken, out message);

        // Assert
        Assert.False(result);
        Assert.Equal(expectedMessage, message);
    }

    [Fact]
    public void TryValidateTokenSet_FieldAndCookieTokensSwapped_CookieDuplicated()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var cookieToken = new AntiforgeryToken() { IsCookieToken = true };
        var fieldtoken = new AntiforgeryToken() { IsCookieToken = false };

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: null,
            additionalDataProvider: null);

        string expectedMessage =
            "Validation of the provided antiforgery token failed. " +
            "The cookie token and the request token were swapped.";

        // Act
        string message;
        var result = tokenProvider.TryValidateTokenSet(httpContext, cookieToken, cookieToken, out message);

        // Assert
        Assert.False(result);
        Assert.Equal(expectedMessage, message);
    }

    [Fact]
    public void TryValidateTokenSet_FieldAndCookieTokensHaveDifferentSecurityKeys()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var cookieToken = new AntiforgeryToken() { IsCookieToken = true };
        var fieldtoken = new AntiforgeryToken() { IsCookieToken = false };

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: null,
            additionalDataProvider: null);

        string expectedMessage = "The antiforgery cookie token and request token do not match.";

        // Act
        string message;
        var result = tokenProvider.TryValidateTokenSet(httpContext, cookieToken, fieldtoken, out message);

        // Assert
        Assert.False(result);
        Assert.Equal(expectedMessage, message);
    }

    [Theory]
    [InlineData("the-user", "the-other-user")]
    [InlineData("http://example.com/uri-casing", "http://example.com/URI-casing")]
    [InlineData("https://example.com/secure-uri-casing", "https://example.com/secure-URI-casing")]
    public void TryValidateTokenSet_UsernameMismatch(string identityUsername, string embeddedUsername)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var identity = GetAuthenticatedIdentity(identityUsername);
        httpContext.User = new ClaimsPrincipal(identity);

        var cookieToken = new AntiforgeryToken() { IsCookieToken = true };
        var fieldtoken = new AntiforgeryToken()
        {
            SecurityToken = cookieToken.SecurityToken,
            Username = embeddedUsername,
            IsCookieToken = false
        };

        var mockClaimUidExtractor = new Mock<IClaimUidExtractor>();
        mockClaimUidExtractor.Setup(o => o.ExtractClaimUid(It.Is<ClaimsPrincipal>(c => c.Identity == identity)))
                             .Returns((string)null);

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: mockClaimUidExtractor.Object,
            additionalDataProvider: null);

        string expectedMessage =
            $"The provided antiforgery token was meant for user \"{embeddedUsername}\", " +
            $"but the current user is \"{identityUsername}\".";

        // Act
        string message;
        var result = tokenProvider.TryValidateTokenSet(httpContext, cookieToken, fieldtoken, out message);

        // Assert
        Assert.False(result);
        Assert.Equal(expectedMessage, message);
    }

    [Fact]
    public void TryValidateTokenSet_ClaimUidMismatch()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var identity = GetAuthenticatedIdentity("the-user");
        httpContext.User = new ClaimsPrincipal(identity);

        var cookieToken = new AntiforgeryToken() { IsCookieToken = true };
        var fieldtoken = new AntiforgeryToken()
        {
            SecurityToken = cookieToken.SecurityToken,
            IsCookieToken = false,
            ClaimUid = new BinaryBlob(256)
        };

        var differentToken = new BinaryBlob(256);
        var mockClaimUidExtractor = new Mock<IClaimUidExtractor>();
        mockClaimUidExtractor.Setup(o => o.ExtractClaimUid(It.Is<ClaimsPrincipal>(c => c.Identity == identity)))
                             .Returns(Convert.ToBase64String(differentToken.GetData()));

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: mockClaimUidExtractor.Object,
            additionalDataProvider: null);

        string expectedMessage =
            "The provided antiforgery token was meant for a different " +
            "claims-based user than the current user.";

        // Act
        string message;
        var result = tokenProvider.TryValidateTokenSet(httpContext, cookieToken, fieldtoken, out message);

        // Assert
        Assert.False(result);
        Assert.Equal(expectedMessage, message);
    }

    [Fact]
    public void TryValidateTokenSet_AdditionalDataRejected()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity();
        httpContext.User = new ClaimsPrincipal(identity);

        var cookieToken = new AntiforgeryToken() { IsCookieToken = true };
        var fieldtoken = new AntiforgeryToken()
        {
            SecurityToken = cookieToken.SecurityToken,
            Username = String.Empty,
            IsCookieToken = false,
            AdditionalData = "some-additional-data"
        };

        var mockAdditionalDataProvider = new Mock<IAntiforgeryAdditionalDataProvider>();
        mockAdditionalDataProvider
            .Setup(o => o.ValidateAdditionalData(httpContext, "some-additional-data"))
            .Returns(false);

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: null,
            additionalDataProvider: mockAdditionalDataProvider.Object);

        string expectedMessage = "The provided antiforgery token failed a custom data check.";

        // Act
        string message;
        var result = tokenProvider.TryValidateTokenSet(httpContext, cookieToken, fieldtoken, out message);

        // Assert
        Assert.False(result);
        Assert.Equal(expectedMessage, message);
    }

    [Fact]
    public void TryValidateTokenSet_Success_AnonymousUser()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity();
        httpContext.User = new ClaimsPrincipal(identity);

        var cookieToken = new AntiforgeryToken() { IsCookieToken = true };
        var fieldtoken = new AntiforgeryToken()
        {
            SecurityToken = cookieToken.SecurityToken,
            Username = String.Empty,
            IsCookieToken = false,
            AdditionalData = "some-additional-data"
        };

        var mockAdditionalDataProvider = new Mock<IAntiforgeryAdditionalDataProvider>();
        mockAdditionalDataProvider.Setup(o => o.ValidateAdditionalData(httpContext, "some-additional-data"))
                                  .Returns(true);

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: null,
            additionalDataProvider: mockAdditionalDataProvider.Object);

        // Act
        string message;
        var result = tokenProvider.TryValidateTokenSet(httpContext, cookieToken, fieldtoken, out message);

        // Assert
        Assert.True(result);
        Assert.Null(message);
    }

    [Fact]
    public void TryValidateTokenSet_Success_AuthenticatedUserWithUsername()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var identity = GetAuthenticatedIdentity("the-user");
        httpContext.User = new ClaimsPrincipal(identity);

        var cookieToken = new AntiforgeryToken() { IsCookieToken = true };
        var fieldtoken = new AntiforgeryToken()
        {
            SecurityToken = cookieToken.SecurityToken,
            Username = "THE-USER",
            IsCookieToken = false,
            AdditionalData = "some-additional-data"
        };

        var mockAdditionalDataProvider = new Mock<IAntiforgeryAdditionalDataProvider>();
        mockAdditionalDataProvider.Setup(o => o.ValidateAdditionalData(httpContext, "some-additional-data"))
                                  .Returns(true);

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: new Mock<IClaimUidExtractor>().Object,
            additionalDataProvider: mockAdditionalDataProvider.Object);

        // Act
        string message;
        var result = tokenProvider.TryValidateTokenSet(httpContext, cookieToken, fieldtoken, out message);

        // Assert
        Assert.True(result);
        Assert.Null(message);
    }

    [Fact]
    public void TryValidateTokenSet_Success_ClaimsBasedUser()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var identity = GetAuthenticatedIdentity("the-user");
        httpContext.User = new ClaimsPrincipal(identity);

        var cookieToken = new AntiforgeryToken() { IsCookieToken = true };
        var fieldtoken = new AntiforgeryToken()
        {
            SecurityToken = cookieToken.SecurityToken,
            IsCookieToken = false,
            ClaimUid = new BinaryBlob(256)
        };

        var mockClaimUidExtractor = new Mock<IClaimUidExtractor>();
        mockClaimUidExtractor.Setup(o => o.ExtractClaimUid(It.Is<ClaimsPrincipal>(c => c.Identity == identity)))
                             .Returns(Convert.ToBase64String(fieldtoken.ClaimUid.GetData()));

        var tokenProvider = new DefaultAntiforgeryTokenGenerator(
            claimUidExtractor: mockClaimUidExtractor.Object,
            additionalDataProvider: null);

        // Act
        string message;
        var result = tokenProvider.TryValidateTokenSet(httpContext, cookieToken, fieldtoken, out message);

        // Assert
        Assert.True(result);
        Assert.Null(message);
    }

    private static ClaimsIdentity GetAuthenticatedIdentity(string identityUsername)
    {
        var claim = new Claim(ClaimsIdentity.DefaultNameClaimType, identityUsername);
        return new ClaimsIdentity(new[] { claim }, "Some-Authentication");
    }

    private sealed class MyAuthenticatedIdentityWithoutUsername : ClaimsIdentity
    {
        public override bool IsAuthenticated
        {
            get { return true; }
        }

        public override string Name
        {
            get { return String.Empty; }
        }
    }
}
#nullable restore

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.Extensions.ObjectPool;
using Moq;

namespace Microsoft.AspNetCore.Antiforgery.Internal;

public class DefaultClaimUidExtractorTest
{
    private static readonly ObjectPool<AntiforgerySerializationContext> _pool =
        new DefaultObjectPoolProvider().Create(new AntiforgerySerializationContextPooledObjectPolicy());

    [Fact]
    public void ExtractClaimUid_Unauthenticated()
    {
        // Arrange
        var extractor = new DefaultClaimUidExtractor(_pool);

        var mockIdentity = new Mock<ClaimsIdentity>();
        mockIdentity.Setup(o => o.IsAuthenticated)
                    .Returns(false);

        // Act
        var claimUid = extractor.ExtractClaimUid(new ClaimsPrincipal(mockIdentity.Object));

        // Assert
        Assert.Null(claimUid);
    }

    [Fact]
    public void ExtractClaimUid_ClaimsIdentity()
    {
        // Arrange
        var mockIdentity = new Mock<ClaimsIdentity>();
        mockIdentity.Setup(o => o.IsAuthenticated)
                    .Returns(true);
        mockIdentity.Setup(o => o.Claims).Returns(new Claim[] { new Claim(ClaimTypes.Name, "someName") });

        var extractor = new DefaultClaimUidExtractor(_pool);

        // Act
        var claimUid = extractor.ExtractClaimUid(new ClaimsPrincipal(mockIdentity.Object));

        // Assert
        Assert.NotNull(claimUid);
        Assert.Equal("yhXE+2v4zSXHtRHmzm4cmrhZca2J0g7yTUwtUerdeF4=", claimUid);
    }

    [Fact]
    public void DefaultUniqueClaimTypes_NotPresent_SerializesAllClaimTypes()
    {
        var identity = new ClaimsIdentity("someAuthentication");
        identity.AddClaim(new Claim(ClaimTypes.Email, "someone@antiforgery.com"));
        identity.AddClaim(new Claim(ClaimTypes.GivenName, "some"));
        identity.AddClaim(new Claim(ClaimTypes.Surname, "one"));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, string.Empty));

        // Arrange
        var claimsIdentity = (ClaimsIdentity)identity;

        // Act
        var identiferParameters = DefaultClaimUidExtractor.GetUniqueIdentifierParameters(new ClaimsIdentity[] { claimsIdentity })!
                                                          .ToArray();
        var claims = claimsIdentity.Claims.ToList();
        claims.Sort((a, b) => string.Compare(a.Type, b.Type, StringComparison.Ordinal));

        // Assert
        int index = 0;
        foreach (var claim in claims)
        {
            Assert.Equal(identiferParameters[index++], claim.Type);
            Assert.Equal(identiferParameters[index++], claim.Value);
            Assert.Equal(identiferParameters[index++], claim.Issuer);
        }
    }

    [Fact]
    public void DefaultUniqueClaimTypes_Present()
    {
        // Arrange
        var identity = new ClaimsIdentity("someAuthentication");
        identity.AddClaim(new Claim("fooClaim", "fooClaimValue"));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "nameIdentifierValue"));

        // Act
        var uniqueIdentifierParameters = DefaultClaimUidExtractor.GetUniqueIdentifierParameters(new ClaimsIdentity[] { identity });

        // Assert
        Assert.Equal(new string[]
        {
                ClaimTypes.NameIdentifier,
                "nameIdentifierValue",
                "LOCAL AUTHORITY",
        }, uniqueIdentifierParameters);
    }

    [Fact]
    public void GetUniqueIdentifierParameters_PrefersSubClaimOverNameIdentifierAndUpn()
    {
        // Arrange
        var identity = new ClaimsIdentity("someAuthentication");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "nameIdentifierValue"));
        identity.AddClaim(new Claim("sub", "subClaimValue"));
        identity.AddClaim(new Claim(ClaimTypes.Upn, "upnClaimValue"));

        // Act
        var uniqueIdentifierParameters = DefaultClaimUidExtractor.GetUniqueIdentifierParameters(new ClaimsIdentity[] { identity });

        // Assert
        Assert.Equal(new string[]
        {
                "sub",
                "subClaimValue",
                "LOCAL AUTHORITY",
        }, uniqueIdentifierParameters);
    }

    [Fact]
    public void GetUniqueIdentifierParameters_PrefersNameIdentifierOverUpn()
    {
        // Arrange
        var identity = new ClaimsIdentity("someAuthentication");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "nameIdentifierValue"));
        identity.AddClaim(new Claim(ClaimTypes.Upn, "upnClaimValue"));

        // Act
        var uniqueIdentifierParameters = DefaultClaimUidExtractor.GetUniqueIdentifierParameters(new ClaimsIdentity[] { identity });

        // Assert
        Assert.Equal(new string[]
        {
                ClaimTypes.NameIdentifier,
                "nameIdentifierValue",
                "LOCAL AUTHORITY",
        }, uniqueIdentifierParameters);
    }

    [Fact]
    public void GetUniqueIdentifierParameters_UsesUpnIfPresent()
    {
        // Arrange
        var identity = new ClaimsIdentity("someAuthentication");
        identity.AddClaim(new Claim("fooClaim", "fooClaimValue"));
        identity.AddClaim(new Claim(ClaimTypes.Upn, "upnClaimValue"));

        // Act
        var uniqueIdentifierParameters = DefaultClaimUidExtractor.GetUniqueIdentifierParameters(new ClaimsIdentity[] { identity });

        // Assert
        Assert.Equal(new string[]
        {
                ClaimTypes.Upn,
                "upnClaimValue",
                "LOCAL AUTHORITY",
        }, uniqueIdentifierParameters);
    }

    [Fact]
    public void GetUniqueIdentifierParameters_MultipleIdentities_UsesOnlyAuthenticatedIdentities()
    {
        // Arrange
        var identity1 = new ClaimsIdentity(); // no authentication
        identity1.AddClaim(new Claim("sub", "subClaimValue"));
        var identity2 = new ClaimsIdentity("someAuthentication");
        identity2.AddClaim(new Claim(ClaimTypes.NameIdentifier, "nameIdentifierValue"));

        // Act
        var uniqueIdentifierParameters = DefaultClaimUidExtractor.GetUniqueIdentifierParameters(new ClaimsIdentity[] { identity1, identity2 });

        // Assert
        Assert.Equal(new string[]
        {
                ClaimTypes.NameIdentifier,
                "nameIdentifierValue",
                "LOCAL AUTHORITY",
        }, uniqueIdentifierParameters);
    }

    [Fact]
    public void GetUniqueIdentifierParameters_NoKnownClaimTypesFound_SortsAndReturnsAllClaimsFromAuthenticatedIdentities()
    {
        // Arrange
        var identity1 = new ClaimsIdentity(); // no authentication
        identity1.AddClaim(new Claim("sub", "subClaimValue"));
        var identity2 = new ClaimsIdentity("someAuthentication");
        identity2.AddClaim(new Claim(ClaimTypes.Email, "email@domain.com"));
        var identity3 = new ClaimsIdentity("someAuthentication");
        identity3.AddClaim(new Claim(ClaimTypes.Country, "countryValue"));
        var identity4 = new ClaimsIdentity("someAuthentication");
        identity4.AddClaim(new Claim(ClaimTypes.Name, "claimName"));

        // Act
        var uniqueIdentifierParameters = DefaultClaimUidExtractor.GetUniqueIdentifierParameters(
            new ClaimsIdentity[] { identity1, identity2, identity3, identity4 });

        // Assert
        Assert.Equal(new List<string>
            {
                ClaimTypes.Country,
                "countryValue",
                "LOCAL AUTHORITY",
                ClaimTypes.Email,
                "email@domain.com",
                "LOCAL AUTHORITY",
                ClaimTypes.Name,
                "claimName",
                "LOCAL AUTHORITY",
            }, uniqueIdentifierParameters);
    }

    [Fact]
    public void GetUniqueIdentifierParameters_PrefersNameFromFirstIdentity_OverSubFromSecondIdentity()
    {
        // Arrange
        var identity1 = new ClaimsIdentity("someAuthentication");
        identity1.AddClaim(new Claim(ClaimTypes.NameIdentifier, "nameIdentifierValue"));
        var identity2 = new ClaimsIdentity("someAuthentication");
        identity2.AddClaim(new Claim("sub", "subClaimValue"));

        // Act
        var uniqueIdentifierParameters = DefaultClaimUidExtractor.GetUniqueIdentifierParameters(
            new ClaimsIdentity[] { identity1, identity2 });

        // Assert
        Assert.Equal(new string[]
        {
                ClaimTypes.NameIdentifier,
                "nameIdentifierValue",
                "LOCAL AUTHORITY",
        }, uniqueIdentifierParameters);
    }

    [Fact]
    public void GetUniqueIdentifierParameters_PrefersUpnFromFirstIdentity_OverNameFromSecondIdentity()
    {
        // Arrange
        var identity1 = new ClaimsIdentity("someAuthentication");
        identity1.AddClaim(new Claim(ClaimTypes.Upn, "upnValue"));
        var identity2 = new ClaimsIdentity("someAuthentication");
        identity2.AddClaim(new Claim(ClaimTypes.NameIdentifier, "nameIdentifierValue"));

        // Act
        var uniqueIdentifierParameters = DefaultClaimUidExtractor.GetUniqueIdentifierParameters(
            new ClaimsIdentity[] { identity1, identity2 });

        // Assert
        Assert.Equal(new string[]
        {
                ClaimTypes.Upn,
                "upnValue",
                "LOCAL AUTHORITY",
        }, uniqueIdentifierParameters);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OpenIdConnect.Claims;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect;

public class UniqueJsonKeyClaimActionTests
{
    [Fact]
    public void AddsIfNoDuplicateExists()
    {
        var userData = JsonDocument.Parse("{ \"jsonKey\": \"value\" }");

        var identity = new ClaimsIdentity();

        var action = new UniqueJsonKeyClaimAction("claimType", "valueType", "jsonKey");
        action.Run(userData.RootElement, identity, "iss");

        var claim = identity.FindFirst("claimType");
        Assert.NotNull(claim);
        Assert.Equal("claimType", claim.Type);
        Assert.Equal("value", claim.Value);
    }

    [Fact]
    public void DoesNotAddIfDuplicateExists()
    {
        var userData = JsonDocument.Parse("{ \"jsonKey\": \"value\" }");

        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("claimType", "value", "valueType"));

        var action = new UniqueJsonKeyClaimAction("claimType", "valueType", "jsonKey");
        action.Run(userData.RootElement, identity, "iss");

        var claims = identity.FindAll("claimType");
        Assert.Single(claims);
        Assert.Equal("claimType", claims.First().Type);
        Assert.Equal("value", claims.First().Value);
    }
}

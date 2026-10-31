// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;

namespace Microsoft.AspNetCore.Authentication;

public class ClaimActionTests
{
    [Fact]
    public void CanMapSingleValueUserDataToClaim()
    {
        var userData = JsonDocument.Parse("{ \"name\": \"test\" }");

        var identity = new ClaimsIdentity();

        var action = new JsonKeyClaimAction("name", "name", "name");
        action.Run(userData.RootElement, identity, "iss");

        Assert.Equal("name", identity.FindFirst("name").Type);
        Assert.Equal("test", identity.FindFirst("name").Value);
    }

    [Fact]
    public void CanMapArrayValueUserDataToClaims()
    {
        var userData = JsonDocument.Parse("{ \"role\": [ \"role1\", null, \"role2\" ] }");

        var identity = new ClaimsIdentity();

        var action = new JsonKeyClaimAction("role", "role", "role");
        action.Run(userData.RootElement, identity, "iss");

        var roleClaims = identity.FindAll("role").ToList();
        Assert.Equal(2, roleClaims.Count);
        Assert.Equal("role", roleClaims[0].Type);
        Assert.Equal("role1", roleClaims[0].Value);
        Assert.Equal("role", roleClaims[1].Type);
        Assert.Equal("role2", roleClaims[1].Value);
    }

    [Fact]
    public void CanMapSingleSubValueUserDataToClaim()
    {
        var userData = JsonDocument.Parse("{ \"name\": { \"subkey\": \"test\" } }");

        var identity = new ClaimsIdentity();

        var action = new JsonSubKeyClaimAction("name", "name", "name", "subkey");
        action.Run(userData.RootElement, identity, "iss");

        Assert.Equal("name", identity.FindFirst("name").Type);
        Assert.Equal("test", identity.FindFirst("name").Value);
    }

    [Fact]
    public void CanMapArraySubValueUserDataToClaims()
    {
        var userData = JsonDocument.Parse("{ \"role\": { \"subkey\": [ \"role1\", null, \"role2\" ] } }");

        var identity = new ClaimsIdentity();

        var action = new JsonSubKeyClaimAction("role", "role", "role", "subkey");
        action.Run(userData.RootElement, identity, "iss");

        var roleClaims = identity.FindAll("role").ToList();
        Assert.Equal(2, roleClaims.Count);
        Assert.Equal("role", roleClaims[0].Type);
        Assert.Equal("role1", roleClaims[0].Value);
        Assert.Equal("role", roleClaims[1].Type);
        Assert.Equal("role2", roleClaims[1].Value);
    }

    [Fact]
    public void MapAllSucceeds()
    {
        var userData = JsonDocument.Parse("{ \"name0\": \"value0\", \"name1\": \"value1\" }");

        var identity = new ClaimsIdentity();
        var action = new MapAllClaimsAction();
        action.Run(userData.RootElement, identity, "iss");

        Assert.Equal("name0", identity.FindFirst("name0").Type);
        Assert.Equal("value0", identity.FindFirst("name0").Value);
        Assert.Equal("name1", identity.FindFirst("name1").Type);
        Assert.Equal("value1", identity.FindFirst("name1").Value);
    }

    [Fact]
    public void MapAllAllowesDulicateKeysWithUniqueValues()
    {
        var userData = JsonDocument.Parse("{ \"name0\": \"value0\", \"name1\": \"value1\" }");

        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("name0", "value2"));
        identity.AddClaim(new Claim("name1", "value3"));
        var action = new MapAllClaimsAction();
        action.Run(userData.RootElement, identity, "iss");

        Assert.Equal(2, identity.FindAll("name0").Count());
        Assert.Equal(2, identity.FindAll("name1").Count());
    }

    [Fact]
    public void MapAllSkipsDuplicateValues()
    {
        var userData = JsonDocument.Parse("{ \"name0\": \"value0\", \"name1\": \"value1\" }");

        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("name0", "value0"));
        identity.AddClaim(new Claim("name1", "value1"));
        var action = new MapAllClaimsAction();
        action.Run(userData.RootElement, identity, "iss");

        Assert.Single(identity.FindAll("name0"));
        Assert.Single(identity.FindAll("name1"));
    }
}

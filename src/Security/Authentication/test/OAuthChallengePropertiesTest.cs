// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.Test;

public class OAuthChallengePropertiesTest
{
    [Fact]
    public void ScopeProperty()
    {
        var properties = new OAuthChallengeProperties
        {
            Scope = new string[] { "foo", "bar" }
        };
        Assert.Equal(new string[] { "foo", "bar" }, properties.Scope);
        Assert.Equal(new string[] { "foo", "bar" }, properties.Parameters["scope"]);
    }

    [Fact]
    public void ScopeProperty_NullValue()
    {
        var properties = new OAuthChallengeProperties();
        properties.Parameters["scope"] = new string[] { "foo", "bar" };
        Assert.Equal(new string[] { "foo", "bar" }, properties.Scope);

        properties.Scope = null;
        Assert.Null(properties.Scope);
    }

    [Fact]
    public void SetScope()
    {
        var properties = new OAuthChallengeProperties();
        properties.SetScope("foo", "bar");
        Assert.Equal(new string[] { "foo", "bar" }, properties.Scope);
        Assert.Equal(new string[] { "foo", "bar" }, properties.Parameters["scope"]);
    }

    [Fact]
    public void OidcMaxAge()
    {
        var properties = new OpenIdConnectChallengeProperties()
        {
            MaxAge = TimeSpan.FromSeconds(200)
        };
        Assert.Equal(TimeSpan.FromSeconds(200), properties.MaxAge);
    }

    [Fact]
    public void OidcMaxAge_NullValue()
    {
        var properties = new OpenIdConnectChallengeProperties();
        properties.Parameters["max_age"] = TimeSpan.FromSeconds(500);
        Assert.Equal(TimeSpan.FromSeconds(500), properties.MaxAge);

        properties.MaxAge = null;
        Assert.Null(properties.MaxAge);
    }

    [Fact]
    public void OidcPrompt()
    {
        var properties = new OpenIdConnectChallengeProperties()
        {
            Prompt = "login"
        };
        Assert.Equal("login", properties.Prompt);
        Assert.Equal("login", properties.Parameters["prompt"]);
    }

    [Fact]
    public void OidcPrompt_NullValue()
    {
        var properties = new OpenIdConnectChallengeProperties();
        properties.Parameters["prompt"] = "consent";
        Assert.Equal("consent", properties.Prompt);

        properties.Prompt = null;
        Assert.Null(properties.Prompt);
    }

    [Fact]
    public void GoogleProperties()
    {
        var properties = new GoogleChallengeProperties()
        {
            AccessType = "offline",
            ApprovalPrompt = "force",
            LoginHint = "test@example.com",
            Prompt = "login",
        };
        Assert.Equal("offline", properties.AccessType);
        Assert.Equal("offline", properties.Parameters["access_type"]);
        Assert.Equal("force", properties.ApprovalPrompt);
        Assert.Equal("force", properties.Parameters["approval_prompt"]);
        Assert.Equal("test@example.com", properties.LoginHint);
        Assert.Equal("test@example.com", properties.Parameters["login_hint"]);
        Assert.Equal("login", properties.Prompt);
        Assert.Equal("login", properties.Parameters["prompt"]);
    }

    [Fact]
    public void GoogleProperties_NullValues()
    {
        var properties = new GoogleChallengeProperties();
        properties.Parameters["access_type"] = "offline";
        properties.Parameters["approval_prompt"] = "force";
        properties.Parameters["login_hint"] = "test@example.com";
        properties.Parameters["prompt"] = "login";
        Assert.Equal("offline", properties.AccessType);
        Assert.Equal("force", properties.ApprovalPrompt);
        Assert.Equal("test@example.com", properties.LoginHint);
        Assert.Equal("login", properties.Prompt);

        properties.AccessType = null;
        Assert.Null(properties.AccessType);

        properties.ApprovalPrompt = null;
        Assert.Null(properties.ApprovalPrompt);

        properties.LoginHint = null;
        Assert.Null(properties.LoginHint);

        properties.Prompt = null;
        Assert.Null(properties.Prompt);
    }

    [Fact]
    public void GoogleIncludeGrantedScopes()
    {
        var properties = new GoogleChallengeProperties()
        {
            IncludeGrantedScopes = true
        };
        Assert.True(properties.IncludeGrantedScopes);
        Assert.Equal(true, properties.Parameters["include_granted_scopes"]);

        properties.IncludeGrantedScopes = false;
        Assert.False(properties.IncludeGrantedScopes);
        Assert.Equal(false, properties.Parameters["include_granted_scopes"]);

        properties.IncludeGrantedScopes = null;
        Assert.Null(properties.IncludeGrantedScopes);
    }
}

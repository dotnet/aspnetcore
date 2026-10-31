// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Google;

/// <summary>
/// Configuration options for <see cref="GoogleHandler"/>.
/// </summary>
public class GoogleOptions : OAuthOptions
{
    /// <summary>
    /// Initializes a new <see cref="GoogleOptions"/>.
    /// </summary>
    public GoogleOptions()
    {
        CallbackPath = new PathString("/signin-google");
        AuthorizationEndpoint = GoogleDefaults.AuthorizationEndpoint;
        TokenEndpoint = GoogleDefaults.TokenEndpoint;
        UserInformationEndpoint = GoogleDefaults.UserInformationEndpoint;
        UsePkce = true;
        Scope.Add("openid");
        Scope.Add("profile");
        Scope.Add("email");

        ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id"); // v2
        ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub"); // v3
        ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
        ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
        ClaimActions.MapJsonKey("urn:google:profile", "link");
        ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
    }

    /// <summary>
    /// Indicates whether your application can refresh access tokens when the user is not present at the browser.
    /// Valid values are <c>online</c>, which is the default value, and <c>offline</c>.
    /// <para>
    /// Set the value to offline if your application needs to refresh access tokens when the user is not present at the browser.
    /// </para>
    /// </summary>
    public string? AccessType { get; set; }
}

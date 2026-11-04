// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.MicrosoftAccount;

/// <summary>
/// Configuration options for <see cref="MicrosoftAccountHandler"/>.
/// </summary>
public class MicrosoftAccountOptions : OAuthOptions
{
    /// <summary>
    /// Initializes a new <see cref="MicrosoftAccountOptions"/>.
    /// </summary>
    public MicrosoftAccountOptions()
    {
        CallbackPath = new PathString("/signin-microsoft");
        AuthorizationEndpoint = MicrosoftAccountDefaults.AuthorizationEndpoint;
        TokenEndpoint = MicrosoftAccountDefaults.TokenEndpoint;
        UserInformationEndpoint = MicrosoftAccountDefaults.UserInformationEndpoint;
        UsePkce = true;
        Scope.Add("https://graph.microsoft.com/user.read");

        ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        ClaimActions.MapJsonKey(ClaimTypes.Name, "displayName");
        ClaimActions.MapJsonKey(ClaimTypes.GivenName, "givenName");
        ClaimActions.MapJsonKey(ClaimTypes.Surname, "surname");
        ClaimActions.MapCustomJson(ClaimTypes.Email, user => user.GetString("mail") ?? user.GetString("userPrincipalName"));
    }
}

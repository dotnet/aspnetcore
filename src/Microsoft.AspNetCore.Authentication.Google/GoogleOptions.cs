// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Google
{
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
            Scope.Add("openid");
            Scope.Add("profile");
            Scope.Add("email");

            ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
            ClaimActions.MapJsonKey(ClaimTypes.Name, "displayName");
            ClaimActions.MapJsonSubKey(ClaimTypes.GivenName, "name", "givenName");
            ClaimActions.MapJsonSubKey(ClaimTypes.Surname, "name", "familyName");
            ClaimActions.MapJsonKey("urn:google:profile", "url");
            ClaimActions.MapCustomJson(ClaimTypes.Email, GoogleHelper.GetEmail);
        }

        /// <summary>
        /// access_type. Set to 'offline' to request a refresh token.
        /// </summary>
        public string AccessType { get; set; }
    }
}
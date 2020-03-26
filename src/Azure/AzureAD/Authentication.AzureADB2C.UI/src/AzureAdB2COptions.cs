// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.AzureADB2C.UI
{
    /// <summary>
    /// Options for configuring authentication using Azure Active Directory B2C.
    /// </summary>
    public class AzureADB2COptions
    {
        /// <summary>
        /// Gets or sets the OpenID Connect authentication scheme to use for authentication with this instance
        /// of Azure Active Directory B2C authentication.
        /// </summary>
        public string OpenIdConnectSchemeName { get; set; } = OpenIdConnectDefaults.AuthenticationScheme;

        /// <summary>
        /// Gets or sets the Cookie authentication scheme to use for sign in with this instance of
        /// Azure Active Directory B2C authentication.
        /// </summary>
        public string CookieSchemeName { get; set; } = CookieAuthenticationDefaults.AuthenticationScheme;

        /// <summary>
        /// Gets or sets the Jwt bearer authentication scheme to use for validating access tokens for this
        /// instance of Azure Active Directory B2C Bearer authentication.
        /// </summary>
        public string JwtBearerSchemeName { get; internal set; }


        /// <summary>
        /// Gets or sets the client Id.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the client secret.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the Azure Active Directory B2C instance.
        /// </summary>
        public string Instance { get; set; }

        /// <summary>
        /// Gets or sets the domain of the Azure Active Directory B2C tenant.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets the edit profile policy name.
        /// </summary>
        public string EditProfilePolicyId { get; set; }

        /// <summary>
        /// Gets or sets the sign up or sign in policy name.
        /// </summary>
        public string SignUpSignInPolicyId { get; set; }

        /// <summary>
        /// Gets or sets the reset password policy id.
        /// </summary>
        public string ResetPasswordPolicyId { get; set; }

        /// <summary>
        /// Gets or sets the sign in callback path.
        /// </summary>
        public string CallbackPath { get; set; }

        /// <summary>
        /// Gets or sets the sign out callback path.
        /// </summary>
        public string SignedOutCallbackPath { get; set; }

        /// <summary>
        /// Gets or sets the default policy.
        /// </summary>
        public string DefaultPolicy => SignUpSignInPolicyId;

        /// <summary>
        /// Gets all the underlying authentication schemes.
        /// </summary>
        public string[] AllSchemes => new[] { CookieSchemeName, OpenIdConnectSchemeName };
    }
}

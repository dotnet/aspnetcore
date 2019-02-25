// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.AzureAD.UI
{
    /// <summary>
    /// Options for configuring authentication using Azure Active Directory.
    /// </summary>
    public class AzureADOptions
    {
        /// <summary>
        /// Gets or sets the OpenID Connect authentication scheme to use for authentication with this instance
        /// of Azure Active Directory authentication.
        /// </summary>
        public string OpenIdConnectSchemeName { get; set; } = OpenIdConnectDefaults.AuthenticationScheme;

        /// <summary>
        /// Gets or sets the Cookie authentication scheme to use for sign in with this instance of
        /// Azure Active Directory authentication.
        /// </summary>
        public string CookieSchemeName { get; set; } = CookieAuthenticationDefaults.AuthenticationScheme;

        /// <summary>
        /// Gets or sets the Jwt bearer authentication scheme to use for validating access tokens for this
        /// instance of Azure Active Directory Bearer authentication.
        /// </summary>
        public string JwtBearerSchemeName { get; internal set; }

        /// <summary>
        /// Gets or sets the client Id (Application Id) of the Azure AD application
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the client secret for the application (application password) 
        /// </summary>
        /// <remarks>
        /// The client secret is only used if the Web app or Web API
        /// calls a Web API
        /// </remarks>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the tenant id. The tenant id can have one of the following values:
        /// <list type="table">
        /// <item><term>a proper tenant ID</term><description>A GUID representing the ID of the Azure Active Directory Tenant (directory ID)</description></item>
        /// <item><term>a domain name</term><description>associated with the Azure Active Directory tenant</description></item>
        /// <item><term>common</term><description>if the <see cref="Authority"/> is Azure AD v2.0, enables to sign-in users from any
        /// Work and School account or Microsoft Personal Account. If Authority is Azure AD v1.0, enables sign-in from any Work and School accounts</description></item>
        /// <item><term>organizations</term><description>if the <see cref="Authority"/> is Azure AD v2.0, enables to sign-in users from any
        /// Work and School account</description></item>
        /// <item><term>consumers</term><description>if the <see cref="Authority"/> is Azure AD v2.0, enables to sign-in users from any
        /// Microsoft personal account</description></item>
        /// </list>
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the Azure Active Directory instance.
        /// Typical values are:
        /// <list type="table">
        /// <item><term>https://login.microsoftonline.com/</term><description>For Microsoft Azure public cloud</description></item>
        /// <item><term>https://login.microsoftonline.us/</term><description>For Azure US Government</description></item>
        /// <item><term>https://login.partner.microsoftonline.cn/</term><description>For Azure China 21Vianet</description></item>
        /// <item><term>https://login.microsoftonline.de/</term><description>For Azure Germany</description></item>
        /// </list>
        /// </summary>
        public string Instance { get; set; } = "https://login.microsoftonline.com/";

        /// <summary>
        /// Gets or sets the domain associated with the Azure Active Directory tenant.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Azure Active Directory Authority
        /// </summary>
        public string Authority { get; set; } = "{Instance}{TenantId}";

        /// <summary>
        /// Gets or sets the audience for a Web API (This audience needs
        /// to match the audience of the tokens sent to access this application)
        /// </summary>
        public string Audience { get; set; } = "{ClientId}";

        /// <summary>
        /// Gets or sets the sign in callback path.
        /// </summary>
        public string CallbackPath { get; set; }

        /// <summary>
        /// Gets or sets the sign out callback path.
        /// </summary>
        public string SignedOutCallbackPath { get; set; }

        /// <summary>
        /// Gets all the underlying authentication schemes.
        /// </summary>
        public string[] AllSchemes => new[] { CookieSchemeName, OpenIdConnectSchemeName };
    }
}

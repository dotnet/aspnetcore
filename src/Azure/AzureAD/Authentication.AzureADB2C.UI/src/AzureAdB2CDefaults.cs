// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Authentication;

namespace Microsoft.AspNetCore.Authentication.AzureADB2C.UI
{
    /// <summary>
    /// Constants for different Azure Active Directory B2C authentication components.
    /// </summary>
    [Obsolete("This is obsolete and will be removed in a future version. Use Microsoft.Identity.Web instead. See https://aka.ms/ms-identity-web.")]
    public static class AzureADB2CDefaults
    {
        /// <summary>
        /// The key for the policy used in <see cref="AuthenticationProperties"/>.
        /// </summary>
        public static readonly string PolicyKey = "Policy";

        /// <summary>
        /// The scheme name for Open ID Connect when using
        /// <see cref="AzureADB2CAuthenticationBuilderExtensions.AddAzureADB2C(AuthenticationBuilder, System.Action{AzureADB2COptions})"/>.
        /// </summary>
        public const string OpenIdScheme = "AzureADB2COpenID";

        /// <summary>
        /// The scheme name for cookies when using
        /// <see cref="AzureADB2CAuthenticationBuilderExtensions.AddAzureADB2C(AuthenticationBuilder, System.Action{AzureADB2COptions})"/>.
        /// </summary>
        public const string CookieScheme = "AzureADB2CCookie";

        /// <summary>
        /// The default scheme for Azure Active Directory B2C Bearer.
        /// </summary>
        public const string BearerAuthenticationScheme = "AzureADB2CBearer";

        /// <summary>
        /// The scheme name for JWT Bearer when using
        /// <see cref="AzureADB2CAuthenticationBuilderExtensions.AddAzureADB2CBearer(AuthenticationBuilder, System.Action{AzureADB2COptions})"/>.
        /// </summary>
        public const string JwtBearerAuthenticationScheme = "AzureADB2CJwtBearer";

        /// <summary>
        /// The default scheme for Azure Active Directory B2C.
        /// </summary>
        public const string AuthenticationScheme = "AzureADB2C";

        /// <summary>
        /// The display name for Azure Active Directory B2C.
        /// </summary>
        public static readonly string DisplayName = "Azure Active Directory B2C";
    }
}

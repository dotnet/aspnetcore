// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;


using Microsoft.AspNetCore.Authentication;

namespace Microsoft.AspNetCore.Authentication.AzureADB2C.UI
{
    /// <summary>
    /// Constants for different Azure Active Directory B2C authentication components.
    /// </summary>
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
        public static readonly string OpenIdScheme = "AzureADB2COpenID";

        /// <summary>
        /// The scheme name for cookies when using
        /// <see cref="AzureADB2CAuthenticationBuilderExtensions.AddAzureADB2C(AuthenticationBuilder, System.Action{AzureADB2COptions})"/>.
        /// </summary>
        public static readonly string CookieScheme = "AzureADB2CCookie";

        /// <summary>
        /// The default scheme for Azure Active Directory B2C Bearer.
        /// </summary>
        public static readonly string BearerAuthenticationScheme = "AzureADB2CBearer";

        /// <summary>
        /// The scheme name for JWT Bearer when using
        /// <see cref="AzureADB2CAuthenticationBuilderExtensions.AddAzureADB2CBearer(AuthenticationBuilder, System.Action{AzureADB2COptions})"/>.
        /// </summary>
        public static readonly string JwtBearerAuthenticationScheme = "AzureADB2CJwtBearer";

        /// <summary>
        /// The default scheme for Azure Active Directory B2C.
        /// </summary>
        public static readonly string AuthenticationScheme = "AzureADB2C";

        /// <summary>
        /// The display name for Azure Active Directory B2C.
        /// </summary>
        public static readonly string DisplayName = "Azure Active Directory B2C";
    }
}

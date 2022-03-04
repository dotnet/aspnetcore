// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;


namespace Microsoft.AspNetCore.Authentication.AzureAD.UI
{
    /// <summary>
    /// Constants for different Azure Active Directory authentication components.
    /// </summary>
    public static class AzureADDefaults
    {        
        /// <summary>
        /// The scheme name for Open ID Connect when using
        /// <see cref="AzureADAuthenticationBuilderExtensions.AddAzureAD(AuthenticationBuilder, System.Action{AzureADOptions})"/>.
        /// </summary>
        public const string OpenIdScheme = "AzureADOpenID";

        /// <summary>
        /// The scheme name for cookies when using
        /// <see cref="AzureADAuthenticationBuilderExtensions.AddAzureAD(AuthenticationBuilder, System.Action{AzureADOptions})"/>.
        /// </summary>
        public const string CookieScheme = "AzureADCookie";

        /// <summary>
        /// The default scheme for Azure Active Directory Bearer.
        /// </summary>
        public const string BearerAuthenticationScheme = "AzureADBearer";

        /// <summary>
        /// The scheme name for JWT Bearer when using
        /// <see cref="AzureADAuthenticationBuilderExtensions.AddAzureADBearer(AuthenticationBuilder, System.Action{AzureADOptions})"/>.
        /// </summary>
        public const string JwtBearerAuthenticationScheme = "AzureADJwtBearer";

        /// <summary>
        /// The default scheme for Azure Active Directory.
        /// </summary>
        public const string AuthenticationScheme = "AzureAD";

        /// <summary>
        /// The display name for Azure Active Directory.
        /// </summary>
        public static readonly string DisplayName = "Azure Active Directory";
    }
}

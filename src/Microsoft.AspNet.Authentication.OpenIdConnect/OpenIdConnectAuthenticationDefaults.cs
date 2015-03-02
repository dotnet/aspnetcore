// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Authentication.OpenIdConnect
{
    /// <summary>
    /// Default values related to OpenIdConnect authentication middleware
    /// </summary>
    public static class OpenIdConnectAuthenticationDefaults
    {
        /// <summary>
        /// The default value used for OpenIdConnectAuthenticationOptions.AuthenticationScheme
        /// </summary>
        public const string AuthenticationScheme = "OpenIdConnect";

        /// <summary>
        /// The prefix used to provide a default OpenIdConnectAuthenticationOptions.CookieName
        /// </summary>
        public const string CookiePrefix = ".AspNet.OpenIdConnect.";

        /// <summary>
        /// The default value for OpenIdConnectAuthenticationOptions.Caption.
        /// </summary>
        public const string Caption = "OpenIdConnect";

        /// <summary>
        /// The prefix used to for the a nonce in the cookie
        /// </summary>
        internal const string CookieNoncePrefix = ".AspNet.OpenIdConnect.Nonce.";

        /// <summary>
        /// The property for the RedirectUri that was used when asking for a 'authorizationCode'
        /// </summary>
        public const string RedirectUriUsedForCodeKey = "OpenIdConnect.Code.RedirectUri";

        /// <summary>
        /// Constant used to identify state in openIdConnect protocal message
        /// </summary>
        internal const string AuthenticationPropertiesKey = "OpenIdConnect.AuthenticationProperties";
    }
}

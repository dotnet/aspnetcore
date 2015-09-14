// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Authentication.OpenIdConnect
{
    /// <summary>
    /// Default values related to OpenIdConnect authentication middleware
    /// </summary>
    public static class OpenIdConnectDefaults
    {
        /// <summary>
        /// Constant used to identify state in openIdConnect protocol message.
        /// </summary>
        public const string AuthenticationPropertiesKey = "OpenIdConnect.AuthenticationProperties";

        /// <summary>
        /// The default value used for OpenIdConnectOptions.AuthenticationScheme.
        /// </summary>
        public const string AuthenticationScheme = "OpenIdConnect";

        /// <summary>
        /// The default value for OpenIdConnectOptions.Caption.
        /// </summary>
        public const string Caption = "OpenIdConnect";

        /// <summary>
        /// The prefix used to for the nonce in the cookie.
        /// </summary>
        public const string CookieNoncePrefix = ".AspNet.OpenIdConnect.Nonce.";

        /// <summary>
        /// The prefix used for the state in the cookie.
        /// </summary>
        public const string CookieStatePrefix = ".AspNet.OpenIdConnect.State.";

        /// <summary>
        /// The property for the RedirectUri that was used when asking for a 'authorizationCode'.
        /// </summary>
        public const string RedirectUriForCodePropertiesKey = "OpenIdConnect.Code.RedirectUri";

        /// <summary>
        /// Constant used to identify userstate inside AuthenticationProperties that have been serialized in the 'state' parameter.
        /// </summary>
        public const string UserstatePropertiesKey = "OpenIdConnect.Userstate";
    }
}

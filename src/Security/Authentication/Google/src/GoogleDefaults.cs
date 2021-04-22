// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Authentication.Google
{
    /// <summary>
    /// Default values for Google authentication
    /// </summary>
    public static class GoogleDefaults
    {
        /// <summary>
        /// The default scheme for Google authentication. Defaults to <c>Google</c>.
        /// </summary>
        public const string AuthenticationScheme = "Google";

        /// <summary>
        /// The default display name for Google authentication. Defaults to <c>Google</c>.
        /// </summary>
        public static readonly string DisplayName = "Google";

        /// <summary>
        /// The default endpoint used to perform Google authentication.
        /// </summary>
        /// <remarks>
        /// For more details about this endpoint, see https://developers.google.com/identity/protocols/oauth2/web-server#httprest
        /// </remarks>
        public static readonly string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";

        /// <summary>
        /// The OAuth endpoint used to exchange access tokens.
        /// </summary>
        public static readonly string TokenEndpoint = "https://oauth2.googleapis.com/token";

        /// <summary>
        /// The Google endpoint that is used to gather additional user information.
        /// </summary>
        /// <remarks>
        /// For more details about this endpoint, see https://developers.google.com/apis-explorer/#search/oauth2/oauth2/v2/.
        /// </remarks>
        public static readonly string UserInformationEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.Facebook
{
    /// <summary>
    /// Default values for the Facebook authentication handler.
    /// </summary>
    public static class FacebookDefaults
    {
        /// <summary>
        /// The default scheme for Facebook authentication. The value is <c>Facebook</c>.
        /// </summary>
        public const string AuthenticationScheme = "Facebook";

        /// <summary>
        /// The default display name for Facebook authentication. Defaults to <c>Facebook</c>.
        /// </summary>
        public static readonly string DisplayName = "Facebook";

        /// <summary>
        /// The default endpoint used to perform Facebook authentication.
        /// </summary>
        /// <remarks>
        /// For more details about this endpoint, see https://developers.facebook.com/docs/facebook-login/manually-build-a-login-flow#login.
        /// </remarks>
        public static readonly string AuthorizationEndpoint = "https://www.facebook.com/v8.0/dialog/oauth";

        /// <summary>
        /// The OAuth endpoint used to retrieve access tokens.
        /// </summary>
        public static readonly string TokenEndpoint = "https://graph.facebook.com/v8.0/oauth/access_token";

        /// <summary>
        /// The Facebook Graph API endpoint that is used to gather additional user information.
        /// </summary>
        public static readonly string UserInformationEndpoint = "https://graph.facebook.com/v8.0/me";
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.MicrosoftAccount
{
    /// <summary>
    /// Default values for Microsoft account authentication
    /// </summary>
    public static class MicrosoftAccountDefaults
    {
        /// <summary>
        /// The default scheme for Microsoft account authentication. Defaults to <c>Microsoft</c>.
        /// </summary>
        public const string AuthenticationScheme = "Microsoft";

        /// <summary>
        /// The default display name for Microsoft account authentication. Defaults to <c>Microsoft</c>.
        /// </summary>
        public static readonly string DisplayName = "Microsoft";

        /// <summary>
        /// The default endpoint used to perform Microsoft account authentication.
        /// </summary>
        /// <remarks>
        /// For more details about this endpoint, see https://developer.microsoft.com/en-us/graph/docs/concepts/auth_v2_user
        /// </remarks>
        public static readonly string AuthorizationEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";

        /// <summary>
        /// The OAuth endpoint used to exchange access tokens.
        /// </summary>
        public static readonly string TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

        /// <summary>
        /// The Microsoft Graph API endpoint that is used to gather additional user information.
        /// </summary>
        public static readonly string UserInformationEndpoint = "https://graph.microsoft.com/v1.0/me";
    }
}

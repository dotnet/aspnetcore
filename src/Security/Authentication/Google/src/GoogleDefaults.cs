// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.Google
{
    /// <summary>
    /// Default values for Google authentication
    /// </summary>
    public static class GoogleDefaults
    {
        public const string AuthenticationScheme = "Google";

        public static readonly string DisplayName = "Google";

        // https://developers.google.com/identity/protocols/OAuth2WebServer
        public static readonly string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";

        public static readonly string TokenEndpoint = "https://www.googleapis.com/oauth2/v4/token";

        // https://developers.google.com/+/web/people/
        public static readonly string UserInformationEndpoint = "https://www.googleapis.com/plus/v1/people/me";
    }
}

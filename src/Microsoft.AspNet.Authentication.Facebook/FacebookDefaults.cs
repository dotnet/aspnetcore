// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Authentication.Facebook
{
    public static class FacebookDefaults
    {
        public const string AuthenticationScheme = "Facebook";

        public static readonly string AuthorizationEndpoint = "https://www.facebook.com/v2.2/dialog/oauth";

        public static readonly string TokenEndpoint = "https://graph.facebook.com/v2.2/oauth/access_token";

        public static readonly string UserInformationEndpoint = "https://graph.facebook.com/v2.2/me";
    }
}

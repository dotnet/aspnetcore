// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Security.Facebook
{
    public static class FacebookAuthenticationDefaults
    {
        public const string AuthenticationType = "Facebook";

        public const string AuthorizationEndpoint = "https://www.facebook.com/dialog/oauth";

        public const string TokenEndpoint = "https://graph.facebook.com/oauth/access_token";

        public const string UserInformationEndpoint = "https://graph.facebook.com/me";
    }
}

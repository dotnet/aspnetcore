// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.MicrosoftAccount
{
    public static class MicrosoftAccountDefaults
    {
        public const string AuthenticationScheme = "Microsoft";

        public static readonly string DisplayName = "Microsoft";

        // https://developer.microsoft.com/en-us/graph/docs/concepts/auth_v2_user
        public static readonly string AuthorizationEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";

        public static readonly string TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

        public static readonly string UserInformationEndpoint = "https://graph.microsoft.com/v1.0/me";
    }
}

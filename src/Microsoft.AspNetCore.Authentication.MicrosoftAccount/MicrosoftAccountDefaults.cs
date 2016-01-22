// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Authentication.MicrosoftAccount
{
    public static class MicrosoftAccountDefaults
    {
        public const string AuthenticationScheme = "Microsoft";

        public static readonly string AuthorizationEndpoint = "https://login.live.com/oauth20_authorize.srf";

        public static readonly string TokenEndpoint = "https://login.live.com/oauth20_token.srf";

        public static readonly string UserInformationEndpoint = "https://apis.live.net/v5.0/me";
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Authentication.OAuth;

namespace Microsoft.AspNet.Authentication.MicrosoftAccount
{
    /// <summary>
    /// Configuration options for <see cref="MicrosoftAccountAuthenticationMiddleware"/>.
    /// </summary>
    public class MicrosoftAccountAuthenticationOptions : OAuthAuthenticationOptions
    {
        /// <summary>
        /// Initializes a new <see cref="MicrosoftAccountAuthenticationOptions"/>.
        /// </summary>
        public MicrosoftAccountAuthenticationOptions()
        {
            AuthenticationScheme = MicrosoftAccountAuthenticationDefaults.AuthenticationScheme;
            Caption = AuthenticationScheme;
            CallbackPath = new PathString("/signin-microsoft");
            AuthorizationEndpoint = MicrosoftAccountAuthenticationDefaults.AuthorizationEndpoint;
            TokenEndpoint = MicrosoftAccountAuthenticationDefaults.TokenEndpoint;
            UserInformationEndpoint = MicrosoftAccountAuthenticationDefaults.UserInformationEndpoint;
            SaveTokensAsClaims = false;
        }
    }
}

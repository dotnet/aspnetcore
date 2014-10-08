// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Security.OAuth;

namespace Microsoft.AspNet.Security.MicrosoftAccount
{
    /// <summary>
    /// Configuration options for <see cref="MicrosoftAccountAuthenticationMiddleware"/>.
    /// </summary>
    public class MicrosoftAccountAuthenticationOptions : OAuthAuthenticationOptions<IMicrosoftAccountAuthenticationNotifications>
    {
        /// <summary>
        /// Initializes a new <see cref="MicrosoftAccountAuthenticationOptions"/>.
        /// </summary>
        public MicrosoftAccountAuthenticationOptions()
        {
            AuthenticationType = MicrosoftAccountAuthenticationDefaults.AuthenticationType;
            Caption = AuthenticationType;
            CallbackPath = new PathString("/signin-microsoft");
            AuthorizationEndpoint = MicrosoftAccountAuthenticationDefaults.AuthorizationEndpoint;
            TokenEndpoint = MicrosoftAccountAuthenticationDefaults.TokenEndpoint;
            UserInformationEndpoint = MicrosoftAccountAuthenticationDefaults.UserInformationEndpoint;
        }
    }
}

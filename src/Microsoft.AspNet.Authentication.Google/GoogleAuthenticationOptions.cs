// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Authentication.OAuth;

namespace Microsoft.AspNet.Authentication.Google
{
    /// <summary>
    /// Configuration options for <see cref="GoogleAuthenticationMiddleware"/>.
    /// </summary>
    public class GoogleAuthenticationOptions : OAuthAuthenticationOptions<IGoogleAuthenticationNotifications>
    {
        /// <summary>
        /// Initializes a new <see cref="GoogleAuthenticationOptions"/>.
        /// </summary>
        public GoogleAuthenticationOptions()
        {
            AuthenticationScheme = GoogleAuthenticationDefaults.AuthenticationScheme;
            Caption = AuthenticationScheme;
            CallbackPath = new PathString("/signin-google");
            AuthorizationEndpoint = GoogleAuthenticationDefaults.AuthorizationEndpoint;
            TokenEndpoint = GoogleAuthenticationDefaults.TokenEndpoint;
            UserInformationEndpoint = GoogleAuthenticationDefaults.UserInformationEndpoint;
        }

        /// <summary>
        /// access_type. Set to 'offline' to request a refresh token.
        /// </summary>
        public string AccessType { get; set; }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Authentication.OAuth;

namespace Microsoft.AspNet.Authentication.Facebook
{
    /// <summary>
    /// Configuration options for <see cref="FacebookMiddleware"/>.
    /// </summary>
    public class FacebookOptions : OAuthOptions
    {
        /// <summary>
        /// Initializes a new <see cref="FacebookOptions"/>.
        /// </summary>
        public FacebookOptions()
        {
            AuthenticationScheme = FacebookDefaults.AuthenticationScheme;
            DisplayName = AuthenticationScheme;
            CallbackPath = new PathString("/signin-facebook");
            SendAppSecretProof = true;
            AuthorizationEndpoint = FacebookDefaults.AuthorizationEndpoint;
            TokenEndpoint = FacebookDefaults.TokenEndpoint;
            UserInformationEndpoint = FacebookDefaults.UserInformationEndpoint;
            SaveTokensAsClaims = false;
        }

        // Facebook uses a non-standard term for this field.
        /// <summary>
        /// Gets or sets the Facebook-assigned appId.
        /// </summary>
        public string AppId
        {
            get { return ClientId; }
            set { ClientId = value; }
        }

        // Facebook uses a non-standard term for this field.
        /// <summary>
        /// Gets or sets the Facebook-assigned app secret.
        /// </summary>
        public string AppSecret
        {
            get { return ClientSecret; }
            set { ClientSecret = value; }
        }

        /// <summary>
        /// Gets or sets if the appsecret_proof should be generated and sent with Facebook API calls.
        /// This is enabled by default.
        /// </summary>
        public bool SendAppSecretProof { get; set; }
    }
}

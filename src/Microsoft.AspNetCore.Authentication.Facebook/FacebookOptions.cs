// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
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
            Scope.Add("public_profile");
            Scope.Add("email");
            Fields.Add("name");
            Fields.Add("email");
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

        /// <summary>
        /// The list of fields to retrieve from the UserInformationEndpoint.
        /// https://developers.facebook.com/docs/graph-api/reference/user
        /// </summary>
        public ICollection<string> Fields { get; } = new HashSet<string>();
    }
}

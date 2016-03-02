// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Contains the options used by the <see cref="RemoteAuthenticationHandler"/>.
    /// </summary>
    public class RemoteAuthenticationOptions : AuthenticationOptions
    {
        /// <summary>
        /// Gets or sets timeout value in milliseconds for back channel communications with the remote provider.
        /// </summary>
        /// <value>
        /// The back channel timeout.
        /// </value>
        public TimeSpan BackchannelTimeout { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// The HttpMessageHandler used to communicate with Twitter.
        /// This cannot be set at the same time as BackchannelCertificateValidator unless the value 
        /// can be downcast to a WebRequestHandler.
        /// </summary>
        public HttpMessageHandler BackchannelHttpHandler { get; set; }

        /// <summary>
        /// The request path within the application's base path where the user-agent will be returned.
        /// The middleware will process this request when it arrives.
        /// </summary>
        public PathString CallbackPath { get; set; }

        /// <summary>
        /// Gets or sets the authentication scheme corresponding to the middleware
        /// responsible of persisting user's identity after a successful authentication.
        /// This value typically corresponds to a cookie middleware registered in the Startup class.
        /// When omitted, <see cref="SharedAuthenticationOptions.SignInScheme"/> is used as a fallback value.
        /// </summary>
        public string SignInScheme { get; set; }

        /// <summary>
        /// Get or sets the text that the user can display on a sign in user interface.
        /// </summary>
        public string DisplayName
        {
            get { return Description.DisplayName; }
            set { Description.DisplayName = value; }
        }

        /// <summary>
        /// Defines whether access and refresh tokens should be stored in the
        /// <see cref="ClaimsPrincipal"/> after a successful authorization with the remote provider.
        /// This property is set to <c>false</c> by default to reduce
        /// the size of the final authentication cookie.
        /// </summary>
        public bool SaveTokensAsClaims { get; set; }

        /// <summary>
        /// Gets or sets the time limit for completing the authentication flow (15 minutes by default).
        /// </summary>
        public TimeSpan RemoteAuthenticationTimeout { get; set; } = TimeSpan.FromMinutes(15);

        public IRemoteAuthenticationEvents Events = new RemoteAuthenticationEvents();
    }
}
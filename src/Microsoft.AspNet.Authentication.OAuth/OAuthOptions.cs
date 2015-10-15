// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;

namespace Microsoft.AspNet.Authentication.OAuth
{
    /// <summary>
    /// Configuration options for <see cref="OAuthMiddleware"/>.
    /// </summary>
    public class OAuthOptions : RemoteAuthenticationOptions
    {
        public OAuthOptions()
        {
            Events = new OAuthEvents();
        }

        /// <summary>
        /// Gets or sets the provider-assigned client id.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the provider-assigned client secret.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the URI where the client will be redirected to authenticate.
        /// </summary>
        public string AuthorizationEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the URI the middleware will access to exchange the OAuth token.
        /// </summary>
        public string TokenEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the URI the middleware will access to obtain the user information.
        /// This value is not used in the default implementation, it is for use in custom implementations of
        /// IOAuthAuthenticationEvents.Authenticated or OAuthAuthenticationHandler.CreateTicketAsync.
        /// </summary>
        public string UserInformationEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IOAuthEvents"/> used to handle authentication events.
        /// </summary>
        public new IOAuthEvents Events
        {
            get { return (IOAuthEvents)base.Events; }
            set { base.Events = value; }
        }

        /// <summary>
        /// A list of permissions to request.
        /// </summary>
        public IList<string> Scope { get; } = new List<string>();

        /// <summary>
        /// Gets or sets the type used to secure data handled by the middleware.
        /// </summary>
        public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; }

        /// <summary>
        /// Defines whether access and refresh tokens should be stored in the
        /// <see cref="ClaimsPrincipal"/> after a successful authentication.
        /// You can set this property to <c>false</c> to reduce the size of the final
        /// authentication cookie. Note that social providers set this property to <c>false</c> by default.
        /// </summary>
        public bool SaveTokensAsClaims { get; set; } = true;
    }
}

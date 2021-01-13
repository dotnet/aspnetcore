// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Provides access denied failure context information to handler providers.
    /// </summary>
    public class AccessDeniedContext : HandleRequestContext<RemoteAuthenticationOptions>
    {
        public AccessDeniedContext(
            HttpContext context,
            AuthenticationScheme scheme,
            RemoteAuthenticationOptions options)
            : base(context, scheme, options)
        {
        }

        /// <summary>
        /// Gets or sets the endpoint path the user agent will be redirected to.
        /// By default, this property is set to <see cref="RemoteAuthenticationOptions.AccessDeniedPath"/>.
        /// </summary>
        public PathString AccessDeniedPath { get; set; }

        /// <summary>
        /// Additional state values for the authentication session.
        /// </summary>
        public AuthenticationProperties Properties { get; set; }

        /// <summary>
        /// Gets or sets the return URL that will be flowed up to the access denied page.
        /// If <see cref="ReturnUrlParameter"/> is not set, this property is not used.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Gets or sets the parameter name that will be used to flow the return URL.
        /// By default, this property is set to <see cref="RemoteAuthenticationOptions.ReturnUrlParameter"/>.
        /// </summary>
        public string ReturnUrlParameter { get; set; }
    }
}

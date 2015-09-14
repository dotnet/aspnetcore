// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNet.Authentication.OpenIdConnect
{
    /// <summary>
    /// When a user configures the <see cref="AuthenticationMiddleware{TOptions}"/> to be notified prior to redirecting to an IdentityProvider
    /// an instance of <see cref="RedirectToIdentityProviderContext"/> is passed to the 'RedirectToIdentityProvider" event.
    /// </summary>
    /// <typeparam name="TMessage">protocol specific message.</typeparam>
    /// <typeparam name="TOptions">protocol specific options.</typeparam>
    public class RedirectToIdentityProviderContext : BaseControlContext<OpenIdConnectOptions>
    {
        public RedirectToIdentityProviderContext([NotNull] HttpContext context, [NotNull] OpenIdConnectOptions options)
            : base(context, options)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="OpenIdConnectMessage"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">if 'value' is null.</exception>
        public OpenIdConnectMessage ProtocolMessage { get; [param: NotNull] set; }
    }
}
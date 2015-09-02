// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Authentication
{
    /// <summary>
    /// When a user configures the <see cref="AuthenticationMiddleware{TOptions}"/> to be notified prior to redirecting to an IdentityProvider
    /// an instance of <see cref="RedirectFromIdentityProviderContext{TMessage, TOptions, TMessage}"/> is passed to the 'RedirectToIdentityProviderContext".
    /// </summary>
    /// <typeparam name="TMessage">protocol specific message.</typeparam>
    /// <typeparam name="TOptions">protocol specific options.</typeparam>
    public class RedirectToIdentityProviderContext<TMessage, TOptions> : BaseControlContext<TOptions>
    {
        public RedirectToIdentityProviderContext([NotNull] HttpContext context, [NotNull] TOptions options) : base(context, options)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="{TMessage}"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">if 'value' is null.</exception>
        public TMessage ProtocolMessage { get; [param: NotNull] set; }
    }
}
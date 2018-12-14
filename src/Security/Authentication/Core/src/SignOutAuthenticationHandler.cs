// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Adds support for SignOutAsync
    /// </summary>
    public abstract class SignOutAuthenticationHandler<TOptions> : AuthenticationHandler<TOptions>, IAuthenticationSignOutHandler
        where TOptions : AuthenticationSchemeOptions, new()
    {
        public SignOutAuthenticationHandler(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        { }

        public virtual Task SignOutAsync(AuthenticationProperties properties)
        {
            var target = ResolveTarget(Options.ForwardSignOut);
            return (target != null)
                ? Context.SignOutAsync(target, properties)
                : HandleSignOutAsync(properties ?? new AuthenticationProperties());
        }

        /// <summary>
        /// Override this method to handle SignOut.
        /// </summary>
        /// <param name="properties"></param>
        /// <returns>A Task.</returns>
        protected abstract Task HandleSignOutAsync(AuthenticationProperties properties);
    }
}
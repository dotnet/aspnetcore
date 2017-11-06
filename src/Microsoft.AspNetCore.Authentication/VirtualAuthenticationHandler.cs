// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Forwards calls to another authentication scheme.
    /// </summary>
    public class VirtualAuthenticationHandler : IAuthenticationHandler, IAuthenticationSignInHandler
    {
        protected IOptionsMonitor<VirtualSchemeOptions> OptionsMonitor { get; }
        public AuthenticationScheme Scheme { get; private set; }
        public VirtualSchemeOptions Options { get; private set; }
        protected HttpContext Context { get; private set; }

        public VirtualAuthenticationHandler(IOptionsMonitor<VirtualSchemeOptions> options)
        {
            OptionsMonitor = options;
        }

        /// <summary>
        /// Initialize the handler, resolve the options and validate them.
        /// </summary>
        /// <param name="scheme"></param>
        /// <param name="context"></param>
        /// <returns>A Task.</returns>
        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            if (scheme == null)
            {
                throw new ArgumentNullException(nameof(scheme));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Scheme = scheme;
            Context = context;

            Options = OptionsMonitor.Get(Scheme.Name) ?? new VirtualSchemeOptions();
            Options.Validate();

            return Task.CompletedTask;
        }

        protected virtual string ResolveTarget(string scheme)
            => scheme ?? Options.DefaultSelector?.Invoke(Context) ?? Options.Default;

        public virtual Task<AuthenticateResult> AuthenticateAsync()
            => Context.AuthenticateAsync(ResolveTarget(Options.Authenticate));

        public virtual Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
            => Context.SignInAsync(ResolveTarget(Options.SignIn), user, properties);

        public virtual Task SignOutAsync(AuthenticationProperties properties)
            => Context.SignOutAsync(ResolveTarget(Options.SignOut), properties);

        public virtual Task ChallengeAsync(AuthenticationProperties properties)
            => Context.ChallengeAsync(ResolveTarget(Options.Challenge), properties);

        public virtual Task ForbidAsync(AuthenticationProperties properties)
            => Context.ForbidAsync(ResolveTarget(Options.Forbid), properties);
    }
}
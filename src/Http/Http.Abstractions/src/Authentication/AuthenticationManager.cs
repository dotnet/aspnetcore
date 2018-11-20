// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features.Authentication;

namespace Microsoft.AspNetCore.Http.Authentication
{
    [Obsolete("This is obsolete and will be removed in a future version. See https://go.microsoft.com/fwlink/?linkid=845470.")]
    public abstract class AuthenticationManager
    {
        /// <summary>
        /// Constant used to represent the automatic scheme
        /// </summary>
        public const string AutomaticScheme = "Automatic";

        public abstract HttpContext HttpContext { get; }

        public abstract IEnumerable<AuthenticationDescription> GetAuthenticationSchemes();

        public abstract Task<AuthenticateInfo> GetAuthenticateInfoAsync(string authenticationScheme);

        // Will remove once callees have been updated
        public abstract Task AuthenticateAsync(AuthenticateContext context);

        public virtual async Task<ClaimsPrincipal> AuthenticateAsync(string authenticationScheme)
        {
            return (await GetAuthenticateInfoAsync(authenticationScheme))?.Principal;
        }

        public virtual Task ChallengeAsync()
        {
            return ChallengeAsync(properties: null);
        }

        public virtual Task ChallengeAsync(AuthenticationProperties properties)
        {
            return ChallengeAsync(authenticationScheme: AutomaticScheme, properties: properties);
        }

        public virtual Task ChallengeAsync(string authenticationScheme)
        {
            if (string.IsNullOrEmpty(authenticationScheme))
            {
                throw new ArgumentException(nameof(authenticationScheme));
            }

            return ChallengeAsync(authenticationScheme: authenticationScheme, properties: null);
        }

        // Leave it up to authentication handler to do the right thing for the challenge
        public virtual Task ChallengeAsync(string authenticationScheme, AuthenticationProperties properties)
        {
            if (string.IsNullOrEmpty(authenticationScheme))
            {
                throw new ArgumentException(nameof(authenticationScheme));
            }

            return ChallengeAsync(authenticationScheme, properties, ChallengeBehavior.Automatic);
        }

        public virtual Task SignInAsync(string authenticationScheme, ClaimsPrincipal principal)
        {
            if (string.IsNullOrEmpty(authenticationScheme))
            {
                throw new ArgumentException(nameof(authenticationScheme));
            }

            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            return SignInAsync(authenticationScheme, principal, properties: null);
        }

        /// <summary>
        /// Creates a challenge for the authentication manager with <see cref="ChallengeBehavior.Forbidden"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents the asynchronous challenge operation.</returns>
        public virtual Task ForbidAsync()
            => ForbidAsync(AutomaticScheme, properties: null);

        public virtual Task ForbidAsync(string authenticationScheme)
        {
            if (authenticationScheme == null)
            {
                throw new ArgumentNullException(nameof(authenticationScheme));
            }

            return ForbidAsync(authenticationScheme, properties: null);
        }

        // Deny access (typically a 403)
        public virtual Task ForbidAsync(string authenticationScheme, AuthenticationProperties properties)
        {
            if (authenticationScheme == null)
            {
                throw new ArgumentNullException(nameof(authenticationScheme));
            }

            return ChallengeAsync(authenticationScheme, properties, ChallengeBehavior.Forbidden);
        }

        /// <summary>
        /// Creates a challenge for the authentication manager with <see cref="ChallengeBehavior.Forbidden"/>.
        /// </summary>
        /// <param name="properties">Additional arbitrary values which may be used by particular authentication types.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous challenge operation.</returns>
        public virtual Task ForbidAsync(AuthenticationProperties properties)
            => ForbidAsync(AutomaticScheme, properties);

        public abstract Task ChallengeAsync(string authenticationScheme, AuthenticationProperties properties, ChallengeBehavior behavior);

        public abstract Task SignInAsync(string authenticationScheme, ClaimsPrincipal principal, AuthenticationProperties properties);

        public virtual Task SignOutAsync(string authenticationScheme)
        {
            if (authenticationScheme == null)
            {
                throw new ArgumentNullException(nameof(authenticationScheme));
            }

            return SignOutAsync(authenticationScheme, properties: null);
        }

        public abstract Task SignOutAsync(string authenticationScheme, AuthenticationProperties properties);
    }
}

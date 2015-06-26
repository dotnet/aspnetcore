// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Http.Authentication
{
    public abstract class AuthenticationManager
    {
        public abstract IEnumerable<AuthenticationDescription> GetAuthenticationSchemes();

        public abstract Task AuthenticateAsync([NotNull] AuthenticateContext context);

        public virtual async Task<ClaimsPrincipal> AuthenticateAsync([NotNull] string authenticationScheme)
        {
            var context = new AuthenticateContext(authenticationScheme);
            await AuthenticateAsync(context);
            return context.Principal;
        }

        public virtual Task ChallengeAsync()
        {
            return ChallengeAsync(properties: null);
        }

        public virtual Task ChallengeAsync(AuthenticationProperties properties)
        {
            return ChallengeAsync(authenticationScheme: string.Empty, properties: properties);
        }

        public virtual Task ChallengeAsync([NotNull] string authenticationScheme)
        {
            return ChallengeAsync(authenticationScheme: authenticationScheme, properties: null);
        }

        // Leave it up to authentication handler to do the right thing for the challenge
        public virtual Task ChallengeAsync([NotNull] string authenticationScheme, AuthenticationProperties properties)
        {
            return ChallengeAsync(authenticationScheme, properties, ChallengeBehavior.Automatic);
        }

        public virtual Task SignInAsync([NotNull] string authenticationScheme, [NotNull] ClaimsPrincipal principal)
        {
            return SignInAsync(authenticationScheme, principal, properties: null);
        }

        public virtual Task ForbidAsync([NotNull] string authenticationScheme)
        {
            return ForbidAsync(authenticationScheme, properties: null);
        }

        // Deny access (typically a 403)
        public virtual Task ForbidAsync([NotNull] string authenticationScheme, AuthenticationProperties properties)
        {
            return ChallengeAsync(authenticationScheme, properties, ChallengeBehavior.Forbidden);
        }

        public abstract Task ChallengeAsync([NotNull] string authenticationScheme, AuthenticationProperties properties, ChallengeBehavior behavior);

        public abstract Task SignInAsync([NotNull] string authenticationScheme, [NotNull] ClaimsPrincipal principal, AuthenticationProperties properties);

        public virtual Task SignOutAsync([NotNull] string authenticationScheme)
        {
            return SignOutAsync(authenticationScheme, properties: null);
        }

        public abstract Task SignOutAsync([NotNull] string authenticationScheme, AuthenticationProperties properties);
    }
}

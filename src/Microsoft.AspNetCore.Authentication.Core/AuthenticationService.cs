// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Implements <see cref="IAuthenticationService"/>.
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="schemes">The <see cref="IAuthenticationSchemeProvider"/>.</param>
        /// <param name="handlers">The <see cref="IAuthenticationRequestHandler"/>.</param>
        /// <param name="transform">The The <see cref="IClaimsTransformation"/>.</param>
        public AuthenticationService(IAuthenticationSchemeProvider schemes, IAuthenticationHandlerProvider handlers, IClaimsTransformation transform)
        {
            Schemes = schemes;
            Handlers = handlers;
            Transform = transform;
        }

        /// <summary>
        /// Used to lookup AuthenticationSchemes.
        /// </summary>
        public IAuthenticationSchemeProvider Schemes { get; }

        /// <summary>
        /// Used to resolve IAuthenticationHandler instances.
        /// </summary>
        public IAuthenticationHandlerProvider Handlers { get; }

        /// <summary>
        /// Used for claims transformation.
        /// </summary>
        public IClaimsTransformation Transform { get; }

        /// <summary>
        /// Authenticate for the specified authentication scheme.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="scheme">The name of the authentication scheme.</param>
        /// <returns>The result.</returns>
        public virtual async Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme)
        {
            if (scheme == null)
            {
                var defaultScheme = await Schemes.GetDefaultAuthenticateSchemeAsync();
                scheme = defaultScheme?.Name;
                if (scheme == null)
                {
                    throw new InvalidOperationException($"No authenticationScheme was specified, and there was no DefaultAuthenticateScheme found.");
                }
            }

            var handler = await Handlers.GetHandlerAsync(context, scheme);
            if (handler == null)
            {
                throw new InvalidOperationException($"No authentication handler is configured to authenticate for the scheme: {scheme}");
            }

            var result = await handler.AuthenticateAsync();
            if (result != null && result.Succeeded)
            {
                var transformed = await Transform.TransformAsync(result.Principal);
                return AuthenticateResult.Success(new AuthenticationTicket(transformed, result.Properties, result.Ticket.AuthenticationScheme));
            }
            return result;
        }

        /// <summary>
        /// Challenge the specified authentication scheme.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="scheme">The name of the authentication scheme.</param>
        /// <param name="properties">The <see cref="AuthenticationProperties"/>.</param>
        /// <returns>A task.</returns>
        public virtual async Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties)
        {
            if (scheme == null)
            {
                var defaultChallengeScheme = await Schemes.GetDefaultChallengeSchemeAsync();
                scheme = defaultChallengeScheme?.Name;
                if (scheme == null)
                {
                    throw new InvalidOperationException($"No authenticationScheme was specified, and there was no DefaultChallengeScheme found.");
                }
            }

            var handler = await Handlers.GetHandlerAsync(context, scheme);
            if (handler == null)
            {
                throw new InvalidOperationException($"No authentication handler is configured to handle the scheme: {scheme}");
            }

            await handler.ChallengeAsync(properties);
        }

        /// <summary>
        /// Forbid the specified authentication scheme.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="scheme">The name of the authentication scheme.</param>
        /// <param name="properties">The <see cref="AuthenticationProperties"/>.</param>
        /// <returns>A task.</returns>
        public virtual async Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties)
        {
            if (scheme == null)
            {
                var defaultForbidScheme = await Schemes.GetDefaultForbidSchemeAsync();
                scheme = defaultForbidScheme?.Name;
                if (scheme == null)
                {
                    throw new InvalidOperationException($"No authenticationScheme was specified, and there was no DefaultForbidScheme found.");
                }
            }

            var handler = await Handlers.GetHandlerAsync(context, scheme);
            if (handler == null)
            {
                throw new InvalidOperationException($"No authentication handler is configured to handle the scheme: {scheme}");
            }

            await handler.ForbidAsync(properties);
        }

        /// <summary>
        /// Sign a principal in for the specified authentication scheme.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="scheme">The name of the authentication scheme.</param>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/> to sign in.</param>
        /// <param name="properties">The <see cref="AuthenticationProperties"/>.</param>
        /// <returns>A task.</returns>
        public virtual async Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            if (scheme == null)
            {
                var defaultScheme = await Schemes.GetDefaultSignInSchemeAsync();
                scheme = defaultScheme?.Name;
                if (scheme == null)
                {
                    throw new InvalidOperationException($"No authenticationScheme was specified, and there was no DefaultSignInScheme found.");
                }
            }

            var handler = await Handlers.GetHandlerAsync(context, scheme) as IAuthenticationSignInHandler;
            if (handler == null)
            {
                throw new InvalidOperationException($"No IAuthenticationSignInHandler is configured to handle sign in for the scheme: {scheme}");
            }

            await handler.SignInAsync(principal, properties);
        }

        /// <summary>
        /// Sign out the specified authentication scheme.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="scheme">The name of the authentication scheme.</param>
        /// <param name="properties">The <see cref="AuthenticationProperties"/>.</param>
        /// <returns>A task.</returns>
        public virtual async Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties)
        {
            if (scheme == null)
            {
                var defaultScheme = await Schemes.GetDefaultSignOutSchemeAsync();
                scheme = defaultScheme?.Name;
                if (scheme == null)
                {
                    throw new InvalidOperationException($"No authenticationScheme was specified, and there was no DefaultSignOutScheme found.");
                }
            }

            var handler = await Handlers.GetHandlerAsync(context, scheme) as IAuthenticationSignOutHandler;
            if (handler == null)
            {
                throw new InvalidOperationException($"No IAuthenticationSignOutHandler is configured to handle sign out for the scheme: {scheme}");
            }

            await handler.SignOutAsync(properties);
        }
    }
}

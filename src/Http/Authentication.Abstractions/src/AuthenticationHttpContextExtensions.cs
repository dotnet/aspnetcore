// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Extension methods to expose Authentication on HttpContext.
    /// </summary>
    public static class AuthenticationHttpContextExtensions
    {
        /// <summary>
        /// Extension method for authenticate using the <see cref="AuthenticationOptions.DefaultAuthenticateScheme"/> scheme.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <returns>The <see cref="AuthenticateResult"/>.</returns>
        public static Task<AuthenticateResult> AuthenticateAsync(this HttpContext context) =>
            context.AuthenticateAsync(scheme: null);

        /// <summary>
        /// Extension method for authenticate.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="scheme">The name of the authentication scheme.</param>
        /// <returns>The <see cref="AuthenticateResult"/>.</returns>
        public static Task<AuthenticateResult> AuthenticateAsync(this HttpContext context, string scheme) =>
            context.RequestServices.GetRequiredService<IAuthenticationService>().AuthenticateAsync(context, scheme);

        /// <summary>
        /// Extension method for Challenge.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="scheme">The name of the authentication scheme.</param>
        /// <returns>The result.</returns>
        public static Task ChallengeAsync(this HttpContext context, string scheme) =>
            context.ChallengeAsync(scheme, properties: null);

        /// <summary>
        /// Extension method for authenticate using the <see cref="AuthenticationOptions.DefaultChallengeScheme"/> scheme.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <returns>The task.</returns>
        public static Task ChallengeAsync(this HttpContext context) =>
            context.ChallengeAsync(scheme: null, properties: null);

        /// <summary>
        /// Extension method for authenticate using the <see cref="AuthenticationOptions.DefaultChallengeScheme"/> scheme.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
        /// <returns>The task.</returns>
        public static Task ChallengeAsync(this HttpContext context, AuthenticationProperties properties) =>
            context.ChallengeAsync(scheme: null, properties: properties);

        /// <summary>
        /// Extension method for Challenge.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="scheme">The name of the authentication scheme.</param>
        /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
        /// <returns>The task.</returns>
        public static Task ChallengeAsync(this HttpContext context, string scheme, AuthenticationProperties properties) =>
            context.RequestServices.GetRequiredService<IAuthenticationService>().ChallengeAsync(context, scheme, properties);

        /// <summary>
        /// Extension method for Forbid.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="scheme">The name of the authentication scheme.</param>
        /// <returns>The task.</returns>
        public static Task ForbidAsync(this HttpContext context, string scheme) =>
            context.ForbidAsync(scheme, properties: null);

        /// <summary>
        /// Extension method for Forbid using the <see cref="AuthenticationOptions.DefaultForbidScheme"/> scheme..
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <returns>The task.</returns>
        public static Task ForbidAsync(this HttpContext context) =>
            context.ForbidAsync(scheme: null, properties: null);

        /// <summary>
        /// Extension method for Forbid.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
        /// <returns>The task.</returns>
        public static Task ForbidAsync(this HttpContext context, AuthenticationProperties properties) =>
            context.ForbidAsync(scheme: null, properties: properties);

        /// <summary>
        /// Extension method for Forbid.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="scheme">The name of the authentication scheme.</param>
        /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
        /// <returns>The task.</returns>
        public static Task ForbidAsync(this HttpContext context, string scheme, AuthenticationProperties properties) =>
            context.RequestServices.GetRequiredService<IAuthenticationService>().ForbidAsync(context, scheme, properties);

        /// <summary>
        /// Extension method for SignIn.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="scheme">The name of the authentication scheme.</param>
        /// <param name="principal">The user.</param>
        /// <returns>The task.</returns>
        public static Task SignInAsync(this HttpContext context, string scheme, ClaimsPrincipal principal) =>
            context.SignInAsync(scheme, principal, properties: null);

        /// <summary>
        /// Extension method for SignIn using the <see cref="AuthenticationOptions.DefaultSignInScheme"/>.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="principal">The user.</param>
        /// <returns>The task.</returns>
        public static Task SignInAsync(this HttpContext context, ClaimsPrincipal principal) =>
            context.SignInAsync(scheme: null, principal: principal, properties: null);

        /// <summary>
        /// Extension method for SignIn using the <see cref="AuthenticationOptions.DefaultSignInScheme"/>.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="principal">The user.</param>
        /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
        /// <returns>The task.</returns>
        public static Task SignInAsync(this HttpContext context, ClaimsPrincipal principal, AuthenticationProperties properties) =>
            context.SignInAsync(scheme: null, principal: principal, properties: properties);

        /// <summary>
        /// Extension method for SignIn.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="scheme">The name of the authentication scheme.</param>
        /// <param name="principal">The user.</param>
        /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
        /// <returns>The task.</returns>
        public static Task SignInAsync(this HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties) =>
            context.RequestServices.GetRequiredService<IAuthenticationService>().SignInAsync(context, scheme, principal, properties);

        /// <summary>
        /// Extension method for SignOut using the <see cref="AuthenticationOptions.DefaultSignOutScheme"/>.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <returns>The task.</returns>
        public static Task SignOutAsync(this HttpContext context) => context.SignOutAsync(scheme: null, properties: null);

        /// <summary>
        /// Extension method for SignOut using the <see cref="AuthenticationOptions.DefaultSignOutScheme"/>.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
        /// <returns>The task.</returns>
        public static Task SignOutAsync(this HttpContext context, AuthenticationProperties properties) => context.SignOutAsync(scheme: null, properties: properties);

        /// <summary>
        /// Extension method for SignOut.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="scheme">The name of the authentication scheme.</param>
        /// <returns>The task.</returns>
        public static Task SignOutAsync(this HttpContext context, string scheme) => context.SignOutAsync(scheme, properties: null);

        /// <summary>
        /// Extension method for SignOut.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="scheme">The name of the authentication scheme.</param>
        /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
        /// <returns>The task.</returns>
        public static Task SignOutAsync(this HttpContext context, string scheme, AuthenticationProperties properties) =>
            context.RequestServices.GetRequiredService<IAuthenticationService>().SignOutAsync(context, scheme, properties);

        /// <summary>
        /// Extension method for getting the value of an authentication token.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="scheme">The name of the authentication scheme.</param>
        /// <param name="tokenName">The name of the token.</param>
        /// <returns>The value of the token.</returns>
        public static Task<string> GetTokenAsync(this HttpContext context, string scheme, string tokenName) =>
            context.RequestServices.GetRequiredService<IAuthenticationService>().GetTokenAsync(context, scheme, tokenName);

        /// <summary>
        /// Extension method for getting the value of an authentication token.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="tokenName">The name of the token.</param>
        /// <returns>The value of the token.</returns>
        public static Task<string> GetTokenAsync(this HttpContext context, string tokenName) =>
            context.RequestServices.GetRequiredService<IAuthenticationService>().GetTokenAsync(context, tokenName);
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Created per request to handle authentication for to a particular scheme.
    /// </summary>
    public interface IAuthenticationHandler
    {
        /// <summary>
        /// The handler should initialize anything it needs from the request and scheme here.
        /// </summary>
        /// <param name="scheme">The <see cref="AuthenticationScheme"/> scheme.</param>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <returns></returns>
        Task InitializeAsync(AuthenticationScheme scheme, HttpContext context);

        /// <summary>
        /// Authentication behavior.
        /// </summary>
        /// <returns>The <see cref="AuthenticateResult"/> result.</returns>
        Task<AuthenticateResult> AuthenticateAsync();

        /// <summary>
        /// Challenge behavior.
        /// </summary>
        /// <param name="context">The <see cref="ChallengeContext"/> context.</param>
        /// <returns>A task.</returns>
        Task ChallengeAsync(ChallengeContext context);

        /// <summary>
        /// Handle sign in.
        /// </summary>
        /// <param name="context">The <see cref="SignInContext"/> context.</param>
        /// <returns>A task.</returns>
        Task SignInAsync(SignInContext context);

        /// <summary>
        /// Signout behavior.
        /// </summary>
        /// <param name="context">The <see cref="SignOutContext"/> context.</param>
        /// <returns>A task.</returns>
        Task SignOutAsync(SignOutContext context);
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Responsible for managing what authenticationSchemes are supported.
    /// </summary>
    public interface IAuthenticationSchemeProvider
    {
        /// <summary>
        /// Returns all currently registered <see cref="AuthenticationScheme"/>s.
        /// </summary>
        /// <returns>All currently registered <see cref="AuthenticationScheme"/>s.</returns>
        Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync();

        /// <summary>
        /// Returns the <see cref="AuthenticationScheme"/> matching the name, or null.
        /// </summary>
        /// <param name="name">The name of the authenticationScheme.</param>
        /// <returns>The scheme or null if not found.</returns>
        Task<AuthenticationScheme> GetSchemeAsync(string name);

        /// <summary>
        /// Returns the scheme that will be used by default for <see cref="IAuthenticationService.AuthenticateAsync(HttpContext, string)"/>.
        /// This is typically specified via <see cref="AuthenticationOptions.DefaultAuthenticateScheme"/>.
        /// Otherwise, this will fallback to <see cref="AuthenticationOptions.DefaultScheme"/>.
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.AuthenticateAsync(HttpContext, string)"/>.</returns>
        Task<AuthenticationScheme> GetDefaultAuthenticateSchemeAsync();

        /// <summary>
        /// Returns the scheme that will be used by default for <see cref="IAuthenticationService.ChallengeAsync(HttpContext, string, AuthenticationProperties)"/>.
        /// This is typically specified via <see cref="AuthenticationOptions.DefaultChallengeScheme"/>.
        /// Otherwise, this will fallback to <see cref="AuthenticationOptions.DefaultScheme"/>.
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.ChallengeAsync(HttpContext, string, AuthenticationProperties)"/>.</returns>
        Task<AuthenticationScheme> GetDefaultChallengeSchemeAsync();

        /// <summary>
        /// Returns the scheme that will be used by default for <see cref="IAuthenticationService.ForbidAsync(HttpContext, string, AuthenticationProperties)"/>.
        /// This is typically specified via <see cref="AuthenticationOptions.DefaultForbidScheme"/>.
        /// Otherwise, this will fallback to <see cref="GetDefaultChallengeSchemeAsync"/> .
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.ForbidAsync(HttpContext, string, AuthenticationProperties)"/>.</returns>
        Task<AuthenticationScheme> GetDefaultForbidSchemeAsync();

        /// <summary>
        /// Returns the scheme that will be used by default for <see cref="IAuthenticationService.SignInAsync(HttpContext, string, System.Security.Claims.ClaimsPrincipal, AuthenticationProperties)"/>.
        /// This is typically specified via <see cref="AuthenticationOptions.DefaultSignInScheme"/>.
        /// Otherwise, this will fallback to <see cref="AuthenticationOptions.DefaultScheme"/>.
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.SignInAsync(HttpContext, string, System.Security.Claims.ClaimsPrincipal, AuthenticationProperties)"/>.</returns>
        Task<AuthenticationScheme> GetDefaultSignInSchemeAsync();

        /// <summary>
        /// Returns the scheme that will be used by default for <see cref="IAuthenticationService.SignOutAsync(HttpContext, string, AuthenticationProperties)"/>.
        /// This is typically specified via <see cref="AuthenticationOptions.DefaultSignOutScheme"/>.
        /// Otherwise, this will fallback to <see cref="GetDefaultSignInSchemeAsync"/> .
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.SignOutAsync(HttpContext, string, AuthenticationProperties)"/>.</returns>
        Task<AuthenticationScheme> GetDefaultSignOutSchemeAsync();

        /// <summary>
        /// Registers a scheme for use by <see cref="IAuthenticationService"/>. 
        /// </summary>
        /// <param name="scheme">The scheme.</param>
        void AddScheme(AuthenticationScheme scheme);

        /// <summary>
        /// Registers a scheme for use by <see cref="IAuthenticationService"/>. 
        /// </summary>
        /// <param name="scheme">The scheme.</param>
        /// <returns>true if the scheme was added successfully.</returns>
        bool TryAddScheme(AuthenticationScheme scheme)
        {
            try
            {
                AddScheme(scheme);
                return true;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Removes a scheme, preventing it from being used by <see cref="IAuthenticationService"/>.
        /// </summary>
        /// <param name="name">The name of the authenticationScheme being removed.</param>
        void RemoveScheme(string name);

        /// <summary>
        /// Returns the schemes in priority order for request handling.
        /// </summary>
        /// <returns>The schemes in priority order for request handling</returns>
        Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync();
    }
}
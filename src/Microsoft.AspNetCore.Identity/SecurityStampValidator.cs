// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Provides default implementation of validation functions for security stamps.
    /// </summary>
    /// <typeparam name="TUser">The type encapsulating a user.</typeparam>
    public class SecurityStampValidator<TUser> : ISecurityStampValidator where TUser : class
    {
        private readonly SignInManager<TUser> _signInManager;
        private readonly IdentityOptions _options;

        /// <summary>
        /// Creates a new instance of <see cref="SecurityStampValidator{TUser}"/>.
        /// </summary>
        /// <param name="options">Used to access the <see cref="IdentityOptions"/>.</param>
        /// <param name="signInManager">The <see cref="SignInManager{TUser}"/>.</param>
        public SecurityStampValidator(IOptions<IdentityOptions> options, SignInManager<TUser> signInManager)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (signInManager == null)
            {
                throw new ArgumentNullException(nameof(signInManager));
            }
            _signInManager = signInManager;
            _options = options.Value;
        }

        /// <summary>
        /// Validates a security stamp of an identity as an asynchronous operation, and rebuilds the identity if the validation succeeds, otherwise rejects
        /// the identity.
        /// </summary>
        /// <param name="context">The context containing the <see cref="System.Security.Claims.ClaimsPrincipal"/>
        /// and <see cref="Http.Authentication.AuthenticationProperties"/> to validate.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous validation operation.</returns>
        public virtual async Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            var currentUtc = DateTimeOffset.UtcNow;
            if (context.Options != null && context.Options.SystemClock != null)
            {
                currentUtc = context.Options.SystemClock.UtcNow;
            }
            var issuedUtc = context.Properties.IssuedUtc;

            // Only validate if enough time has elapsed
            var validate = (issuedUtc == null);
            if (issuedUtc != null)
            {
                var timeElapsed = currentUtc.Subtract(issuedUtc.Value);
                validate = timeElapsed > _options.SecurityStampValidationInterval;
            }
            if (validate)
            {
                var user = await _signInManager.ValidateSecurityStampAsync(context.Principal);
                if (user != null)
                {
                    // REVIEW: note we lost login authenticaiton method
                    context.ReplacePrincipal(await _signInManager.CreateUserPrincipalAsync(user));
                    context.ShouldRenew = true;
                }
                else
                {
                    context.RejectPrincipal();
                    await _signInManager.SignOutAsync();
                }
            }
        }
    }

    /// <summary>
    /// Static helper class used to configure a CookieAuthenticationNotifications to validate a cookie against a user's security
    /// stamp.
    /// </summary>
    public static class SecurityStampValidator
    {
        /// <summary>
        /// Validates a principal against a user's stored security stamp.
        /// the identity.
        /// </summary>
        /// <param name="context">The context containing the <see cref="System.Security.Claims.ClaimsPrincipal"/>
        /// and <see cref="Http.Authentication.AuthenticationProperties"/> to validate.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous validation operation.</returns>
        public static Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
        {
            if (context.HttpContext.RequestServices == null)
            {
                throw new InvalidOperationException("RequestServices is null.");
            }

            var validator = context.HttpContext.RequestServices.GetRequiredService<ISecurityStampValidator>();
            return validator.ValidateAsync(context);
        }
    }
}
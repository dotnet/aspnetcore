// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
        private readonly SecurityStampValidatorOptions _options;
        private ISystemClock _clock;

        /// <summary>
        /// Creates a new instance of <see cref="SecurityStampValidator{TUser}"/>.
        /// </summary>
        /// <param name="options">Used to access the <see cref="IdentityOptions"/>.</param>
        /// <param name="signInManager">The <see cref="SignInManager{TUser}"/>.</param>
        /// <param name="clock">The system clock.</param>
        public SecurityStampValidator(IOptions<SecurityStampValidatorOptions> options, SignInManager<TUser> signInManager, ISystemClock clock)
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
            _clock = clock;
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
            if (context.Options != null && _clock != null)
            {
                currentUtc = _clock.UtcNow;
            }
            var issuedUtc = context.Properties.IssuedUtc;

            // Only validate if enough time has elapsed
            var validate = (issuedUtc == null);
            if (issuedUtc != null)
            {
                var timeElapsed = currentUtc.Subtract(issuedUtc.Value);
                validate = timeElapsed > _options.ValidationInterval;
            }
            if (validate)
            {
                var user = await _signInManager.ValidateSecurityStampAsync(context.Principal);
                if (user != null)
                {
                    var newPrincipal = await _signInManager.CreateUserPrincipalAsync(user);

                    if (_options.OnRefreshingPrincipal != null)
                    {
                        var replaceContext = new SecurityStampRefreshingPrincipalContext
                        {
                            CurrentPrincipal = context.Principal,
                            NewPrincipal = newPrincipal
                        };

                        // Note: a null principal is allowed and results in a failed authentication.
                        await _options.OnRefreshingPrincipal(replaceContext);
                        newPrincipal = replaceContext.NewPrincipal;
                    }

                    // REVIEW: note we lost login authentication method
                    context.ReplacePrincipal(newPrincipal);
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
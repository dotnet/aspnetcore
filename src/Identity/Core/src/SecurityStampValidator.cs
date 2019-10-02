// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Provides default implementation of validation functions for security stamps.
    /// </summary>
    /// <typeparam name="TUser">The type encapsulating a user.</typeparam>
    public class SecurityStampValidator<TUser> : ISecurityStampValidator where TUser : class
    {
        /// <summary>
        /// Creates a new instance of <see cref="SecurityStampValidator{TUser}"/>.
        /// </summary>
        /// <param name="options">Used to access the <see cref="IdentityOptions"/>.</param>
        /// <param name="signInManager">The <see cref="SignInManager{TUser}"/>.</param>
        /// <param name="clock">The system clock.</param>
        /// <param name="logger">The logger.</param>
        public SecurityStampValidator(IOptions<SecurityStampValidatorOptions> options, SignInManager<TUser> signInManager, ISystemClock clock, ILoggerFactory logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (signInManager == null)
            {
                throw new ArgumentNullException(nameof(signInManager));
            }
            SignInManager = signInManager;
            Options = options.Value;
            Clock = clock;
            Logger = logger.CreateLogger(this.GetType().FullName);
        }

        /// <summary>
        /// The SignInManager.
        /// </summary>
        public SignInManager<TUser> SignInManager { get; }

        /// <summary>
        /// The <see cref="SecurityStampValidatorOptions"/>.
        /// </summary>
        public SecurityStampValidatorOptions Options { get; }

        /// <summary>
        /// The <see cref="ISystemClock"/>.
        /// </summary>
        public ISystemClock Clock { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/> used to log messages.
        /// </summary>
        /// <value>
        /// The <see cref="ILogger"/> used to log messages.
        /// </value>
        public ILogger Logger { get; set; }
        
        /// <summary>
        /// Called when the security stamp has been verified.
        /// </summary>
        /// <param name="user">The user who has been verified.</param>
        /// <param name="context">The <see cref="CookieValidatePrincipalContext"/>.</param>
        /// <returns>A task.</returns>
        protected virtual async Task SecurityStampVerified(TUser user, CookieValidatePrincipalContext context)
        {
            var newPrincipal = await SignInManager.CreateUserPrincipalAsync(user);

            if (Options.OnRefreshingPrincipal != null)
            {
                var replaceContext = new SecurityStampRefreshingPrincipalContext
                {
                    CurrentPrincipal = context.Principal,
                    NewPrincipal = newPrincipal
                };

                // Note: a null principal is allowed and results in a failed authentication.
                await Options.OnRefreshingPrincipal(replaceContext);
                newPrincipal = replaceContext.NewPrincipal;
            }

            // REVIEW: note we lost login authentication method
            context.ReplacePrincipal(newPrincipal);
            context.ShouldRenew = true;
        }

        /// <summary>
        /// Verifies the principal's security stamp, returns the matching user if successful
        /// </summary>
        /// <param name="principal">The principal to verify.</param>
        /// <returns>The verified user or null if verification fails.</returns>
        protected virtual Task<TUser> VerifySecurityStamp(ClaimsPrincipal principal)
            => SignInManager.ValidateSecurityStampAsync(principal);

        /// <summary>
        /// Validates a security stamp of an identity as an asynchronous operation, and rebuilds the identity if the validation succeeds, otherwise rejects
        /// the identity.
        /// </summary>
        /// <param name="context">The context containing the <see cref="System.Security.Claims.ClaimsPrincipal"/>
        /// and <see cref="AuthenticationProperties"/> to validate.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous validation operation.</returns>
        public virtual async Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            var currentUtc = DateTimeOffset.UtcNow;
            if (context.Options != null && Clock != null)
            {
                currentUtc = Clock.UtcNow;
            }
            var issuedUtc = context.Properties.IssuedUtc;

            // Only validate if enough time has elapsed
            var validate = (issuedUtc == null);
            if (issuedUtc != null)
            {
                var timeElapsed = currentUtc.Subtract(issuedUtc.Value);
                validate = timeElapsed > Options.ValidationInterval;
            }
            if (validate)
            {
                var user = await VerifySecurityStamp(context.Principal); 
                if (user != null)
                {
                    await SecurityStampVerified(user, context);
                }
                else
                {
                    Logger.LogDebug(0, "Security stamp validation failed, rejecting cookie.");
                    context.RejectPrincipal();
                    await SignInManager.SignOutAsync();
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
        /// </summary>
        /// <param name="context">The context containing the <see cref="System.Security.Claims.ClaimsPrincipal"/>
        /// and <see cref="AuthenticationProperties"/> to validate.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous validation operation.</returns>
        public static Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
            => ValidateAsync<ISecurityStampValidator>(context);

        /// <summary>
        /// Used to validate the <see cref="IdentityConstants.TwoFactorUserIdScheme"/> and 
        /// <see cref="IdentityConstants.TwoFactorRememberMeScheme"/> cookies against the user's 
        /// stored security stamp.
        /// </summary>
        /// <param name="context">The context containing the <see cref="System.Security.Claims.ClaimsPrincipal"/>
        /// and <see cref="AuthenticationProperties"/> to validate.</param>
        /// <returns></returns>

        public static Task ValidateAsync<TValidator>(CookieValidatePrincipalContext context) where TValidator : ISecurityStampValidator
        {
            if (context.HttpContext.RequestServices == null)
            {
                throw new InvalidOperationException("RequestServices is null.");
            }

            var validator = context.HttpContext.RequestServices.GetRequiredService<TValidator>();
            return validator.ValidateAsync(context);
        }
    }
}

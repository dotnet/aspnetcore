// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity;

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
    [Obsolete("ISystemClock is obsolete, use TimeProvider on SecurityStampValidatorOptions instead.")]
    public SecurityStampValidator(IOptions<SecurityStampValidatorOptions> options, SignInManager<TUser> signInManager, ISystemClock clock, ILoggerFactory logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(signInManager);
        SignInManager = signInManager;
        Options = options.Value;
        TimeProvider = Options.TimeProvider ?? TimeProvider.System;
        Clock = new TimeProviderClock(TimeProvider);
        Logger = logger.CreateLogger(GetType());
    }

    /// <summary>
    /// Creates a new instance of <see cref="SecurityStampValidator{TUser}"/>.
    /// </summary>
    /// <param name="options">Used to access the <see cref="IdentityOptions"/>.</param>
    /// <param name="signInManager">The <see cref="SignInManager{TUser}"/>.</param>
    /// <param name="logger">The logger.</param>
    public SecurityStampValidator(IOptions<SecurityStampValidatorOptions> options, SignInManager<TUser> signInManager, ILoggerFactory logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(signInManager);
        SignInManager = signInManager;
        Options = options.Value;
        TimeProvider = Options.TimeProvider ?? TimeProvider.System;
#pragma warning disable CS0618 // Type or member is obsolete
        Clock = new TimeProviderClock(TimeProvider);
#pragma warning restore CS0618 // Type or member is obsolete
        Logger = logger.CreateLogger(GetType());
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
    [Obsolete("ISystemClock is obsolete, use TimeProvider instead.")]
    public ISystemClock Clock { get; }

    /// <summary>
    /// The <see cref="System.TimeProvider"/>.
    /// </summary>
    public TimeProvider TimeProvider { get; }

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

        if (!context.Options.SlidingExpiration)
        {
            // On renewal calculate the new ticket length relative to now to avoid
            // extending the expiration.
            context.Properties.IssuedUtc = TimeProvider.GetUtcNow();
        }
    }

    /// <summary>
    /// Verifies the principal's security stamp, returns the matching user if successful
    /// </summary>
    /// <param name="principal">The principal to verify.</param>
    /// <returns>The verified user or null if verification fails.</returns>
    protected virtual Task<TUser?> VerifySecurityStamp(ClaimsPrincipal? principal)
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
        var currentUtc = TimeProvider.GetUtcNow();
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
                Logger.LogDebug(EventIds.SecurityStampValidationFailed, "Security stamp validation failed, rejecting cookie.");
                context.RejectPrincipal();
                await SignInManager.SignOutAsync();
                await SignInManager.Context.SignOutAsync(IdentityConstants.TwoFactorRememberMeScheme);
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

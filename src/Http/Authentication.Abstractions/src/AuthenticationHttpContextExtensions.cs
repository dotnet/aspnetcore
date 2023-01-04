// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Extension methods to expose Authentication on HttpContext.
/// </summary>
public static class AuthenticationHttpContextExtensions
{
    /// <summary>
    /// Authenticate the current request using the default authentication scheme.
    /// The default authentication scheme can be configured using <see cref="AuthenticationOptions.DefaultAuthenticateScheme"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <returns>The <see cref="AuthenticateResult"/>.</returns>
    public static Task<AuthenticateResult> AuthenticateAsync(this HttpContext context) =>
        context.AuthenticateAsync(scheme: null);

    /// <summary>
    /// Authenticate the current request using the specified scheme.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <param name="scheme">The name of the authentication scheme.</param>
    /// <returns>The <see cref="AuthenticateResult"/>.</returns>
    public static Task<AuthenticateResult> AuthenticateAsync(this HttpContext context, string? scheme) =>
        GetAuthenticationService(context).AuthenticateAsync(context, scheme);

    /// <summary>
    /// Challenge the current request using the specified scheme.
    /// An authentication challenge can be issued when an unauthenticated user requests an endpoint that requires authentication.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <param name="scheme">The name of the authentication scheme.</param>
    /// <returns>The result.</returns>
    public static Task ChallengeAsync(this HttpContext context, string? scheme) =>
        context.ChallengeAsync(scheme, properties: null);

    /// <summary>
    /// Challenge the current request using the default challenge scheme.
    /// An authentication challenge can be issued when an unauthenticated user requests an endpoint that requires authentication.
    /// The default challenge scheme can be configured using <see cref="AuthenticationOptions.DefaultChallengeScheme"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <returns>The task.</returns>
    public static Task ChallengeAsync(this HttpContext context) =>
        context.ChallengeAsync(scheme: null, properties: null);

    /// <summary>
    /// Challenge the current request using the default challenge scheme.
    /// An authentication challenge can be issued when an unauthenticated user requests an endpoint that requires authentication.
    /// The default challenge scheme can be configured using <see cref="AuthenticationOptions.DefaultChallengeScheme"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
    /// <returns>The task.</returns>
    public static Task ChallengeAsync(this HttpContext context, AuthenticationProperties? properties) =>
        context.ChallengeAsync(scheme: null, properties: properties);

    /// <summary>
    /// Challenge the current request using the specified scheme.
    /// An authentication challenge can be issued when an unauthenticated user requests an endpoint that requires authentication.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <param name="scheme">The name of the authentication scheme.</param>
    /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
    /// <returns>The task.</returns>
    public static Task ChallengeAsync(this HttpContext context, string? scheme, AuthenticationProperties? properties) =>
        GetAuthenticationService(context).ChallengeAsync(context, scheme, properties);

    /// <summary>
    /// Forbid the current request using the specified scheme.
    /// Forbid is used when an authenticated user attempts to access a resource they are not permitted to access.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <param name="scheme">The name of the authentication scheme.</param>
    /// <returns>The task.</returns>
    public static Task ForbidAsync(this HttpContext context, string? scheme) =>
        context.ForbidAsync(scheme, properties: null);

    /// <summary>
    /// Forbid the current request using the default forbid scheme.
    /// Forbid is used when an authenticated user attempts to access a resource they are not permitted to access.
    /// The default forbid scheme can be configured using <see cref="AuthenticationOptions.DefaultForbidScheme"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <returns>The task.</returns>
    public static Task ForbidAsync(this HttpContext context) =>
        context.ForbidAsync(scheme: null, properties: null);

    /// <summary>
    /// Forbid the current request using the default forbid scheme.
    /// Forbid is used when an authenticated user attempts to access a resource they are not permitted to access.
    /// The default forbid scheme can be configured using <see cref="AuthenticationOptions.DefaultForbidScheme"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
    /// <returns>The task.</returns>
    public static Task ForbidAsync(this HttpContext context, AuthenticationProperties? properties) =>
        context.ForbidAsync(scheme: null, properties: properties);

    /// <summary>
    /// Forbid the current request using the specified scheme.
    /// Forbid is used when an authenticated user attempts to access a resource they are not permitted to access.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <param name="scheme">The name of the authentication scheme.</param>
    /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
    /// <returns>The task.</returns>
    public static Task ForbidAsync(this HttpContext context, string? scheme, AuthenticationProperties? properties) =>
        GetAuthenticationService(context).ForbidAsync(context, scheme, properties);

    /// <summary>
    /// Sign in a principal for the specified scheme.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <param name="scheme">The name of the authentication scheme.</param>
    /// <param name="principal">The user.</param>
    /// <returns>The task.</returns>
    public static Task SignInAsync(this HttpContext context, string? scheme, ClaimsPrincipal principal) =>
        context.SignInAsync(scheme, principal, properties: null);

    /// <summary>
    /// Sign in a principal for the default authentication scheme.
    /// The default scheme for signing in can be configured using <see cref="AuthenticationOptions.DefaultSignInScheme"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <param name="principal">The user.</param>
    /// <returns>The task.</returns>
    public static Task SignInAsync(this HttpContext context, ClaimsPrincipal principal) =>
        context.SignInAsync(scheme: null, principal: principal, properties: null);

    /// <summary>
    /// Sign in a principal for the default authentication scheme.
    /// The default scheme for signing in can be configured using <see cref="AuthenticationOptions.DefaultSignInScheme"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <param name="principal">The user.</param>
    /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
    /// <returns>The task.</returns>
    public static Task SignInAsync(this HttpContext context, ClaimsPrincipal principal, AuthenticationProperties? properties) =>
        context.SignInAsync(scheme: null, principal: principal, properties: properties);

    /// <summary>
    /// Sign in a principal for the specified scheme.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <param name="scheme">The name of the authentication scheme.</param>
    /// <param name="principal">The user.</param>
    /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
    /// <returns>The task.</returns>
    public static Task SignInAsync(this HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) =>
        GetAuthenticationService(context).SignInAsync(context, scheme, principal, properties);

    /// <summary>
    /// Sign out a principal for the default authentication scheme.
    /// The default scheme for signing out can be configured using <see cref="AuthenticationOptions.DefaultSignOutScheme"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <returns>The task.</returns>
    public static Task SignOutAsync(this HttpContext context) => context.SignOutAsync(scheme: null, properties: null);

    /// <summary>
    /// Sign out a principal for the default authentication scheme.
    /// The default scheme for signing out can be configured using <see cref="AuthenticationOptions.DefaultSignOutScheme"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
    /// <returns>The task.</returns>
    public static Task SignOutAsync(this HttpContext context, AuthenticationProperties? properties) => context.SignOutAsync(scheme: null, properties: properties);

    /// <summary>
    /// Sign out a principal for the specified scheme.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <param name="scheme">The name of the authentication scheme.</param>
    /// <returns>The task.</returns>
    public static Task SignOutAsync(this HttpContext context, string? scheme) => context.SignOutAsync(scheme, properties: null);

    /// <summary>
    /// Sign out a principal for the specified scheme.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <param name="scheme">The name of the authentication scheme.</param>
    /// <param name="properties">The <see cref="AuthenticationProperties"/> properties.</param>
    /// <returns>The task.</returns>
    public static Task SignOutAsync(this HttpContext context, string? scheme, AuthenticationProperties? properties) =>
        GetAuthenticationService(context).SignOutAsync(context, scheme, properties);

    /// <summary>
    /// Authenticates the request using the specified scheme and returns the value for the token.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <param name="scheme">The name of the authentication scheme.</param>
    /// <param name="tokenName">The name of the token.</param>
    /// <returns>The value of the token if present.</returns>
    public static Task<string?> GetTokenAsync(this HttpContext context, string? scheme, string tokenName) =>
        GetAuthenticationService(context).GetTokenAsync(context, scheme, tokenName);

    /// <summary>
    /// Authenticates the request using the default authentication scheme and returns the value for the token.
    /// The default authentication scheme can be configured using <see cref="AuthenticationOptions.DefaultAuthenticateScheme"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    /// <param name="tokenName">The name of the token.</param>
    /// <returns>The value of the token if present.</returns>
    public static Task<string?> GetTokenAsync(this HttpContext context, string tokenName) =>
        GetAuthenticationService(context).GetTokenAsync(context, tokenName);

    // This project doesn't reference AuthenticationServiceCollectionExtensions.AddAuthentication so we use a string.
    private static IAuthenticationService GetAuthenticationService(HttpContext context) =>
        context.RequestServices.GetService<IAuthenticationService>() ??
            throw new InvalidOperationException(Resources.FormatException_UnableToFindServices(
                nameof(IAuthenticationService),
                nameof(IServiceCollection),
                "AddAuthentication"));
}

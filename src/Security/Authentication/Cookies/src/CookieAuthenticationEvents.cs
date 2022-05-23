// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// Allows subscribing to events raised during cookie authentication.
/// </summary>
public class CookieAuthenticationEvents
{
    /// <summary>
    /// Invoked to validate the principal.
    /// </summary>
    public Func<CookieValidatePrincipalContext, Task> OnValidatePrincipal { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked to check if the cookie should be renewed.
    /// </summary>
    public Func<CookieSlidingExpirationContext, Task> OnCheckSlidingExpiration { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked on signing in.
    /// </summary>
    public Func<CookieSigningInContext, Task> OnSigningIn { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked after sign in has completed.
    /// </summary>
    public Func<CookieSignedInContext, Task> OnSignedIn { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked on signing out.
    /// </summary>
    public Func<CookieSigningOutContext, Task> OnSigningOut { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked when the client needs to be redirected to the sign in url.
    /// </summary>
    public Func<RedirectContext<CookieAuthenticationOptions>, Task> OnRedirectToLogin { get; set; } = context =>
    {
        if (IsAjaxRequest(context.Request))
        {
            context.Response.Headers.Location = context.RedirectUri;
            context.Response.StatusCode = 401;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    };

    /// <summary>
    /// Invoked when the client needs to be redirected to the access denied url.
    /// </summary>
    public Func<RedirectContext<CookieAuthenticationOptions>, Task> OnRedirectToAccessDenied { get; set; } = context =>
    {
        if (IsAjaxRequest(context.Request))
        {
            context.Response.Headers.Location = context.RedirectUri;
            context.Response.StatusCode = 403;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    };

    /// <summary>
    /// Invoked when the client is to be redirected to logout.
    /// </summary>
    public Func<RedirectContext<CookieAuthenticationOptions>, Task> OnRedirectToLogout { get; set; } = context =>
    {
        if (IsAjaxRequest(context.Request))
        {
            context.Response.Headers.Location = context.RedirectUri;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    };

    /// <summary>
    /// Invoked when the client is to be redirected after logout.
    /// </summary>
    public Func<RedirectContext<CookieAuthenticationOptions>, Task> OnRedirectToReturnUrl { get; set; } = context =>
    {
        if (IsAjaxRequest(context.Request))
        {
            context.Response.Headers.Location = context.RedirectUri;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    };

    private static bool IsAjaxRequest(HttpRequest request)
    {
        return string.Equals(request.Query[HeaderNames.XRequestedWith], "XMLHttpRequest", StringComparison.Ordinal) ||
            string.Equals(request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.Ordinal);
    }

    /// <summary>
    /// Invoked to validate the principal.
    /// </summary>
    /// <param name="context">The <see cref="CookieValidatePrincipalContext"/>.</param>
    public virtual Task ValidatePrincipal(CookieValidatePrincipalContext context) => OnValidatePrincipal(context);

    /// <summary>
    /// Invoked to check if the cookie should be renewed.
    /// </summary>
    /// <param name="context">The <see cref="CookieSlidingExpirationContext"/>.</param>
    public virtual Task CheckSlidingExpiration(CookieSlidingExpirationContext context) => OnCheckSlidingExpiration(context);

    /// <summary>
    /// Invoked during sign in.
    /// </summary>
    /// <param name="context">The <see cref="CookieSigningInContext"/>.</param>
    public virtual Task SigningIn(CookieSigningInContext context) => OnSigningIn(context);

    /// <summary>
    /// Invoked after sign in has completed.
    /// </summary>
    /// <param name="context">The <see cref="CookieSignedInContext"/>.</param>
    public virtual Task SignedIn(CookieSignedInContext context) => OnSignedIn(context);

    /// <summary>
    /// Invoked on sign out.
    /// </summary>
    /// <param name="context">The <see cref="CookieSigningOutContext"/>.</param>
    public virtual Task SigningOut(CookieSigningOutContext context) => OnSigningOut(context);

    /// <summary>
    /// Invoked when the client is being redirected to the log out url.
    /// </summary>
    /// <param name="context">The <see cref="RedirectContext{TOptions}"/>.</param>
    public virtual Task RedirectToLogout(RedirectContext<CookieAuthenticationOptions> context) => OnRedirectToLogout(context);

    /// <summary>
    /// Invoked when the client is being redirected to the log in url.
    /// </summary>
    /// <param name="context">The <see cref="RedirectContext{TOptions}"/>.</param>
    public virtual Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context) => OnRedirectToLogin(context);

    /// <summary>
    /// Invoked when the client is being redirected after log out.
    /// </summary>
    /// <param name="context">The <see cref="RedirectContext{TOptions}"/>.</param>
    public virtual Task RedirectToReturnUrl(RedirectContext<CookieAuthenticationOptions> context) => OnRedirectToReturnUrl(context);

    /// <summary>
    /// Invoked when the client is being redirected to the access denied url.
    /// </summary>
    /// <param name="context">The <see cref="RedirectContext{TOptions}"/>.</param>
    public virtual Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context) => OnRedirectToAccessDenied(context);
}

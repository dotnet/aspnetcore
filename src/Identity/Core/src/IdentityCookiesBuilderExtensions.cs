// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Helper functions for configuring identity services.
/// </summary>
public static class IdentityCookieAuthenticationBuilderExtensions
{
    /// <summary>
    /// Adds cookie authentication.
    /// </summary>
    /// <param name="builder">The current <see cref="AuthenticationBuilder"/> instance.</param>
    /// <returns>The <see cref="IdentityCookiesBuilder"/> which can be used to configure the identity cookies.</returns>
    public static IdentityCookiesBuilder AddIdentityCookies(this AuthenticationBuilder builder)
        => builder.AddIdentityCookies(o => { });

    /// <summary>
    /// Adds the cookie authentication needed for sign in manager.
    /// </summary>
    /// <param name="builder">The current <see cref="AuthenticationBuilder"/> instance.</param>
    /// <param name="configureCookies">Action used to configure the cookies.</param>
    /// <returns>The <see cref="IdentityCookiesBuilder"/> which can be used to configure the identity cookies.</returns>
    public static IdentityCookiesBuilder AddIdentityCookies(this AuthenticationBuilder builder, Action<IdentityCookiesBuilder> configureCookies)
    {
        var cookieBuilder = new IdentityCookiesBuilder();
        cookieBuilder.ApplicationCookie = builder.AddApplicationCookie();
        cookieBuilder.ExternalCookie = builder.AddExternalCookie();
        cookieBuilder.TwoFactorRememberMeCookie = builder.AddTwoFactorRememberMeCookie();
        cookieBuilder.TwoFactorUserIdCookie = builder.AddTwoFactorUserIdCookie();
        configureCookies?.Invoke(cookieBuilder);
        return cookieBuilder;
    }

    /// <summary>
    /// Adds the identity application cookie.
    /// </summary>
    /// <param name="builder">The current <see cref="AuthenticationBuilder"/> instance.</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> which can be used to configure the cookie authentication.</returns>
    public static OptionsBuilder<CookieAuthenticationOptions> AddApplicationCookie(this AuthenticationBuilder builder)
    {
        builder.AddCookie(IdentityConstants.ApplicationScheme, o =>
        {
            o.LoginPath = new PathString("/Account/Login");
            o.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync
            };
        });
        return new OptionsBuilder<CookieAuthenticationOptions>(builder.Services, IdentityConstants.ApplicationScheme);
    }

    /// <summary>
    /// Adds the identity cookie used for external logins.
    /// </summary>
    /// <param name="builder">The current <see cref="AuthenticationBuilder"/> instance.</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> which can be used to configure the cookie authentication.</returns>
    public static OptionsBuilder<CookieAuthenticationOptions> AddExternalCookie(this AuthenticationBuilder builder)
    {
        builder.AddCookie(IdentityConstants.ExternalScheme, o =>
        {
            o.Cookie.Name = IdentityConstants.ExternalScheme;
            o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        });
        return new OptionsBuilder<CookieAuthenticationOptions>(builder.Services, IdentityConstants.ExternalScheme);
    }

    /// <summary>
    /// Adds the identity cookie used for two factor remember me.
    /// </summary>
    /// <param name="builder">The current <see cref="AuthenticationBuilder"/> instance.</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> which can be used to configure the cookie authentication.</returns>
    public static OptionsBuilder<CookieAuthenticationOptions> AddTwoFactorRememberMeCookie(this AuthenticationBuilder builder)
    {
        builder.AddCookie(IdentityConstants.TwoFactorRememberMeScheme, o =>
        {
            o.Cookie.Name = IdentityConstants.TwoFactorRememberMeScheme;
            o.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = SecurityStampValidator.ValidateAsync<ITwoFactorSecurityStampValidator>
            };
        });
        return new OptionsBuilder<CookieAuthenticationOptions>(builder.Services, IdentityConstants.TwoFactorRememberMeScheme);
    }

    /// <summary>
    /// Adds the identity cookie used for two factor logins.
    /// </summary>
    /// <param name="builder">The current <see cref="AuthenticationBuilder"/> instance.</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> which can be used to configure the cookie authentication.</returns>
    public static OptionsBuilder<CookieAuthenticationOptions> AddTwoFactorUserIdCookie(this AuthenticationBuilder builder)
    {
        builder.AddCookie(IdentityConstants.TwoFactorUserIdScheme, o =>
        {
            o.Cookie.Name = IdentityConstants.TwoFactorUserIdScheme;
            o.Events = new CookieAuthenticationEvents
            {
                OnRedirectToReturnUrl = _ => Task.CompletedTask
            };
            o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        });
        return new OptionsBuilder<CookieAuthenticationOptions>(builder.Services, IdentityConstants.TwoFactorUserIdScheme);
    }
}

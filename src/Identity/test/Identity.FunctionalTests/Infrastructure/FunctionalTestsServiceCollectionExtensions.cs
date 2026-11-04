// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Security.Claims;
using Identity.DefaultUI.WebSite;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.FunctionalTests;

public static class FunctionalTestsServiceCollectionExtensions
{
    public static IServiceCollection SetupTestDatabase<TContext>(this IServiceCollection services, DbConnection connection) where TContext : DbContext =>
        services.ConfigureDbContext<TContext>((sp, options) => options
            .ConfigureWarnings(b => b.Log(CoreEventId.ManyServiceProvidersCreatedWarning))
            .UseSqlite(connection),
            ServiceLifetime.Scoped);

    public static IServiceCollection SetupTestThirdPartyLogin(this IServiceCollection services) =>
        services.AddAuthentication()
            .AddContosoAuthentication(o => o.SignInScheme = IdentityConstants.ExternalScheme)
            .Services;

    public static IServiceCollection SetupTestEmailSender(this IServiceCollection services, IEmailSender sender) =>
        services.AddSingleton(sender);

    public static IServiceCollection SetupGetUserClaimsPrincipal(this IServiceCollection services, Action<ClaimsPrincipal> captureUser, string schemeName) =>
        services.Configure<CookieAuthenticationOptions>(schemeName, o => o.Events.OnSigningIn = context =>
        {
            captureUser(context.Principal);
            return Task.CompletedTask;
        });

    public static IServiceCollection SetupEmailRequired(this IServiceCollection services) =>
        services.Configure<IdentityOptions>(o => o.SignIn.RequireConfirmedEmail = true);

    public static IServiceCollection SetupGlobalAuthorizeFilter(this IServiceCollection services) =>
        services.AddMvc(config =>
        {
            var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
            config.Filters.Add(new AuthorizeFilter(policy));
        })
        .Services;

    public static IServiceCollection SetupMaxFailedAccessAttempts(this IServiceCollection services) =>
        services.Configure<IdentityOptions>(o => o.Lockout.MaxFailedAccessAttempts = 0);
}

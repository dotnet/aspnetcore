// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Identity.DefaultUI.WebSite;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public static class FunctionalTestsServiceCollectionExtensions
    {
        public static IServiceCollection SetupTestDatabase<TContext>(this IServiceCollection services, string databaseName) where TContext : DbContext =>
            services.AddDbContext<TContext>(options =>
                options.UseInMemoryDatabase(databaseName, memoryOptions => { }));

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
}

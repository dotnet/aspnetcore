// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class IdentityServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureIdentity(this IServiceCollection services, Action<IdentityOptions> configure)
        {
            return services.Configure(configure);
        }

        public static IdentityBuilder AddIdentity(this IServiceCollection services)
        {
            return services.AddIdentity<IdentityUser, IdentityRole>();
        }

        public static IdentityBuilder AddIdentity(
            this IServiceCollection services, 
            IConfiguration identityConfig = null,
            Action<IdentityOptions> configureOptions = null,
            bool useDefaultSubKey = true)
        {
            return services.AddIdentity<IdentityUser, IdentityRole>(identityConfig, configureOptions, useDefaultSubKey);
        }

        public static IdentityBuilder AddIdentity<TUser, TRole>(
            this IServiceCollection services, 
            IConfiguration identityConfig = null, 
            Action<IdentityOptions> configureOptions = null, 
            bool useDefaultSubKey = true)
            where TUser : class
            where TRole : class
        {
            if (identityConfig != null)
            {
                if (useDefaultSubKey)
                {
                    identityConfig = identityConfig.GetSubKey("identity");
                }
                services.Configure<IdentityOptions>(identityConfig);
            }
            var describe = new ServiceDescriber(identityConfig);

            // Services used by identity
            services.AddOptions(identityConfig);
            services.AddDataProtection(identityConfig);
            services.AddLogging(identityConfig);
            services.TryAdd(describe.Singleton<IHttpContextAccessor, HttpContextAccessor>());

            // Identity services
            services.TryAdd(describe.Transient<IUserValidator<TUser>, UserValidator<TUser>>());
            services.TryAdd(describe.Transient<IPasswordValidator<TUser>, PasswordValidator<TUser>>());
            services.TryAdd(describe.Transient<IPasswordHasher<TUser>, PasswordHasher<TUser>>());
            services.TryAdd(describe.Transient<ILookupNormalizer, UpperInvariantLookupNormalizer>());
            services.TryAdd(describe.Transient<IRoleValidator<TRole>, RoleValidator<TRole>>());
            // No interface for the error describer so we can add errors without rev'ing the interface
            services.TryAdd(describe.Transient<IdentityErrorDescriber, IdentityErrorDescriber>());
            services.TryAdd(describe.Scoped<ISecurityStampValidator, SecurityStampValidator<TUser>>());
            services.TryAdd(describe.Scoped<IUserClaimsPrincipalFactory<TUser>, UserClaimsPrincipalFactory<TUser, TRole>>());
            services.TryAdd(describe.Scoped<UserManager<TUser>, UserManager<TUser>>());
            services.TryAdd(describe.Scoped<SignInManager<TUser>, SignInManager<TUser>>());
            services.TryAdd(describe.Scoped<RoleManager<TRole>, RoleManager<TRole>>());

            if (configureOptions != null)
            {
                services.ConfigureIdentity(configureOptions);
            }
            services.Configure<ExternalAuthenticationOptions>(options =>
            {
                options.SignInScheme = IdentityOptions.ExternalCookieAuthenticationScheme;
            });

            // Configure all of the cookie middlewares
            services.Configure<CookieAuthenticationOptions>(options =>
            {
                options.AuthenticationScheme = IdentityOptions.ApplicationCookieAuthenticationScheme;
                options.LoginPath = new PathString("/Account/Login");
                options.Notifications = new CookieAuthenticationNotifications
                {
                    OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync
                };
            }, IdentityOptions.ApplicationCookieAuthenticationScheme);
            services.Configure<CookieAuthenticationOptions>(options =>
            {
                options.AuthenticationScheme = IdentityOptions.ExternalCookieAuthenticationScheme;
                options.AutomaticAuthentication = false;
                options.CookieName = IdentityOptions.ExternalCookieAuthenticationScheme;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            }, IdentityOptions.ExternalCookieAuthenticationScheme);
            services.Configure<CookieAuthenticationOptions>(options =>
            {
                options.AuthenticationScheme = IdentityOptions.TwoFactorRememberMeCookieAuthenticationScheme;
                options.AutomaticAuthentication = false;
                options.CookieName = IdentityOptions.TwoFactorRememberMeCookieAuthenticationScheme;
            }, IdentityOptions.TwoFactorRememberMeCookieAuthenticationScheme);
            services.Configure<CookieAuthenticationOptions>(options =>
            {
                options.AuthenticationScheme = IdentityOptions.TwoFactorUserIdCookieAuthenticationScheme;
                options.AutomaticAuthentication = false;
                options.CookieName = IdentityOptions.TwoFactorUserIdCookieAuthenticationScheme;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            }, IdentityOptions.TwoFactorUserIdCookieAuthenticationScheme);

            return new IdentityBuilder(typeof(TUser), typeof(TRole), services);
        }
    }
}
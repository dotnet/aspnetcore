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

        public static IServiceCollection ConfigureIdentity(this IServiceCollection services, IConfiguration config)
        {
            return services.Configure<IdentityOptions>(config);
        }

        public static IServiceCollection ConfigureIdentityApplicationCookie(this IServiceCollection services, Action<CookieAuthenticationOptions> configureOptions)
        {
            return services.Configure<CookieAuthenticationOptions>(configureOptions, IdentityOptions.ApplicationCookieAuthenticationScheme);
        }

        public static IdentityBuilder AddIdentity<TUser, TRole>(
            this IServiceCollection services)
            where TUser : class
            where TRole : class
        {
            return services.AddIdentity<TUser, TRole>(configureOptions: null);
        }

        public static IdentityBuilder AddIdentity<TUser, TRole>(
            this IServiceCollection services, 
            Action<IdentityOptions> configureOptions)
            where TUser : class
            where TRole : class
        {
            // Services used by identity
            services.AddOptions();
            services.AddDataProtection();
            services.AddLogging();
            services.TryAdd(ServiceDescriptor.Singleton<IHttpContextAccessor, HttpContextAccessor>());

            // Identity services
            services.TryAdd(ServiceDescriptor.Transient<IUserValidator<TUser>, UserValidator<TUser>>());
            services.TryAdd(ServiceDescriptor.Transient<IPasswordValidator<TUser>, PasswordValidator<TUser>>());
            services.TryAdd(ServiceDescriptor.Transient<IPasswordHasher<TUser>, PasswordHasher<TUser>>());
            services.TryAdd(ServiceDescriptor.Transient<ILookupNormalizer, UpperInvariantLookupNormalizer>());
            services.TryAdd(ServiceDescriptor.Transient<IRoleValidator<TRole>, RoleValidator<TRole>>());
            // No interface for the error describer so we can add errors without rev'ing the interface
            services.TryAdd(ServiceDescriptor.Transient<IdentityErrorDescriber, IdentityErrorDescriber>());
            services.TryAdd(ServiceDescriptor.Scoped<ISecurityStampValidator, SecurityStampValidator<TUser>>());
            services.TryAdd(ServiceDescriptor.Scoped<IUserClaimsPrincipalFactory<TUser>, UserClaimsPrincipalFactory<TUser, TRole>>());
            services.TryAdd(ServiceDescriptor.Scoped<UserManager<TUser>, UserManager<TUser>>());
            services.TryAdd(ServiceDescriptor.Scoped<SignInManager<TUser>, SignInManager<TUser>>());
            services.TryAdd(ServiceDescriptor.Scoped<RoleManager<TRole>, RoleManager<TRole>>());

            if (configureOptions != null)
            {
                services.ConfigureIdentity(configureOptions);
            }
            services.Configure<ExternalAuthenticationOptions>(options =>
            {
                options.SignInScheme = IdentityOptions.ExternalCookieAuthenticationScheme;
            });

            // Configure all of the cookie middlewares
            services.ConfigureIdentityApplicationCookie(options =>
            {
                options.AuthenticationScheme = IdentityOptions.ApplicationCookieAuthenticationScheme;
                options.AutomaticAuthentication = true;
                options.LoginPath = new PathString("/Account/Login");
                options.Notifications = new CookieAuthenticationNotifications
                {
                    OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync
                };
            });
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
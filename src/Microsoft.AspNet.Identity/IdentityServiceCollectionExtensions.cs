// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Identity;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.AspNet.Security.DataProtection;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Security;

namespace Microsoft.Framework.DependencyInjection
{
    public static class IdentityServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureIdentity(this IServiceCollection services, Action<IdentityOptions> configure)
        {
            return services.Configure(configure);
        }

        public static IdentityBuilder<IdentityUser, IdentityRole> AddIdentity(this IServiceCollection services, 
            IConfiguration identityConfig = null, Action<IdentityOptions> configureOptions = null)
        {
            return services.AddIdentity<IdentityUser, IdentityRole>(identityConfig, configureOptions);
        }

        public static IdentityBuilder<IdentityUser, IdentityRole> AddIdentity(this IServiceCollection services)
        {
            return services.AddIdentity<IdentityUser, IdentityRole>();
        }

        public static IdentityBuilder<TUser, TRole> AddIdentity<TUser, TRole>(this IServiceCollection services, 
            IConfiguration identityConfig = null, Action<IdentityOptions> configureOptions = null)
            where TUser : class
            where TRole : class
        {
            if (identityConfig != null)
            {
                services.Configure<IdentityOptions>(identityConfig);
            }
            if (configureOptions != null)
            {
                services.ConfigureIdentity(configureOptions);
            }

            services.Add(IdentityServices.GetDefaultServices<TUser, TRole>(identityConfig));
            services.AddScoped<UserManager<TUser>>();
            services.AddScoped<SignInManager<TUser>>();
            services.AddScoped<ISecurityStampValidator, SecurityStampValidator<TUser>>();
            services.AddScoped<RoleManager<TRole>>();
            services.AddScoped<IClaimsIdentityFactory<TUser>, ClaimsIdentityFactory<TUser, TRole>>();

            services.Configure<ExternalAuthenticationOptions>(options =>
            {
                options.SignInAsAuthenticationType = IdentityOptions.ExternalCookieAuthenticationType;
            });

            services.Configure<CookieAuthenticationOptions>(options =>
            {
                options.AuthenticationType = IdentityOptions.ApplicationCookieAuthenticationType;
                //CookieName = ".AspNet.Identity." + ClaimsIdentityOptions.DefaultAuthenticationType,
                options.LoginPath = new PathString("/Account/Login");
                options.Notifications = new CookieAuthenticationNotifications
                {
                    OnValidateIdentity = SecurityStampValidator.ValidateIdentityAsync
                };
            }, IdentityOptions.ApplicationCookieAuthenticationType);

            services.Configure<CookieAuthenticationOptions>(options =>
            {
                options.AuthenticationType = IdentityOptions.ExternalCookieAuthenticationType;
                options.AuthenticationMode = AuthenticationMode.Passive;
                options.CookieName = IdentityOptions.ExternalCookieAuthenticationType;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            }, IdentityOptions.ExternalCookieAuthenticationType);

            services.Configure<CookieAuthenticationOptions>(options =>
            {
                options.AuthenticationType = IdentityOptions.TwoFactorRememberMeCookieAuthenticationType;
                options.AuthenticationMode = AuthenticationMode.Passive;
                options.CookieName = IdentityOptions.TwoFactorRememberMeCookieAuthenticationType;
            }, IdentityOptions.TwoFactorRememberMeCookieAuthenticationType);

            services.Configure<CookieAuthenticationOptions>(options =>
            {
                options.AuthenticationType = IdentityOptions.TwoFactorUserIdCookieAuthenticationType;
                options.AuthenticationMode = AuthenticationMode.Passive;
                options.CookieName = IdentityOptions.TwoFactorUserIdCookieAuthenticationType;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            }, IdentityOptions.TwoFactorUserIdCookieAuthenticationType);

            return new IdentityBuilder<TUser, TRole>(services);
        }

        public static IdentityBuilder<TUser, TRole> AddDefaultIdentity<TUser, TRole>(this IServiceCollection services, IConfiguration config = null, Action<IdentityOptions> configureOptions = null)
            where TUser : class
            where TRole : class
        {
            return services.AddIdentity<TUser, TRole>(config, configureOptions)
                .AddTokenProvider(new DataProtectorTokenProvider<TUser>(
                    new DataProtectionTokenProviderOptions
                    {
                        Name = Resources.DefaultTokenProvider,
                    },
                    // TODO: This needs to get IDataProtectionProvider from the environment 
                    new EphemeralDataProtectionProvider().CreateProtector("ASP.NET Identity")))
                .AddTokenProvider(new PhoneNumberTokenProvider<TUser>())
                .AddTokenProvider(new EmailTokenProvider<TUser>());
        }

        public static IdentityBuilder<TUser, IdentityRole> AddIdentity<TUser>(this IServiceCollection services)
            where TUser : class
        {
            return services.AddIdentity<TUser, IdentityRole>();
        }

        public static IdentityBuilder<TUser, IdentityRole> AddIdentity<TUser>(this IServiceCollection services, 
            IConfiguration identityConfig)
            where TUser : class
        {
            return services.AddIdentity<TUser, IdentityRole>(identityConfig);
        }
    }
}
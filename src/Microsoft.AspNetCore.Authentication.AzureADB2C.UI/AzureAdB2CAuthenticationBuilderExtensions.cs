// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Extension methods to add Azure Active Directory B2C Authentication to your application.
    /// </summary>
    public static class AzureADB2CAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Adds JWT Bearer authentication to your app for Azure AD B2C Applications.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="configureOptions">The <see cref="Action{AzureADB2COptions}"/> to configure the
        /// <see cref="AzureADB2COptions"/>.
        /// </param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddAzureADB2CBearer(this AuthenticationBuilder builder, Action<AzureADB2COptions> configureOptions) => 
            builder.AddAzureADB2CBearer(
                AzureADB2CDefaults.BearerAuthenticationScheme,
                AzureADB2CDefaults.JwtBearerAuthenticationScheme,
                configureOptions);

        /// <summary>
        /// Adds JWT Bearer authentication to your app for Azure AD B2C Applications.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="scheme">The identifier for the virtual scheme.</param>
        /// <param name="jwtBearerScheme">The identifier for the underlying JWT Bearer scheme.</param>
        /// <param name="configureOptions">The <see cref="Action{AzureADB2COptions}"/> to configure the
        /// <see cref="AzureADB2COptions"/>.
        /// </param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddAzureADB2CBearer(
            this AuthenticationBuilder builder,
            string scheme,
            string jwtBearerScheme,
            Action<AzureADB2COptions> configureOptions) {

            builder.AddVirtualScheme(scheme, displayName: null, configureOptions: o =>
            {
                o.Default = jwtBearerScheme;
            });

            builder.Services.Configure(TryAddJwtBearerSchemeMapping(scheme, jwtBearerScheme));

            builder.Services.TryAddSingleton<IConfigureOptions<AzureADB2COptions>, AzureADB2COptionsConfiguration>();

            builder.Services.TryAddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerOptionsConfiguration>();

            builder.Services.Configure(scheme, configureOptions);
            builder.AddJwtBearer();

            return builder;
        }

        /// <summary>
        /// Adds Azure Active Directory B2C Authentication to your application.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="configureOptions">The <see cref="Action{AzureADB2COptions}"/> to configure the
        /// <see cref="AzureADB2COptions"/>
        /// </param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddAzureADB2C(this AuthenticationBuilder builder, Action<AzureADB2COptions> configureOptions) =>
            builder.AddAzureADB2C(
                AzureADB2CDefaults.AuthenticationScheme,
                AzureADB2CDefaults.OpenIdScheme,
                AzureADB2CDefaults.CookieScheme,
                AzureADB2CDefaults.DisplayName,
                configureOptions);

        /// <summary>
        /// Adds Azure Active Directory B2C Authentication to your application.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="scheme">The identifier for the virtual scheme.</param>
        /// <param name="openIdConnectScheme">The identifier for the underlying Open ID Connect scheme.</param>
        /// <param name="cookieScheme">The identifier for the underlying cookie scheme.</param>
        /// <param name="displayName">The display name for the scheme.</param>
        /// <param name="configureOptions">The <see cref="Action{AzureADB2COptions}"/> to configure the
        /// <see cref="AzureADB2COptions"/>
        /// </param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddAzureADB2C(
            this AuthenticationBuilder builder,
            string scheme,
            string openIdConnectScheme,
            string cookieScheme,
            string displayName,
            Action<AzureADB2COptions> configureOptions)
        {
            AddAdditionalMvcApplicationParts(builder.Services);
            builder.AddVirtualScheme(scheme, displayName, o =>
            {
                o.Default = cookieScheme;
                o.Challenge = openIdConnectScheme;
            });

            builder.Services.Configure(TryAddOpenIDCookieSchemeMappings(scheme, openIdConnectScheme, cookieScheme));

            builder.Services.TryAddSingleton<IConfigureOptions<AzureADB2COptions>, AzureADB2COptionsConfiguration>();

            builder.Services.TryAddSingleton<IConfigureOptions<OpenIdConnectOptions>, OpenIdConnectOptionsConfiguration>();

            builder.Services.TryAddSingleton<IConfigureOptions<CookieAuthenticationOptions>, CookieOptionsConfiguration>();

            builder.Services.Configure(scheme, configureOptions);

            builder.AddOpenIdConnect(openIdConnectScheme, null, o => { });
            builder.AddCookie(cookieScheme, null, o => { });

            return builder;
        }

        private static Action<AzureADB2CSchemeOptions> TryAddJwtBearerSchemeMapping(string scheme, string jwtBearerScheme)
        {
            return TryAddMapping;

            void TryAddMapping(AzureADB2CSchemeOptions o)
            {
                if (o.JwtBearerMappings.ContainsKey(scheme))
                {
                    throw new InvalidOperationException($"A scheme with the name '{scheme}' was already added.");
                }
                foreach (var mapping in o.JwtBearerMappings)
                {
                    if (mapping.Value.JwtBearerScheme == jwtBearerScheme)
                    {
                        throw new InvalidOperationException(
                            $"The JSON Web Token Bearer scheme '{jwtBearerScheme}' can't be associated with the Azure Active Directory B2C scheme '{scheme}'. " +
                            $"The JSON Web Token Bearer scheme '{jwtBearerScheme}' is already mapped to the Azure Active Directory B2C scheme '{mapping.Key}'");
                    }
                }
                o.JwtBearerMappings.Add(scheme, new AzureADB2CSchemeOptions.JwtBearerSchemeMapping
                {
                    JwtBearerScheme = jwtBearerScheme
                });
            };
        }

        private static Action<AzureADB2CSchemeOptions> TryAddOpenIDCookieSchemeMappings(string scheme, string openIdConnectScheme, string cookieScheme)
        {
            return TryAddMapping;

            void TryAddMapping(AzureADB2CSchemeOptions o)
            {
                if (o.OpenIDMappings.ContainsKey(scheme))
                {
                    throw new InvalidOperationException($"A scheme with the name '{scheme}' was already added.");
                }
                foreach (var mapping in o.OpenIDMappings)
                {
                    if (mapping.Value.CookieScheme == cookieScheme)
                    {
                        throw new InvalidOperationException(
                            $"The cookie scheme '{cookieScheme}' can't be associated with the Azure Active Directory B2C scheme '{scheme}'. " +
                            $"The cookie scheme '{cookieScheme}' is already mapped to the Azure Active Directory B2C scheme '{mapping.Key}'");
                    }

                    if (mapping.Value.OpenIdConnectScheme == openIdConnectScheme)
                    {
                        throw new InvalidOperationException(
                            $"The Open ID Connect scheme '{openIdConnectScheme}' can't be associated with the Azure Active Directory B2C scheme '{scheme}'. " +
                            $"The Open ID Connect scheme '{openIdConnectScheme}' is already mapped to the Azure Active Directory B2C scheme '{mapping.Key}'");
                    }
                }
                o.OpenIDMappings.Add(scheme, new AzureADB2CSchemeOptions.AzureADB2COpenIDSchemeMapping
                {
                    OpenIdConnectScheme = openIdConnectScheme,
                    CookieScheme = cookieScheme
                });
            };
        }

        private static void AddAdditionalMvcApplicationParts(IServiceCollection services)
        {
            var thisAssembly = typeof(AzureADB2CAuthenticationBuilderExtensions).Assembly;
            var additionalReferences = thisAssembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Where(am => string.Equals(am.Key, "Microsoft.AspNetCore.Mvc.AdditionalReference"))
                .Select(am => am.Value.Split(',')[0])
                .ToArray();

            var mvcBuilder = services
                .AddMvc()
                .AddRazorPagesOptions(o => o.AllowAreas = true)
                .ConfigureApplicationPartManager(apm =>
                {
                    foreach (var reference in additionalReferences)
                    {
                        var fileName = Path.GetFileName(reference);
                        var filePath = Path.Combine(Path.GetDirectoryName(thisAssembly.Location), fileName);
                        var additionalAssembly = LoadAssembly(filePath);
                        // This needs to change to additional assembly part.
                        var additionalPart = new AdditionalAssemblyPart(additionalAssembly);
                        if (!apm.ApplicationParts.Any(ap => HasSameName(ap.Name, additionalPart.Name)))
                        {
                            apm.ApplicationParts.Add(additionalPart);
                        }
                    }

                    apm.FeatureProviders.Add(new AzureADB2CAccountControllerFeatureProvider());
                });

            bool HasSameName(string left, string right) => string.Equals(left, right, StringComparison.Ordinal);
        }

        private static Assembly LoadAssembly(string filePath)
        {
            Assembly viewsAssembly = null;
            if (File.Exists(filePath))
            {
                try
                {
                    viewsAssembly = Assembly.LoadFile(filePath);
                }
                catch (FileLoadException)
                {
                    throw new InvalidOperationException("Unable to load the precompiled views assembly in " +
                        $"'{filePath}'.");
                }
            }
            else
            {
                throw new InvalidOperationException("Could not find the precompiled views assembly for 'Microsoft.AspNetCore.Authentication.AzureADB2C.UI' at " +
                    $"'{filePath}'.");
            }

            return viewsAssembly;
        }
    }
}

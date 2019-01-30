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
using System.Collections.Generic;

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
            Action<AzureADB2COptions> configureOptions)
        {
            builder.AddPolicyScheme(scheme, displayName: null, configureOptions: o =>
            {
                o.ForwardDefault = jwtBearerScheme;
            });

            builder.Services.Configure(TryAddJwtBearerSchemeMapping(scheme, jwtBearerScheme));

            builder.Services.TryAddSingleton<IConfigureOptions<AzureADB2COptions>, AzureADB2COptionsConfiguration>();

            builder.Services.TryAddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerOptionsConfiguration>();

            builder.Services.Configure(scheme, configureOptions);
            builder.AddJwtBearer(jwtBearerScheme, o => { });

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
            builder.AddPolicyScheme(scheme, displayName, o =>
            {
                o.ForwardDefault = cookieScheme;
                o.ForwardChallenge = openIdConnectScheme;
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
            var additionalParts = GetAdditionalParts();
            var mvcBuilder = services
                .AddMvc()
                .ConfigureApplicationPartManager(apm =>
                {
                    foreach (var part in additionalParts)
                    {
                        if (!apm.ApplicationParts.Any(ap => HasSameName(ap.Name, part.Name)))
                        {
                            apm.ApplicationParts.Add(part);
                        }
                    }

                    apm.FeatureProviders.Add(new AzureADB2CAccountControllerFeatureProvider());
                });

            bool HasSameName(string left, string right) => string.Equals(left, right, StringComparison.Ordinal);
        }

        private static IEnumerable<ApplicationPart> GetAdditionalParts()
        {
            var thisAssembly = typeof(AzureADB2CAuthenticationBuilderExtensions).Assembly;
            var relatedAssemblies = RelatedAssemblyAttribute.GetRelatedAssemblies(thisAssembly, throwOnError: true);

            foreach (var reference in relatedAssemblies)
            {
                yield return new CompiledRazorAssemblyPart(reference);
            }
        }
    }
}

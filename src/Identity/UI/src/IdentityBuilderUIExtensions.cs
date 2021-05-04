// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Default UI extensions to <see cref="IdentityBuilder"/>.
    /// </summary>
    public static class IdentityBuilderUIExtensions
    {
        /// <summary>
        /// Adds a default, self-contained UI for Identity to the application using
        /// Razor Pages in an area named Identity.
        /// </summary>
        /// <remarks>
        /// In order to use the default UI, the application must be using <see cref="Microsoft.AspNetCore.Mvc"/>,
        /// <see cref="Microsoft.AspNetCore.StaticFiles"/> and contain a <c>_LoginPartial</c> partial view that
        /// can be found by the application.
        /// </remarks>
        /// <param name="builder">The <see cref="IdentityBuilder"/>.</param>
        /// <returns>The <see cref="IdentityBuilder"/>.</returns>
        public static IdentityBuilder AddDefaultUI(this IdentityBuilder builder)
        {
            builder.AddSignInManager();
            builder.Services.AddMvc();

            builder.Services.ConfigureOptions(
                typeof(IdentityDefaultUIConfigureOptions<>)
                    .MakeGenericType(builder.UserType));
            builder.Services.TryAddTransient<IEmailSender, EmailSender>();

            return builder;
        }

        private static Assembly GetApplicationAssembly(IdentityBuilder builder)
        {
            // Whis is the same logic that MVC follows to find the application assembly.
            var environment = builder.Services.Where(d => d.ServiceType == typeof(IWebHostEnvironment)).ToArray();
            var applicationName = ((IWebHostEnvironment)environment.LastOrDefault()?.ImplementationInstance)
                .ApplicationName;

            var appAssembly = Assembly.Load(applicationName);
            return appAssembly;
        }

        private static bool TryResolveUIFramework(Assembly assembly, out UIFramework uiFramework)
        {
            uiFramework = default;

            var metadata = assembly.GetCustomAttributes<UIFrameworkAttribute>()
                .SingleOrDefault()?.UIFramework; // Bootstrap4 is the default
            if (metadata == null)
            {
                return false;
            }

            // If we find the metadata there must be a valid framework here.
            if (!Enum.TryParse<UIFramework>(metadata, ignoreCase: true, out uiFramework))
            {
                var enumValues = string.Join(", ", Enum.GetNames(typeof(UIFramework)).Select(v => $"'{v}'"));
                throw new InvalidOperationException(
                    $"Found an invalid value for the 'IdentityUIFrameworkVersion'. Valid values are {enumValues}");
            }

            return true;
        }
    }
}
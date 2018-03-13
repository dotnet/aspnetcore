// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Identity.UI.Services;
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
            AddAdditionalApplicationParts(builder);

            builder.Services.ConfigureOptions(
                typeof(IdentityDefaultUIConfigureOptions<>)
                    .MakeGenericType(builder.UserType));
            builder.Services.TryAddTransient<IEmailSender, EmailSender>();

            return builder;
        }

        private static void AddAdditionalApplicationParts(IdentityBuilder builder)
        {
            // For preview1, we don't have a good mechanism to plug in additional parts.
            // We need to provide API surface to allow libraries to plug in existing parts
            // that were optionally added.
            // Challenges here are:
            // * Discovery of the parts.
            // * Ordering of the parts.
            // * Loading of the assembly in memory.
            var thisAssembly = typeof(IdentityBuilderUIExtensions).Assembly;
            var additionalReferences = thisAssembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Where(am => string.Equals(am.Key, "Microsoft.AspNetCore.Mvc.AdditionalReference"))
                .Select(am => am.Value.Split(',')[0])
                .ToArray();

            var mvcBuilder = builder.Services
                .AddMvc()
                .ConfigureApplicationPartManager(apm =>
                {
                    foreach (var reference in additionalReferences)
                    {
                        var fileName = Path.GetFileName(reference);
                        var filePath = Path.Combine(Path.GetDirectoryName(thisAssembly.Location), fileName);
                        var additionalAssembly = LoadAssembly(filePath);
                        // This needs to change to additional assembly part.
                        var additionalPart = new AssemblyPart(additionalAssembly);
                        if (!apm.ApplicationParts.Any(ap => HasSameName(ap.Name, additionalPart.Name)))
                        {
                            apm.ApplicationParts.Add(additionalPart);
                        }
                    }
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
                throw new InvalidOperationException("Could not find the precompiled views assembly for 'Microsoft.AspNetCore.Identity.UI' at " +
                    $"'{filePath}'.");
            }

            return viewsAssembly;
        }
    }
}
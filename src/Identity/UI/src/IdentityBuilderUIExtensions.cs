// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Linq;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Hosting;

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
            builder.Services
                .AddMvc()
                .ConfigureApplicationPartManager(apm =>
                {
                    // We try to resolve the UI framework that was used by looking at the entry assembly.
                    // When an app runs, the entry assembly will point to the built app. In some rare cases
                    // (functional testing) the app assembly will be different, and we'll try to locate it through
                    // the same mechanism that MVC uses today.
                    // Finally, if for some reason we aren't able to find the assembly, we'll use our default value
                    // (Bootstrap5)
                    if (!TryResolveUIFramework(Assembly.GetEntryAssembly(), out var framework) &&
                        !TryResolveUIFramework(GetApplicationAssembly(builder), out framework))
                    {
                        framework = default;
                    }

                    var parts = new ConsolidatedAssemblyApplicationPartFactory().GetApplicationParts(typeof(IdentityBuilderUIExtensions).Assembly);
                    foreach (var part in parts)
                    {
                        apm.ApplicationParts.Add(part);
                    }
                    apm.FeatureProviders.Add(new ViewVersionFeatureProvider(framework == UIFramework.Bootstrap4));
                });

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

            if (applicationName == null)
            {
                return null;
            }
            var appAssembly = Assembly.Load(applicationName);
            return appAssembly;
        }

        private static bool TryResolveUIFramework(Assembly assembly, out UIFramework uiFramework)
        {
            uiFramework = default;

            var metadata = assembly?.GetCustomAttributes<UIFrameworkAttribute>()
                .SingleOrDefault()?.UIFramework; // Bootstrap5 is the default
            if (metadata == null)
            {
                return false;
            }

            // If we find the metadata there must be a valid framework here.
            if (!Enum.TryParse(metadata, ignoreCase: true, out uiFramework))
            {
                var enumValues = string.Join(", ", Enum.GetNames(typeof(UIFramework)).Select(v => $"'{v}'"));
                throw new InvalidOperationException(
                    $"Found an invalid value for the 'IdentityUIFrameworkVersion'. Valid values are {enumValues}");
            }

            return true;
        }

        internal class ViewVersionFeatureProvider : IApplicationFeatureProvider<ViewsFeature>
        {
            private readonly bool _isV4;

            public ViewVersionFeatureProvider(bool isV4) => _isV4 = isV4;

            public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewsFeature feature)
            {
                var viewsToRemove = new List<CompiledViewDescriptor>();
                foreach (var descriptor in feature.ViewDescriptors)
                {
                    if (IsIdentityUIView(descriptor))
                    {
                        if (_isV4) {
                            if (descriptor.Type.FullName.Contains("V5"))
                            {
                                // Remove V5 views
                                viewsToRemove.Add(descriptor);
                            }
                            else
                            {
                                // Fix up paths to eliminate version subdir
                                descriptor.RelativePath = descriptor.RelativePath.Replace("V4/", "");
                            }
                        } else {
                            if (descriptor.Type.FullName.Contains("V4"))
                            {
                                // Remove V4 views
                                viewsToRemove.Add(descriptor);
                            }
                            else
                            {
                                // Fix up paths to eliminate version subdir
                                descriptor.RelativePath = descriptor.RelativePath.Replace("V5/", "");
                            }
                        }
                    }
                }

                foreach (var descriptorToRemove in viewsToRemove)
                {
                    feature.ViewDescriptors.Remove(descriptorToRemove);
                }
            }

            private static bool IsIdentityUIView(CompiledViewDescriptor desc) => desc.RelativePath.StartsWith("/Areas/Identity", StringComparison.OrdinalIgnoreCase) && desc.Type.Assembly == typeof(IdentityBuilderUIExtensions).Assembly;
        }
    }
}

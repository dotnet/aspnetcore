// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.Extensions.CompilationAbstractions;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MvcDnxServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services required for Mvc applications to work with DNX to the specified <paramref name="services"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" />.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddMvcDnx(this IServiceCollection services)
        {
            if (DnxPlatformServices.Default.LibraryManager != null)
            {
                var partManager = GetApplicationPartManager(services);
                var provider = new DnxAssemblyProvider(DnxPlatformServices.Default.LibraryManager);
                foreach (var assembly in provider.CandidateAssemblies)
                {
                    partManager.ApplicationParts.Add(new AssemblyPart(assembly));
                }

                // Add IAssemblyProvider services
                services.AddSingleton(DnxPlatformServices.Default.LibraryManager);

                // Add compilation services
                services.AddSingleton(CompilationServices.Default.LibraryExporter);
                services.AddSingleton<ICompilationService, DnxRoslynCompilationService>();
            }

            return services;
        }

        private static ApplicationPartManager GetApplicationPartManager(IServiceCollection services)
        {
            var manager = GetServiceFromCollection<ApplicationPartManager>(services);
            if (manager == null)
            {
                manager = new ApplicationPartManager();
            }

            return manager;
        }

        private static T GetServiceFromCollection<T>(IServiceCollection services)
        {
            return (T)services
                .FirstOrDefault(d => d.ServiceType == typeof(T))
                ?.ImplementationInstance;
        }
    }
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
                // Add IAssemblyProvider services
                services.AddSingleton(DnxPlatformServices.Default.LibraryManager);
                services.AddTransient<IAssemblyProvider, DnxAssemblyProvider>();

                // Add compilation services
                services.AddSingleton(CompilationServices.Default.LibraryExporter);
                services.AddSingleton<ICompilationService, DnxRoslynCompilationService>();
            }

            return services;
        }
    }
}
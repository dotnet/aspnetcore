// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc
{
    internal class DesignTimeMvcServiceCollectionProvider
    {
        // This method invoked by RazorTooling using reflection.
        public static void PopulateServiceCollection(IServiceCollection services, string assemblyName)
        {
            var assembly = Assembly.Load(new AssemblyName(assemblyName));

            var partManager = new ApplicationPartManager();
            partManager.ApplicationParts.Add(new AssemblyPart(assembly));

            services.AddSingleton(partManager);
            services.AddMvc();
        }
    }
}

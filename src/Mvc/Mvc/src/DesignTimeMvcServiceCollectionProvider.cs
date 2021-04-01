// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    // Discovers assemblies that are part of the MVC application using the DependencyContext.
    public static class DefaultAssemblyPartDiscoveryProvider
    {
        private const string NativeImageSufix = ".ni";

        internal static HashSet<string> ReferenceAssemblies { get; } = new HashSet<string>(StringComparer.Ordinal)
        {
            "Microsoft.AspNetCore.Mvc",
            "Microsoft.AspNetCore.Mvc.Abstractions",
            "Microsoft.AspNetCore.Mvc.ApiExplorer",
            "Microsoft.AspNetCore.Mvc.Core",
            "Microsoft.AspNetCore.Mvc.Cors",
            "Microsoft.AspNetCore.Mvc.DataAnnotations",
            "Microsoft.AspNetCore.Mvc.Formatters.Json",
            "Microsoft.AspNetCore.Mvc.Formatters.Xml",
            "Microsoft.AspNetCore.Mvc.Localization",
            "Microsoft.AspNetCore.Mvc.Razor",
            "Microsoft.AspNetCore.Mvc.Razor.Host",
            "Microsoft.AspNetCore.Mvc.TagHelpers",
            "Microsoft.AspNetCore.Mvc.ViewFeatures"
        };

        public static IEnumerable<ApplicationPart> DiscoverAssemblyParts(string entryPointAssemblyName)
        {
            var entryAssembly = Assembly.Load(new AssemblyName(entryPointAssemblyName));
            var context = DependencyContext.Load(Assembly.Load(new AssemblyName(entryPointAssemblyName)));

            return GetCandidateAssemblies(entryAssembly, context).Select(p => new AssemblyPart(p));
        }

        internal static IEnumerable<Assembly> GetCandidateAssemblies(Assembly entryAssembly, DependencyContext dependencyContext)
        {
            if (dependencyContext == null)
            {
                // Use the entry assembly as the sole candidate.
                return new[] { entryAssembly };
            }

            return GetCandidateLibraries(dependencyContext)
                .SelectMany(library => library.RuntimeAssemblyGroups.GetDefaultGroup().AssetPaths)
                .Select(Load)
                .Where(assembly => assembly != null);
        }

        // Returns a list of libraries that references the assemblies in <see cref="ReferenceAssemblies"/>.
        // By default it returns all assemblies that reference any of the primary MVC assemblies
        // while ignoring MVC assemblies.
        // Internal for unit testing
        internal static IEnumerable<RuntimeLibrary> GetCandidateLibraries(DependencyContext dependencyContext)
        {
            if (ReferenceAssemblies == null)
            {
                return Enumerable.Empty<RuntimeLibrary>();
            }

            return dependencyContext.RuntimeLibraries.Where(IsCandidateLibrary);
        }

        private static Assembly Load(string assetPath)
        {
            var name = Path.GetFileNameWithoutExtension(assetPath);
            if (name != null)
            {
                if (name.EndsWith(NativeImageSufix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - NativeImageSufix.Length);
                }

                return Assembly.Load(new AssemblyName(name));
            }

            return null;
        }

        private static bool IsCandidateLibrary(RuntimeLibrary library)
        {
            Debug.Assert(ReferenceAssemblies != null);
            return library.Dependencies.Any(dependency => ReferenceAssemblies.Contains(dependency.Name));
        }
    }
}

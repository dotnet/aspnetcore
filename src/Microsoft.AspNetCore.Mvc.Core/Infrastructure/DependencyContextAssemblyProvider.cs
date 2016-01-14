// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// An <see cref="IAssemblyProvider"/> that uses <see cref="DependencyContext"/> to discover assemblies that may
    /// contain Mvc specific types such as controllers, and view components.
    /// </summary>
    public class DependencyContextAssemblyProvider : IAssemblyProvider
    {
        private readonly DependencyContext _dependencyContext;

        /// <summary>
        /// Initializes a new instance of <see cref="DependencyContextAssemblyProvider"/>.
        /// </summary>
        /// <param name="environment">The <see cref="IApplicationEnvironment"/>.</param>
        public DependencyContextAssemblyProvider(IApplicationEnvironment environment)
        {
            var applicationAssembly = Assembly.Load(new AssemblyName(environment.ApplicationName));
            _dependencyContext = DependencyContext.Load(applicationAssembly);
        }

        /// <summary>
        /// Gets the set of assembly names that are used as root for discovery of
        /// MVC controllers, view components and views.
        /// </summary>
        // DefaultControllerTypeProvider uses CandidateAssemblies to determine if the base type of a POCO controller
        // lives in an assembly that references MVC. CandidateAssemblies excludes all assemblies from the
        // ReferenceAssemblies set. Consequently adding WebApiCompatShim to this set would cause the ApiController to
        // fail this test.
        protected virtual HashSet<string> ReferenceAssemblies { get; } = new HashSet<string>(StringComparer.Ordinal)
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

        /// <inheritdoc />
        public IEnumerable<Assembly> CandidateAssemblies
        {
            get
            {
                return GetCandidateLibraries()
                    .SelectMany(l => l.Assemblies)
                    .Select(Load);
            }
        }

        /// <summary>
        /// Returns a list of libraries that references the assemblies in <see cref="ReferenceAssemblies"/>.
        /// By default it returns all assemblies that reference any of the primary MVC assemblies
        /// while ignoring MVC assemblies.
        /// </summary>
        /// <returns>A set of <see cref="Library"/>.</returns>
        protected virtual IEnumerable<RuntimeLibrary> GetCandidateLibraries()
        {
            if (ReferenceAssemblies == null)
            {
                return Enumerable.Empty<RuntimeLibrary>();
            }

            return DependencyContext.Default.RuntimeLibraries.Where(IsCandidateLibrary);
        }

        private static Assembly Load(RuntimeAssembly assembly)
        {
            return Assembly.Load(assembly.Name);
        }

        private bool IsCandidateLibrary(RuntimeLibrary library)
        {
            Debug.Assert(ReferenceAssemblies != null);
            return library.Dependencies.Any(dependency => ReferenceAssemblies.Contains(dependency.Name));
        }
    }
}
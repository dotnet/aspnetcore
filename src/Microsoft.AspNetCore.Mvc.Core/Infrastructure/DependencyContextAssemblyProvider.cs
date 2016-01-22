// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.AspNet.Mvc.Infrastructure
{
    public class DependencyContextAssemblyProvider : IAssemblyProvider
    {
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
            "Microsoft.AspNet.Mvc",
            "Microsoft.AspNet.Mvc.Abstractions",
            "Microsoft.AspNet.Mvc.ApiExplorer",
            "Microsoft.AspNet.Mvc.Core",
            "Microsoft.AspNet.Mvc.Cors",
            "Microsoft.AspNet.Mvc.DataAnnotations",
            "Microsoft.AspNet.Mvc.Formatters.Json",
            "Microsoft.AspNet.Mvc.Formatters.Xml",
            "Microsoft.AspNet.Mvc.Localization",
            "Microsoft.AspNet.Mvc.Razor",
            "Microsoft.AspNet.Mvc.Razor.Host",
            "Microsoft.AspNet.Mvc.TagHelpers",
            "Microsoft.AspNet.Mvc.ViewFeatures"
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
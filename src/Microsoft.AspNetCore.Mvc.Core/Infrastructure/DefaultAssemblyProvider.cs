// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// An <see cref="IAssemblyProvider"/> that uses <see cref="DependencyContext"/> to discover assemblies that may
    /// contain Mvc specific types such as controllers, and view components.
    /// </summary>
    public class DefaultAssemblyProvider : IAssemblyProvider
    {
        private const string NativeImageSufix = ".ni";
        private readonly Assembly _entryAssembly;
        private readonly DependencyContext _dependencyContext;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultAssemblyProvider"/>.
        /// </summary>
        /// <param name="environment">The <see cref="IHostingEnvironment"/>.</param>
        public DefaultAssemblyProvider(IHostingEnvironment environment)
            : this(
                  Assembly.Load(new AssemblyName(environment.ApplicationName)),
                  DependencyContext.Load(Assembly.Load(new AssemblyName(environment.ApplicationName))))
        {
        }

        // Internal for unit testing.
        internal DefaultAssemblyProvider(Assembly entryAssembly, DependencyContext dependencyContext)
        {
            _entryAssembly = entryAssembly;
            _dependencyContext = dependencyContext;
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
                if (_dependencyContext == null)
                {
                    // Use the entry assembly as the sole candidate.
                    return new[] { _entryAssembly };
                }

                return GetCandidateLibraries()
                    .SelectMany(library => library.RuntimeAssemblyGroups.GetDefaultGroup().AssetPaths)
                    .Select(Load)
                    .Where(assembly => assembly != null);
            }
        }

        /// <summary>
        /// Returns a list of libraries that references the assemblies in <see cref="ReferenceAssemblies"/>.
        /// By default it returns all assemblies that reference any of the primary MVC assemblies
        /// while ignoring MVC assemblies.
        /// </summary>
        /// <returns>A set of <see cref="Library"/>.</returns>
        // Internal for unit testing
        protected internal virtual IEnumerable<RuntimeLibrary> GetCandidateLibraries()
        {
            if (ReferenceAssemblies == null)
            {
                return Enumerable.Empty<RuntimeLibrary>();
            }

            return _dependencyContext.RuntimeLibraries.Where(IsCandidateLibrary);
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

        private bool IsCandidateLibrary(RuntimeLibrary library)
        {
            Debug.Assert(ReferenceAssemblies != null);
            return library.Dependencies.Any(dependency => ReferenceAssemblies.Contains(dependency.Name));
        }
    }
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
{
    internal class RazorReferenceManager
    {
        private readonly ApplicationPartManager _partManager;
        private readonly MvcRazorRuntimeCompilationOptions _options;
        private object _compilationReferencesLock = new object();
        private bool _compilationReferencesInitialized;
        private IReadOnlyList<MetadataReference> _compilationReferences;
        private IReadOnlyList<IAssemblyPartResolver> _assemblyPartResolvers;

        public RazorReferenceManager(
            ApplicationPartManager partManager,
            IOptions<MvcRazorRuntimeCompilationOptions> options)
        {
            _partManager = partManager;
            _options = options.Value;
            _assemblyPartResolvers = CreateAssemblyPartResolvers();
        }

        public virtual IReadOnlyList<MetadataReference> CompilationReferences
        {
            get
            {
                return LazyInitializer.EnsureInitialized(
                    ref _compilationReferences,
                    ref _compilationReferencesInitialized,
                    ref _compilationReferencesLock,
                    GetCompilationReferences);
            }
        }

        private List<IAssemblyPartResolver> CreateAssemblyPartResolvers()
        {
            var resolvers = new List<IAssemblyPartResolver>();
            resolvers.AddRange(_options.AssemblyPartResolvers);
            resolvers.Add(new CompileOptionsPartResolver(new HashSet<string>(_options.AdditionalReferencePaths,
                StringComparer.OrdinalIgnoreCase)));
            resolvers.Add(new DependencyContextResolver());
            return resolvers;
        }

        private IReadOnlyList<MetadataReference> GetCompilationReferences()
        {
            var referencePaths = GetReferencePaths();

            return referencePaths
                .Select(CreateMetadataReference)
                .ToList();
        }

        // For unit testing
        internal IEnumerable<string> GetReferencePaths()
        {
            var referencePaths = new List<string>();

            foreach (var part in _partManager.ApplicationParts)
            {
                if (part is ICompilationReferencesProvider compilationReferenceProvider)
                {
                    referencePaths.AddRange(compilationReferenceProvider.GetReferencePaths());
                }
                else if (part is AssemblyPart assemblyPart)
                {
                    referencePaths.AddRange(GetAssemblyPartReferences(assemblyPart));
                }
            }

            referencePaths.AddRange(_options.AdditionalReferencePaths);

            return referencePaths;
        }

        private IEnumerable<string> GetAssemblyPartReferences(AssemblyPart assemblyPart)
        {
            foreach (var resolver in _assemblyPartResolvers)
            {
                foreach (var referencePath in resolver.GetReferencePaths(assemblyPart))
                {
                    yield return referencePath;
                }

                /* In order to gracefully allow resolution failures of the DependencyContextResolver we need to provide an execution path where the developer
                 * can inject his own resolution ahead of the DependencyContextResolver to prevent compilation exceptions and then abort resolution for a given Assembly part.
                 * That way DependencyContextResolver compilation crashes can be prevented and the feature of runtime compilation stays intact
                 */

                if (resolver.IsFullyResolved(assemblyPart))
                    break;
            }
        }

        private static MetadataReference CreateMetadataReference(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                var moduleMetadata = ModuleMetadata.CreateFromStream(stream, PEStreamOptions.PrefetchMetadata);
                var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);

                return assemblyMetadata.GetReference(filePath: path);
            }
        }
    }
}

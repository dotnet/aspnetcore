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
        private readonly IAssemblyPartResolver _assemblyPartResolver;

        public RazorReferenceManager(
            ApplicationPartManager partManager,
            IOptions<MvcRazorRuntimeCompilationOptions> options)
        {
            _partManager = partManager;
            _options = options.Value;
            // In a later release this could be extensible to add more assembly resolvers through options?
            // For now it would just make sure that compilation of plugins works without breaking the application
            _assemblyPartResolver = new CompositeAssemblyPartResolver(
                new IAssemblyPartResolver[]
                {
                    new CompileOptionsPartResolver(new HashSet<string>(options.Value.AdditionalReferencePaths, StringComparer.OrdinalIgnoreCase)),
                    new DependencyContextResolver(), 
                });
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
                    referencePaths.AddRange(_assemblyPartResolver.GetReferencePaths(assemblyPart));
                }
            }

            referencePaths.AddRange(_options.AdditionalReferencePaths);

            return referencePaths;
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

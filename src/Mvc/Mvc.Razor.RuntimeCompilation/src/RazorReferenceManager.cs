// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

        public RazorReferenceManager(
            ApplicationPartManager partManager,
            IOptions<MvcRazorRuntimeCompilationOptions> options)
        {
            _partManager = partManager;
            _options = options.Value;
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
                    referencePaths.AddRange(assemblyPart.GetReferencePaths());
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

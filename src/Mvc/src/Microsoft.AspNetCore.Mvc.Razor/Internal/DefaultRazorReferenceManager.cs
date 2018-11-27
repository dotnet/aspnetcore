// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class DefaultRazorReferenceManager : RazorReferenceManager
    {
        private readonly ApplicationPartManager _partManager;
        private readonly IList<MetadataReference> _additionalMetadataReferences;
        private object _compilationReferencesLock = new object();
        private bool _compilationReferencesInitialized;
        private IReadOnlyList<MetadataReference> _compilationReferences;

        public DefaultRazorReferenceManager(
            ApplicationPartManager partManager,
            IOptions<RazorViewEngineOptions> optionsAccessor)
        {
            _partManager = partManager;
            _additionalMetadataReferences = optionsAccessor.Value.AdditionalCompilationReferences;
        }

        public override IReadOnlyList<MetadataReference> CompilationReferences
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
            var feature = new MetadataReferenceFeature();
            _partManager.PopulateFeature(feature);
            var applicationReferences = feature.MetadataReferences;

            if (_additionalMetadataReferences.Count == 0)
            {
                return applicationReferences.ToArray();
            }

            var compilationReferences = new List<MetadataReference>(applicationReferences.Count + _additionalMetadataReferences.Count);
            compilationReferences.AddRange(applicationReferences);
            compilationReferences.AddRange(_additionalMetadataReferences);

            return compilationReferences;
        }
    }
}

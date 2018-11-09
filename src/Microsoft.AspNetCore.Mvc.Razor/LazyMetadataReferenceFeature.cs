// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    internal class LazyMetadataReferenceFeature : IMetadataReferenceFeature
    {
#pragma warning disable CS0618 // Type or member is obsolete
        private readonly RazorReferenceManager _referenceManager;
#pragma warning restore CS0618 // Type or member is obsolete

#pragma warning disable CS0618 // Type or member is obsolete
        public LazyMetadataReferenceFeature(RazorReferenceManager referenceManager)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            _referenceManager = referenceManager;
        }

        /// <remarks>
        /// Invoking <see cref="RazorReferenceManager.CompilationReferences"/> ensures that compilation
        /// references are lazily evaluated.
        /// </remarks>
        public IReadOnlyList<MetadataReference> References => _referenceManager.CompilationReferences;

        public RazorEngine Engine { get; set; }
    }
}

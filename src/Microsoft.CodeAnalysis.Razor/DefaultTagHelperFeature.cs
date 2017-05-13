// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Razor
{
    public class DefaultTagHelperFeature : RazorEngineFeatureBase, ITagHelperFeature
    {
        public ITagHelperDescriptorResolver Resolver { get; private set; }

        protected override void OnInitialized()
        {
            Resolver = new InnerResolver(GetRequiredFeature<IMetadataReferenceFeature>());
        }

        private class InnerResolver : ITagHelperDescriptorResolver
        {
            private readonly IMetadataReferenceFeature _referenceFeature;

            public InnerResolver(IMetadataReferenceFeature referenceFeature)
            {
                _referenceFeature = referenceFeature;
            }
            public IEnumerable<TagHelperDescriptor> Resolve(IList<RazorDiagnostic> errors)
            {
                var compilation = CSharpCompilation.Create("__TagHelpers", references: _referenceFeature.References);
                return TagHelpers.GetTagHelpers(compilation);
            }
        }
    }
}

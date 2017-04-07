// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Razor
{
    public class DefaultTagHelperFeature : ITagHelperFeature
    {
        private RazorEngine _engine;
        private IMetadataReferenceFeature _referenceFeature;

        public RazorEngine Engine
        {
            get
            {
                return _engine;
            }
            set
            {
                _engine = value;
                OnInitialized();
            }
        }

        public ITagHelperDescriptorResolver Resolver { get; private set; }

        private void OnInitialized()
        {
            _referenceFeature = Engine.Features.OfType<IMetadataReferenceFeature>().FirstOrDefault();
            Resolver = new InnerResolver(_referenceFeature);
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

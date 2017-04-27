// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class DefaultTagHelperResolver : TagHelperResolver
    {
        public DefaultTagHelperResolver(bool designTime)
        {
            DesignTime = designTime;
        }

        public bool DesignTime { get; }

        public override TagHelperResolutionResult GetTagHelpers(Compilation compilation)
        {
            var descriptors = new List<TagHelperDescriptor>();

            var providers = new ITagHelperDescriptorProvider[]
            {
                new DefaultTagHelperDescriptorProvider() { DesignTime = true, },
                new ViewComponentTagHelperDescriptorProvider(),
            };

            var results = new List<TagHelperDescriptor>();
            var context = TagHelperDescriptorProviderContext.Create(results);
            context.SetCompilation(compilation);

            for (var i = 0; i < providers.Length; i++)
            {
                var provider = providers[i];
                provider.Execute(context);
            }

            var diagnostics = new List<RazorDiagnostic>();
            var resolutionResult = new TagHelperResolutionResult(results, diagnostics);

            return resolutionResult;
        }
    }
}
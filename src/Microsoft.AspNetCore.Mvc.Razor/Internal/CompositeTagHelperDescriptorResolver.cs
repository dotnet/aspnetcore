// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class CompositeTagHelperDescriptorResolver : ITagHelperDescriptorResolver
    {
        private readonly TagHelperDescriptorResolver _tagHelperDescriptorResolver;
        private readonly ViewComponentTagHelperDescriptorResolver _viewComponentTagHelperDescriptorResolver;

        public CompositeTagHelperDescriptorResolver(
            TagHelperDescriptorResolver tagHelperDescriptorResolver,
            ViewComponentTagHelperDescriptorResolver viewComponentTagHelperDescriptorResolver)
        {
            _tagHelperDescriptorResolver = tagHelperDescriptorResolver;
            _viewComponentTagHelperDescriptorResolver = viewComponentTagHelperDescriptorResolver;
        }

        public IEnumerable<TagHelperDescriptor> Resolve(TagHelperDescriptorResolutionContext resolutionContext)
        {
            var descriptors = new List<TagHelperDescriptor>();

            descriptors.AddRange(_tagHelperDescriptorResolver.Resolve(resolutionContext));
            descriptors.AddRange(_viewComponentTagHelperDescriptorResolver.Resolve(resolutionContext));

            return descriptors;
        }
    }
}

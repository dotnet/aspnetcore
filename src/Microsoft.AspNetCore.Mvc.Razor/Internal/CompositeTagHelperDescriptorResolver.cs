// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class CompositeTagHelperDescriptorResolver : ITagHelperDescriptorResolver
    {
        public IList<TagHelperDescriptorResolver> _resolvers;

        public CompositeTagHelperDescriptorResolver(
            TagHelperDescriptorResolver tagHelperDescriptorResolver,
            ViewComponentTagHelperDescriptorResolver viewComponentTagHelperDescriptorResolver)
        {
            _resolvers = new List<TagHelperDescriptorResolver>();
            _resolvers.Add(tagHelperDescriptorResolver);
            _resolvers.Add(viewComponentTagHelperDescriptorResolver);
        }

        public IEnumerable<TagHelperDescriptor> Resolve(TagHelperDescriptorResolutionContext resolutionContext)
        {
            var descriptors = new List<TagHelperDescriptor>();

            foreach (var resolver in _resolvers)
            {
                var currentDescriptors = resolver.Resolve(resolutionContext);
                descriptors.AddRange(currentDescriptors);
            }

            return descriptors;
        }
    }
}

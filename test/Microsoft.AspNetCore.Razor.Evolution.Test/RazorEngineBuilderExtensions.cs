// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public static class RazorEngineBuilderExtensions
    {
        public static IRazorEngineBuilder AddTagHelpers(this IRazorEngineBuilder builder, params TagHelperDescriptor[] tagHelpers)
        {
            var resolver = (TestTagHelperDescriptorResolver)builder.Features.OfType<TagHelperFeature>().FirstOrDefault()?.Resolver;
            if (resolver == null)
            {
                resolver = new TestTagHelperDescriptorResolver();
                builder.Features.Add(new TagHelperFeature(resolver));
            }

            resolver.TagHelpers.AddRange(tagHelpers);
            return builder;
        }

        private class TestTagHelperDescriptorResolver : ITagHelperDescriptorResolver
        {
            public List<TagHelperDescriptor> TagHelpers { get; } = new List<TagHelperDescriptor>();

            public IEnumerable<TagHelperDescriptor> Resolve(TagHelperDescriptorResolutionContext resolutionContext)
            {
                return TagHelpers;
            }
        }
    }
}

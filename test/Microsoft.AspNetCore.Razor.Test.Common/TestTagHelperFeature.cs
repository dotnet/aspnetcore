// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Legacy;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class TestTagHelperFeature : ITagHelperFeature
    {
        public TestTagHelperFeature()
        {
            Resolver = new TestTagHelperDescriptorResolver();
        }

        public TestTagHelperFeature(IEnumerable<TagHelperDescriptor> tagHelpers)
        {
            Resolver = new TestTagHelperDescriptorResolver(tagHelpers);
        }

        public RazorEngine Engine { get; set; }

        public List<TagHelperDescriptor> TagHelpers => ((TestTagHelperDescriptorResolver)Resolver).TagHelpers;

        public ITagHelperDescriptorResolver Resolver { get; }
    }
}

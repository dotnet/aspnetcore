// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class TestTagHelperFeature : RazorEngineFeatureBase, ITagHelperFeature
    {
        public TestTagHelperFeature()
        {
            TagHelpers = new List<TagHelperDescriptor>();
        }

        public TestTagHelperFeature(IEnumerable<TagHelperDescriptor> tagHelpers)
        {
            TagHelpers = new List<TagHelperDescriptor>(tagHelpers);
        }

        public List<TagHelperDescriptor> TagHelpers { get; }

        public IReadOnlyList<TagHelperDescriptor> GetDescriptors()
        {
            return TagHelpers.ToArray();
        }
    }
}

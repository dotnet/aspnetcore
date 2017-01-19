// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class TestTagHelperDescriptorResolver : ITagHelperDescriptorResolver
    {
        public TestTagHelperDescriptorResolver()
        {
        }

        public TestTagHelperDescriptorResolver(IEnumerable<TagHelperDescriptor> tagHelpers)
        {
            TagHelpers.AddRange(tagHelpers);
        }

        public List<TagHelperDescriptor> TagHelpers { get; } = new List<TagHelperDescriptor>();

        public IEnumerable<TagHelperDescriptor> Resolve(ErrorSink errorSink)
        {
            return TagHelpers;
        }
    }
}

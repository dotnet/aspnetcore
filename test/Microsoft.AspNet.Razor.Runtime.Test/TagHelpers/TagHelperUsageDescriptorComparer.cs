// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperUsageDescriptorComparer : IEqualityComparer<TagHelperUsageDescriptor>
    {
        public static readonly TagHelperUsageDescriptorComparer Default = new TagHelperUsageDescriptorComparer();

        private TagHelperUsageDescriptorComparer()
        {
        }

        public bool Equals(TagHelperUsageDescriptor descriptorX, TagHelperUsageDescriptor descriptorY)
        {
            if (descriptorX == descriptorY)
            {
                return true;
            }

            return descriptorX != null &&
                descriptorY != null &&
                string.Equals(descriptorX.Summary, descriptorY.Summary, StringComparison.Ordinal) &&
                string.Equals(descriptorX.Remarks, descriptorY.Remarks, StringComparison.Ordinal);
        }

        public int GetHashCode(TagHelperUsageDescriptor descriptor)
        {
            return HashCodeCombiner
                .Start()
                .Add(descriptor.Summary, StringComparer.Ordinal)
                .Add(descriptor.Remarks, StringComparer.Ordinal)
                .CombinedHash;
        }
    }
}

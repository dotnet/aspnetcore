// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Test.Internal
{
    internal class TagHelperAttributeDesignTimeDescriptorComparer :
        IEqualityComparer<TagHelperAttributeDesignTimeDescriptor>
    {
        public static readonly TagHelperAttributeDesignTimeDescriptorComparer Default =
            new TagHelperAttributeDesignTimeDescriptorComparer();

        private TagHelperAttributeDesignTimeDescriptorComparer()
        {
        }

        public bool Equals(
            TagHelperAttributeDesignTimeDescriptor descriptorX,
            TagHelperAttributeDesignTimeDescriptor descriptorY)
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

        public int GetHashCode(TagHelperAttributeDesignTimeDescriptor descriptor)
        {
            return HashCodeCombiner
                .Start()
                .Add(descriptor.Summary, StringComparer.Ordinal)
                .Add(descriptor.Remarks, StringComparer.Ordinal)
                .CombinedHash;
        }
    }
}

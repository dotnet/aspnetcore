// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    public class TagHelperDesignTimeDescriptorComparer : IEqualityComparer<TagHelperDesignTimeDescriptor>
    {
        public static readonly TagHelperDesignTimeDescriptorComparer Default =
            new TagHelperDesignTimeDescriptorComparer();

        private TagHelperDesignTimeDescriptorComparer()
        {
        }

        public bool Equals(TagHelperDesignTimeDescriptor descriptorX, TagHelperDesignTimeDescriptor descriptorY)
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

        public int GetHashCode(TagHelperDesignTimeDescriptor descriptor)
        {
            return HashCodeCombiner
                .Start()
                .Add(descriptor.Summary, StringComparer.Ordinal)
                .Add(descriptor.Remarks, StringComparer.Ordinal)
                .CombinedHash;
        }
    }
}

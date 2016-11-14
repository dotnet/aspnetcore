// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
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

            Assert.NotNull(descriptorX);
            Assert.NotNull(descriptorY);
            Assert.Equal(descriptorX.Summary, descriptorY.Summary, StringComparer.Ordinal);
            Assert.Equal(descriptorX.Remarks, descriptorY.Remarks, StringComparer.Ordinal);

            return true;
        }

        public int GetHashCode(TagHelperAttributeDesignTimeDescriptor descriptor)
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(descriptor.Summary, StringComparer.Ordinal);
            hashCodeCombiner.Add(descriptor.Remarks, StringComparer.Ordinal);

            return hashCodeCombiner;
        }
    }
}

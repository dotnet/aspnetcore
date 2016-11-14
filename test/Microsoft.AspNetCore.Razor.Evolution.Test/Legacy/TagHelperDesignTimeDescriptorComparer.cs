// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class TagHelperDesignTimeDescriptorComparer : IEqualityComparer<TagHelperDesignTimeDescriptor>
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

            Assert.NotNull(descriptorX);
            Assert.NotNull(descriptorY);
            Assert.Equal(descriptorX.Summary, descriptorY.Summary, StringComparer.Ordinal);
            Assert.Equal(descriptorX.Remarks, descriptorY.Remarks, StringComparer.Ordinal);
            Assert.Equal(descriptorX.OutputElementHint, descriptorY.OutputElementHint, StringComparer.Ordinal);

            return true;
        }

        public int GetHashCode(TagHelperDesignTimeDescriptor descriptor)
        {
            var hashCodeCombiner = HashCodeCombiner.Start();

            hashCodeCombiner.Add(descriptor.Summary, StringComparer.Ordinal);
            hashCodeCombiner.Add(descriptor.Remarks, StringComparer.Ordinal);
            hashCodeCombiner.Add(descriptor.OutputElementHint, StringComparer.Ordinal);

            return hashCodeCombiner;
        }
    }
}

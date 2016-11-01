// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Razor.TagHelpers.Testing
{
    internal class TagHelperAttributeDescriptorComparer : IEqualityComparer<TagHelperAttributeDescriptor>
    {
        public static readonly TagHelperAttributeDescriptorComparer Default =
            new TagHelperAttributeDescriptorComparer();

        private TagHelperAttributeDescriptorComparer()
        {
        }

        public bool Equals(TagHelperAttributeDescriptor descriptorX, TagHelperAttributeDescriptor descriptorY)
        {
            if (descriptorX == descriptorY)
            {
                return true;
            }

            Assert.NotNull(descriptorX);
            Assert.NotNull(descriptorY);
            Assert.Equal(descriptorX.IsIndexer, descriptorY.IsIndexer);
            Assert.Equal(descriptorX.Name, descriptorY.Name, StringComparer.Ordinal);
            Assert.Equal(descriptorX.PropertyName, descriptorY.PropertyName, StringComparer.Ordinal);
            Assert.Equal(descriptorX.TypeName, descriptorY.TypeName, StringComparer.Ordinal);
            Assert.Equal(descriptorX.IsEnum, descriptorY.IsEnum);
            Assert.Equal(descriptorX.IsStringProperty, descriptorY.IsStringProperty);

            return TagHelperAttributeDesignTimeDescriptorComparer.Default.Equals(
                    descriptorX.DesignTimeDescriptor,
                    descriptorY.DesignTimeDescriptor);
        }

        public int GetHashCode(TagHelperAttributeDescriptor descriptor)
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(descriptor.IsIndexer);
            hashCodeCombiner.Add(descriptor.Name, StringComparer.Ordinal);
            hashCodeCombiner.Add(descriptor.PropertyName, StringComparer.Ordinal);
            hashCodeCombiner.Add(descriptor.TypeName, StringComparer.Ordinal);
            hashCodeCombiner.Add(descriptor.IsEnum);
            hashCodeCombiner.Add(descriptor.IsStringProperty);
            hashCodeCombiner.Add(TagHelperAttributeDesignTimeDescriptorComparer.Default.GetHashCode(
                descriptor.DesignTimeDescriptor));

            return hashCodeCombiner;
        }
    }
}
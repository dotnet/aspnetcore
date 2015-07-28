// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Test.Internal
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

            return descriptorX != null &&
                descriptorY != null &&
                descriptorX.IsIndexer == descriptorY.IsIndexer &&
                string.Equals(descriptorX.Name, descriptorY.Name, StringComparison.Ordinal) &&
                string.Equals(descriptorX.PropertyName, descriptorY.PropertyName, StringComparison.Ordinal) &&
                string.Equals(descriptorX.TypeName, descriptorY.TypeName, StringComparison.Ordinal) &&
                descriptorX.IsStringProperty == descriptorY.IsStringProperty &&
                TagHelperAttributeDesignTimeDescriptorComparer.Default.Equals(
                    descriptorX.DesignTimeDescriptor,
                    descriptorY.DesignTimeDescriptor);
        }

        public int GetHashCode(TagHelperAttributeDescriptor descriptor)
        {
            return HashCodeCombiner.Start()
                .Add(descriptor.IsIndexer)
                .Add(descriptor.Name, StringComparer.Ordinal)
                .Add(descriptor.PropertyName, StringComparer.Ordinal)
                .Add(descriptor.TypeName, StringComparer.Ordinal)
                .Add(descriptor.IsStringProperty)
                .Add(TagHelperAttributeDesignTimeDescriptorComparer.Default.GetHashCode(
                    descriptor.DesignTimeDescriptor))
                .CombinedHash;
        }
    }
}
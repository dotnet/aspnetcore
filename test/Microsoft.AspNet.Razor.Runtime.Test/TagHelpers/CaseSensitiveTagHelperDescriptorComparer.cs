// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class CaseSensitiveTagHelperDescriptorComparer : TagHelperDescriptorComparer
    {
        public new static readonly CaseSensitiveTagHelperDescriptorComparer Default =
            new CaseSensitiveTagHelperDescriptorComparer();

        private CaseSensitiveTagHelperDescriptorComparer()
            : base()
        {
        }

        public override bool Equals(TagHelperDescriptor descriptorX, TagHelperDescriptor descriptorY)
        {
            if (descriptorX == descriptorY)
            {
                return true;
            }

            return base.Equals(descriptorX, descriptorY) &&
                // Normal comparer doesn't care about the case, required attribute order, attributes or prefixes.
                // In tests we do.
                string.Equals(descriptorX.TagName, descriptorY.TagName, StringComparison.Ordinal) &&
                string.Equals(descriptorX.Prefix, descriptorY.Prefix, StringComparison.Ordinal) &&
                Enumerable.SequenceEqual(
                    descriptorX.RequiredAttributes,
                    descriptorY.RequiredAttributes,
                    StringComparer.Ordinal) &&
                descriptorX.Attributes.SequenceEqual(
                    descriptorY.Attributes,
                    TagHelperAttributeDescriptorComparer.Default) &&
                TagHelperDesignTimeDescriptorComparer.Default.Equals(
                    descriptorX.DesignTimeDescriptor,
                    descriptorY.DesignTimeDescriptor);
        }

        public override int GetHashCode(TagHelperDescriptor descriptor)
        {
            var hashCodeCombiner = HashCodeCombiner.Start()
                .Add(base.GetHashCode(descriptor))
                .Add(descriptor.TagName, StringComparer.Ordinal)
                .Add(descriptor.Prefix, StringComparer.Ordinal);

            if (descriptor.DesignTimeDescriptor != null)
            {
                hashCodeCombiner.Add(
                    TagHelperDesignTimeDescriptorComparer.Default.GetHashCode(descriptor.DesignTimeDescriptor));
            }

            foreach (var requiredAttribute in descriptor.RequiredAttributes)
            {
                hashCodeCombiner.Add(requiredAttribute, StringComparer.Ordinal);
            }

            foreach (var attribute in descriptor.Attributes)
            {
                hashCodeCombiner.Add(TagHelperAttributeDescriptorComparer.Default.GetHashCode(attribute));
            }

            return hashCodeCombiner.CombinedHash;
        }
    }
}
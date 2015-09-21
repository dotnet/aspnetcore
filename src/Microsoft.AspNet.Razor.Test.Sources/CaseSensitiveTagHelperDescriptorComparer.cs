// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Test.Internal
{
    internal class CaseSensitiveTagHelperDescriptorComparer : TagHelperDescriptorComparer
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
                // Normal comparer doesn't care about the case, required attribute order, allowed children order,
                // attributes or prefixes. In tests we do.
                string.Equals(descriptorX.TagName, descriptorY.TagName, StringComparison.Ordinal) &&
                string.Equals(descriptorX.Prefix, descriptorY.Prefix, StringComparison.Ordinal) &&
                Enumerable.SequenceEqual(
                    descriptorX.RequiredAttributes,
                    descriptorY.RequiredAttributes,
                    StringComparer.Ordinal) &&
                (descriptorX.AllowedChildren == descriptorY.AllowedChildren ||
                Enumerable.SequenceEqual(
                    descriptorX.AllowedChildren,
                    descriptorY.AllowedChildren,
                    StringComparer.Ordinal)) &&
                descriptorX.Attributes.SequenceEqual(
                    descriptorY.Attributes,
                    TagHelperAttributeDescriptorComparer.Default) &&
                TagHelperDesignTimeDescriptorComparer.Default.Equals(
                    descriptorX.DesignTimeDescriptor,
                    descriptorY.DesignTimeDescriptor);
        }

        public override int GetHashCode(TagHelperDescriptor descriptor)
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(base.GetHashCode(descriptor));
            hashCodeCombiner.Add(descriptor.TagName, StringComparer.Ordinal);
            hashCodeCombiner.Add(descriptor.Prefix, StringComparer.Ordinal);

            if (descriptor.DesignTimeDescriptor != null)
            {
                hashCodeCombiner.Add(
                    TagHelperDesignTimeDescriptorComparer.Default.GetHashCode(descriptor.DesignTimeDescriptor));
            }

            foreach (var requiredAttribute in descriptor.RequiredAttributes.OrderBy(attribute => attribute))
            {
                hashCodeCombiner.Add(requiredAttribute, StringComparer.Ordinal);
            }

            if (descriptor.AllowedChildren != null)
            {
                foreach (var child in descriptor.AllowedChildren.OrderBy(child => child))
                {
                    hashCodeCombiner.Add(child, StringComparer.Ordinal);
                }
            }

            var orderedAttributeHashCodes = descriptor.Attributes
                .Select(attribute => TagHelperAttributeDescriptorComparer.Default.GetHashCode(attribute))
                .OrderBy(hashcode => hashcode);
            foreach (var attributeHashCode in orderedAttributeHashCodes)
            {
                hashCodeCombiner.Add(attributeHashCode);
            }

            return hashCodeCombiner.CombinedHash;
        }
    }
}
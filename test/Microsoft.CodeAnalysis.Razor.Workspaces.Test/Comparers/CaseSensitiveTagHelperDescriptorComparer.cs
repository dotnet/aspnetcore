// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Test.Comparers
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

            Assert.True(base.Equals(descriptorX, descriptorY));

            // Normal comparer doesn't care about the case, required attribute order, allowed children order,
            // attributes or prefixes. In tests we do.
            Assert.Equal(descriptorX.TagName, descriptorY.TagName, StringComparer.Ordinal);
            Assert.Equal(descriptorX.Prefix, descriptorY.Prefix, StringComparer.Ordinal);
            Assert.Equal(
                descriptorX.RequiredAttributes,
                descriptorY.RequiredAttributes,
                CaseSensitiveTagHelperRequiredAttributeDescriptorComparer.Default);
            Assert.Equal(descriptorX.RequiredParent, descriptorY.RequiredParent, StringComparer.Ordinal);

            if (descriptorX.AllowedChildren != descriptorY.AllowedChildren)
            {
                Assert.Equal(descriptorX.AllowedChildren, descriptorY.AllowedChildren, StringComparer.Ordinal);
            }

            Assert.Equal(
                descriptorX.Attributes,
                descriptorY.Attributes,
                TagHelperAttributeDescriptorComparer.Default);
            Assert.Equal(
                descriptorX.DesignTimeDescriptor,
                descriptorY.DesignTimeDescriptor,
                TagHelperDesignTimeDescriptorComparer.Default);

            return true;
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

            foreach (var requiredAttribute in descriptor.RequiredAttributes.OrderBy(attribute => attribute.Name))
            {
                hashCodeCombiner.Add(
                    CaseSensitiveTagHelperRequiredAttributeDescriptorComparer.Default.GetHashCode(requiredAttribute));
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
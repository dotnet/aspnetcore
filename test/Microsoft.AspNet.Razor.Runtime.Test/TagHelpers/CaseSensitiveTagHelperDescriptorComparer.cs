// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class CaseSensitiveTagHelperDescriptorComparer : TagHelperDescriptorComparer, IEqualityComparer<TagHelperDescriptor>
    {
        public new static readonly CaseSensitiveTagHelperDescriptorComparer Default =
            new CaseSensitiveTagHelperDescriptorComparer();

        private CaseSensitiveTagHelperDescriptorComparer()
        {
        }

        bool IEqualityComparer<TagHelperDescriptor>.Equals(
            TagHelperDescriptor descriptorX, 
            TagHelperDescriptor descriptorY)
        {
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
                    CaseSensitiveAttributeDescriptorComparer.Default);
        }

        int IEqualityComparer<TagHelperDescriptor>.GetHashCode(TagHelperDescriptor descriptor)
        {
            var hashCodeCombiner = HashCodeCombiner
                .Start()
                .Add(base.GetHashCode())
                .Add(descriptor.TagName, StringComparer.Ordinal)
                .Add(descriptor.Prefix);

            foreach (var requiredAttribute in descriptor.RequiredAttributes)
            {
                hashCodeCombiner.Add(requiredAttribute, StringComparer.Ordinal);
            }

            foreach (var attribute in descriptor.Attributes)
            {
                hashCodeCombiner.Add(CaseSensitiveAttributeDescriptorComparer.Default.GetHashCode(attribute));
            }

            return hashCodeCombiner.CombinedHash;
        }

        private class CaseSensitiveAttributeDescriptorComparer : IEqualityComparer<TagHelperAttributeDescriptor>
        {
            public static readonly CaseSensitiveAttributeDescriptorComparer Default =
                new CaseSensitiveAttributeDescriptorComparer();

            private CaseSensitiveAttributeDescriptorComparer()
            {
            }

            public bool Equals(TagHelperAttributeDescriptor descriptorX, TagHelperAttributeDescriptor descriptorY)
            {
                return
                    // Normal comparer doesn't care about case, in tests we do.
                    string.Equals(descriptorX.Name, descriptorY.Name, StringComparison.Ordinal) &&
                    string.Equals(descriptorX.PropertyName, descriptorY.PropertyName, StringComparison.Ordinal) &&
                    string.Equals(descriptorX.TypeName, descriptorY.TypeName, StringComparison.Ordinal);
            }

            public int GetHashCode(TagHelperAttributeDescriptor descriptor)
            {
                return HashCodeCombiner
                    .Start()
                    .Add(descriptor.Name, StringComparer.Ordinal)
                    .Add(descriptor.PropertyName, StringComparer.Ordinal)
                    .Add(descriptor.TypeName, StringComparer.Ordinal)
                    .CombinedHash;
            }
        }
    }
}
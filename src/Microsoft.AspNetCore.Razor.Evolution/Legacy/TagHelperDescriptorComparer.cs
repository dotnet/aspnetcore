// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class TagHelperDescriptorComparer : IEqualityComparer<TagHelperDescriptor>
    {
        /// <summary>
        /// A default instance of the <see cref="TagHelperDescriptorComparer"/>.
        /// </summary>
        public static readonly TagHelperDescriptorComparer Default = new TagHelperDescriptorComparer();

        /// <summary>
        /// A default instance of the <see cref="TagHelperDescriptorComparer"/> that does case-sensitive comparison.
        /// </summary>
        internal static readonly TagHelperDescriptorComparer CaseSensitive =
            new TagHelperDescriptorComparer(caseSensitive: true);

        private readonly StringComparer _stringComparer;
        private readonly StringComparison _stringComparison;
        private readonly BoundAttributeDescriptorComparer _boundAttributeComparer;
        private readonly TagMatchingRuleComparer _tagMatchingRuleComparer;

        private TagHelperDescriptorComparer(bool caseSensitive = false)
        {
            if (caseSensitive)
            {
                _stringComparer = StringComparer.Ordinal;
                _stringComparison = StringComparison.Ordinal;
                _boundAttributeComparer = BoundAttributeDescriptorComparer.CaseSensitive;
                _tagMatchingRuleComparer = TagMatchingRuleComparer.CaseSensitive;
            }
            else
            {
                _stringComparer = StringComparer.OrdinalIgnoreCase;
                _stringComparison = StringComparison.OrdinalIgnoreCase;
                _boundAttributeComparer = BoundAttributeDescriptorComparer.Default;
                _tagMatchingRuleComparer = TagMatchingRuleComparer.Default;
            }
        }

        public virtual bool Equals(TagHelperDescriptor descriptorX, TagHelperDescriptor descriptorY)
        {
            if (descriptorX == descriptorY)
            {
                return true;
            }

            return descriptorX != null &&
                string.Equals(descriptorX.Kind, descriptorY.Kind, StringComparison.Ordinal) &&
                string.Equals(descriptorX.AssemblyName, descriptorY.AssemblyName, StringComparison.Ordinal) &&
                Enumerable.SequenceEqual(
                    descriptorX.BoundAttributes.OrderBy(attribute => attribute.Name, _stringComparer),
                    descriptorY.BoundAttributes.OrderBy(attribute => attribute.Name, _stringComparer),
                    _boundAttributeComparer) &&
                Enumerable.SequenceEqual(
                    descriptorX.TagMatchingRules.OrderBy(rule => rule.TagName, _stringComparer),
                    descriptorY.TagMatchingRules.OrderBy(rule => rule.TagName, _stringComparer),
                    _tagMatchingRuleComparer) &&
                (descriptorX.AllowedChildTags == descriptorY.AllowedChildTags ||
                (descriptorX.AllowedChildTags != null &&
                descriptorY.AllowedChildTags != null &&
                Enumerable.SequenceEqual(
                    descriptorX.AllowedChildTags.OrderBy(child => child, _stringComparer),
                    descriptorY.AllowedChildTags.OrderBy(child => child, _stringComparer),
                    _stringComparer))) &&
                string.Equals(descriptorX.Documentation, descriptorY.Documentation, StringComparison.Ordinal) &&
                string.Equals(descriptorX.DisplayName, descriptorY.DisplayName, StringComparison.Ordinal) &&
                string.Equals(descriptorX.TagOutputHint, descriptorY.TagOutputHint, _stringComparison) &&
                Enumerable.SequenceEqual(descriptorX.Diagnostics, descriptorY.Diagnostics) &&
                Enumerable.SequenceEqual(
                    descriptorX.Metadata.OrderBy(metadataX => metadataX.Key, StringComparer.Ordinal),
                    descriptorY.Metadata.OrderBy(metadataY => metadataY.Key, StringComparer.Ordinal));
        }

        /// <inheritdoc />
        public virtual int GetHashCode(TagHelperDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(descriptor.Kind);
            hashCodeCombiner.Add(descriptor.AssemblyName, StringComparer.Ordinal);

            var boundAttributes = descriptor.BoundAttributes.OrderBy(attribute => attribute.Name, _stringComparer);
            foreach (var attribute in boundAttributes)
            {
                hashCodeCombiner.Add(_boundAttributeComparer.GetHashCode(attribute));
            }

            var rules = descriptor.TagMatchingRules.OrderBy(rule => rule.TagName, _stringComparer);
            foreach (var rule in rules)
            {
                hashCodeCombiner.Add(_tagMatchingRuleComparer.GetHashCode(rule));
            }

            hashCodeCombiner.Add(descriptor.Documentation, StringComparer.Ordinal);
            hashCodeCombiner.Add(descriptor.DisplayName, StringComparer.Ordinal);
            hashCodeCombiner.Add(descriptor.TagOutputHint, _stringComparer);

            if (descriptor.AllowedChildTags != null)
            {
                var allowedChildren = descriptor.AllowedChildTags.OrderBy(child => child, _stringComparer);
                foreach (var child in allowedChildren)
                {
                    hashCodeCombiner.Add(child, _stringComparer);
                }
            }

            return hashCodeCombiner.CombinedHash;
        }
    }
}
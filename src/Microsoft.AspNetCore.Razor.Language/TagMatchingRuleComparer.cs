// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class TagMatchingRuleComparer : IEqualityComparer<TagMatchingRule>
    {
        /// <summary>
        /// A default instance of the <see cref="TagMatchingRuleComparer"/>.
        /// </summary>
        public static readonly TagMatchingRuleComparer Default = new TagMatchingRuleComparer();

        /// <summary>
        /// A default instance of the <see cref="TagMatchingRuleComparer"/> that does case-sensitive comparison.
        /// </summary>
        internal static readonly TagMatchingRuleComparer CaseSensitive =
            new TagMatchingRuleComparer(caseSensitive: true);

        private readonly StringComparer _stringComparer;
        private readonly StringComparison _stringComparison;
        private readonly RequiredAttributeDescriptorComparer _requiredAttributeComparer;

        private TagMatchingRuleComparer(bool caseSensitive = false)
        {
            if (caseSensitive)
            {
                _stringComparer = StringComparer.Ordinal;
                _stringComparison = StringComparison.Ordinal;
                _requiredAttributeComparer = RequiredAttributeDescriptorComparer.CaseSensitive;
            }
            else
            {
                _stringComparer = StringComparer.OrdinalIgnoreCase;
                _stringComparison = StringComparison.OrdinalIgnoreCase;
                _requiredAttributeComparer = RequiredAttributeDescriptorComparer.Default;
            }
        }

        public virtual bool Equals(TagMatchingRule ruleX, TagMatchingRule ruleY)
        {
            if (object.ReferenceEquals(ruleX, ruleY))
            {
                return true;
            }

            if (ruleX == null ^ ruleY == null)
            {
                return false;
            }

            return ruleX != null &&
                string.Equals(ruleX.TagName, ruleY.TagName, _stringComparison) &&
                string.Equals(ruleX.ParentTag, ruleY.ParentTag, _stringComparison) &&
                ruleX.TagStructure == ruleY.TagStructure &&
                Enumerable.SequenceEqual(ruleX.Attributes, ruleY.Attributes, _requiredAttributeComparer) &&
                Enumerable.SequenceEqual(ruleX.Diagnostics, ruleY.Diagnostics);
        }

        public virtual int GetHashCode(TagMatchingRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(rule.TagName, _stringComparer);
            hashCodeCombiner.Add(rule.ParentTag, _stringComparer);
            hashCodeCombiner.Add(rule.TagStructure);

            var attributes = rule.Attributes.OrderBy(attribute => attribute.Name, _stringComparer);
            foreach (var attribute in attributes)
            {
                hashCodeCombiner.Add(_requiredAttributeComparer.GetHashCode(attribute));
            }

            return hashCodeCombiner.CombinedHash;
        }
    }
}
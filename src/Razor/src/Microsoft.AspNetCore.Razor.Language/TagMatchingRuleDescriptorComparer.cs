// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class TagMatchingRuleDescriptorComparer : IEqualityComparer<TagMatchingRuleDescriptor>
    {
        /// <summary>
        /// A default instance of the <see cref="TagMatchingRuleDescriptorComparer"/>.
        /// </summary>
        public static readonly TagMatchingRuleDescriptorComparer Default = new TagMatchingRuleDescriptorComparer();

        /// <summary>
        /// A default instance of the <see cref="TagMatchingRuleDescriptorComparer"/> that does case-sensitive comparison.
        /// </summary>
        internal static readonly TagMatchingRuleDescriptorComparer CaseSensitive =
            new TagMatchingRuleDescriptorComparer(caseSensitive: true);

        private readonly StringComparer _stringComparer;
        private readonly StringComparison _stringComparison;
        private readonly RequiredAttributeDescriptorComparer _requiredAttributeComparer;

        private TagMatchingRuleDescriptorComparer(bool caseSensitive = false)
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

        public virtual bool Equals(TagMatchingRuleDescriptor ruleX, TagMatchingRuleDescriptor ruleY)
        {
            if (object.ReferenceEquals(ruleX, ruleY))
            {
                return true;
            }

            if (ruleX == null ^ ruleY == null)
            {
                return false;
            }

            return
                string.Equals(ruleX.TagName, ruleY.TagName, _stringComparison) &&
                string.Equals(ruleX.ParentTag, ruleY.ParentTag, _stringComparison) &&
                ruleX.TagStructure == ruleY.TagStructure &&
                Enumerable.SequenceEqual(ruleX.Attributes, ruleY.Attributes, _requiredAttributeComparer);
        }

        public virtual int GetHashCode(TagMatchingRuleDescriptor rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            var hash = HashCodeCombiner.Start();
            hash.Add(rule.TagName, _stringComparer);

            return hash.CombinedHash;
        }
    }
}
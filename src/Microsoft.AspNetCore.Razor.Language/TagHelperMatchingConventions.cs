// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal static class TagHelperMatchingConventions
    {
        public const string ElementCatchAllName = "*";

        public const char ElementOptOutCharacter = '!';

        public static bool SatisfiesRule(
            string tagNameWithoutPrefix,
            string parentTagName,
            IEnumerable<KeyValuePair<string, string>> tagAttributes,
            TagMatchingRuleDescriptor rule)
        {
            if (tagNameWithoutPrefix == null)
            {
                throw new ArgumentNullException(nameof(tagNameWithoutPrefix));
            }

            if (tagAttributes == null)
            {
                throw new ArgumentNullException(nameof(tagAttributes));
            }

            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            var satisfiesTagName = SatisfiesTagName(tagNameWithoutPrefix, rule);
            if (!satisfiesTagName)
            {
                return false;
            }

            var satisfiesParentTag = SatisfiesParentTag(parentTagName, rule);
            if (!satisfiesParentTag)
            {
                return false;
            }

            var satisfiesAttributes = SatisfiesAttributes(tagAttributes, rule);
            if (!satisfiesAttributes)
            {
                return false;
            }

            return true;
        }

        public static bool SatisfiesTagName(string tagNameWithoutPrefix, TagMatchingRuleDescriptor rule)
        {
            if (tagNameWithoutPrefix == null)
            {
                throw new ArgumentNullException(nameof(tagNameWithoutPrefix));
            }

            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            if (string.IsNullOrEmpty(tagNameWithoutPrefix))
            {
                return false;
            }

            if (tagNameWithoutPrefix[0] == ElementOptOutCharacter)
            {
                // TagHelpers can never satisfy tag names that are prefixed with the opt-out character.
                return false;
            }

            if (rule.TagName != ElementCatchAllName &&
                rule.TagName != null &&
                !string.Equals(tagNameWithoutPrefix, rule.TagName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        public static bool SatisfiesParentTag(string parentTagName, TagMatchingRuleDescriptor rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            if (rule.ParentTag != null && !string.Equals(parentTagName, rule.ParentTag, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        public static bool SatisfiesAttributes(IEnumerable<KeyValuePair<string, string>> tagAttributes, TagMatchingRuleDescriptor rule)
        {
            if (tagAttributes == null)
            {
                throw new ArgumentNullException(nameof(tagAttributes));
            }

            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            if (!rule.Attributes.All(
                requiredAttribute => tagAttributes.Any(
                    attribute => SatisfiesRequiredAttribute(attribute.Key, attribute.Value, requiredAttribute))))
            {
                return false;
            }

            return true;
        }

        public static bool CanSatisfyBoundAttribute(string name, BoundAttributeDescriptor descriptor)
        {
            return SatisfiesBoundAttributeName(name, descriptor) || SatisfiesBoundAttributeIndexer(name, descriptor);
        }

        public static bool SatisfiesBoundAttributeIndexer(string name, BoundAttributeDescriptor descriptor)
        {
            return descriptor.IndexerNamePrefix != null &&
                !SatisfiesBoundAttributeName(name, descriptor) &&
                name.StartsWith(descriptor.IndexerNamePrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static bool SatisfiesBoundAttributeName(string name, BoundAttributeDescriptor descriptor)
        {
            return string.Equals(descriptor.Name, name, StringComparison.OrdinalIgnoreCase);
        }

        // Internal for testing
        internal static bool SatisfiesRequiredAttribute(string attributeName, string attributeValue, RequiredAttributeDescriptor descriptor)
        {
            var nameMatches = false;
            if (descriptor.NameComparison == RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
            {
                nameMatches = string.Equals(descriptor.Name, attributeName, StringComparison.OrdinalIgnoreCase);
            }
            else if (descriptor.NameComparison == RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch)
            {
                // attributeName cannot equal the Name if comparing as a PrefixMatch.
                nameMatches = attributeName.Length != descriptor.Name.Length &&
                    attributeName.StartsWith(descriptor.Name, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                Debug.Assert(false, "Unknown name comparison.");
            }

            if (!nameMatches)
            {
                return false;
            }

            switch (descriptor.ValueComparison)
            {
                case RequiredAttributeDescriptor.ValueComparisonMode.None:
                    return true;
                case RequiredAttributeDescriptor.ValueComparisonMode.PrefixMatch: // Value starts with
                    return attributeValue.StartsWith(descriptor.Value, StringComparison.Ordinal);
                case RequiredAttributeDescriptor.ValueComparisonMode.SuffixMatch: // Value ends with
                    return attributeValue.EndsWith(descriptor.Value, StringComparison.Ordinal);
                case RequiredAttributeDescriptor.ValueComparisonMode.FullMatch: // Value equals
                    return string.Equals(attributeValue, descriptor.Value, StringComparison.Ordinal);
                default:
                    Debug.Assert(false, "Unknown value comparison.");
                    return false;
            }
        }
    }
}

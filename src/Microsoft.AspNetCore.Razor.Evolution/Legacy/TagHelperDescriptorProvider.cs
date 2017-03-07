// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    /// <summary>
    /// Enables retrieval of <see cref="TagHelperDescriptor"/>'s.
    /// </summary>
    internal class TagHelperDescriptorProvider
    {
        public const string ElementCatchAllTarget = "*";

        private IDictionary<string, HashSet<TagHelperDescriptor>> _registrations;
        private readonly string _tagHelperPrefix;

        /// <summary>
        /// Instantiates a new instance of the <see cref="TagHelperDescriptorProvider"/>.
        /// </summary>
        /// <param name="tagHelperPrefix">The tag helper prefix being used by the document.</param>
        /// <param name="descriptors">The descriptors that the <see cref="TagHelperDescriptorProvider"/> will pull from.</param>
        public TagHelperDescriptorProvider(string tagHelperPrefix, IEnumerable<TagHelperDescriptor> descriptors)
        {
            _tagHelperPrefix = tagHelperPrefix;
            _registrations = new Dictionary<string, HashSet<TagHelperDescriptor>>(StringComparer.OrdinalIgnoreCase);

            // Populate our registrations
            foreach (var descriptor in descriptors)
            {
                Register(descriptor);
            }
        }

        /// <summary>
        /// Gets all tag helpers that match the given <paramref name="tagName"/>.
        /// </summary>
        /// <param name="tagName">The name of the HTML tag to match. Providing a '*' tag name
        /// retrieves catch-all <see cref="TagHelperDescriptor"/>s (descriptors that target every tag).</param>
        /// <param name="attributes">Attributes the HTML element must contain to match.</param>
        /// <param name="parentTagName">The parent tag name of the given <paramref name="tagName"/> tag.</param>
        /// <returns><see cref="TagHelperDescriptor"/>s that apply to the given <paramref name="tagName"/>.
        /// Will return an empty <see cref="Enumerable" /> if no <see cref="TagHelperDescriptor"/>s are
        /// found.</returns>
        public TagHelperBinding GetTagHelperBinding(
            string tagName,
            IEnumerable<KeyValuePair<string, string>> attributes,
            string parentTagName)
        {
            if (!string.IsNullOrEmpty(_tagHelperPrefix) &&
                (tagName.Length <= _tagHelperPrefix.Length ||
                !tagName.StartsWith(_tagHelperPrefix, StringComparison.OrdinalIgnoreCase)))
            {
                // The tagName doesn't have the tag helper prefix, we can short circuit.
                return null;
            }

            IEnumerable<TagHelperDescriptor> descriptors;

            // Ensure there's a HashSet to use.
            if (!_registrations.TryGetValue(ElementCatchAllTarget, out HashSet<TagHelperDescriptor> catchAllDescriptors))
            {
                descriptors = new HashSet<TagHelperDescriptor>(TagHelperDescriptorComparer.Default);
            }
            else
            {
                descriptors = catchAllDescriptors;
            }

            // If we have a tag name associated with the requested name, we need to combine matchingDescriptors
            // with all the catch-all descriptors.
            if (_registrations.TryGetValue(tagName, out HashSet<TagHelperDescriptor> matchingDescriptors))
            {
                descriptors = matchingDescriptors.Concat(descriptors);
            }

            var tagNameWithoutPrefix = _tagHelperPrefix != null ? tagName.Substring(_tagHelperPrefix.Length) : tagName;
            Dictionary<TagHelperDescriptor, IEnumerable<TagMatchingRule>> applicableDescriptorMappings = null;
            foreach (var descriptor in descriptors)
            {
                var applicableRules = descriptor.TagMatchingRules.Where(
                    rule => MatchesRule(rule, attributes, tagNameWithoutPrefix, parentTagName));

                if (applicableRules.Any())
                {
                    if (applicableDescriptorMappings == null)
                    {
                        applicableDescriptorMappings = new Dictionary<TagHelperDescriptor, IEnumerable<TagMatchingRule>>();
                    }

                    applicableDescriptorMappings[descriptor] = applicableRules;
                }
            }

            if (applicableDescriptorMappings == null)
            {
                return null;
            }

            var tagMappingResult = new TagHelperBinding(applicableDescriptorMappings);

            return tagMappingResult;
        }

        private bool MatchesRule(
            TagMatchingRule rule,
            IEnumerable<KeyValuePair<string, string>> tagAttributes,
            string tagNameWithoutPrefix,
            string parentTagName)
        {
            // Verify tag name
            if (rule.TagName != ElementCatchAllTarget &&
                rule.TagName != null &&
                !string.Equals(tagNameWithoutPrefix, rule.TagName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Verify parent tag
            if (rule.ParentTag != null && !string.Equals(parentTagName, rule.ParentTag, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!rule.Attributes.All(
                requiredAttribute => tagAttributes.Any(
                    attribute => requiredAttribute.IsMatch(attribute.Key, attribute.Value))))
            {
                return false;
            }

            return true;
        }

        private void Register(TagHelperDescriptor descriptor)
        {
            foreach (var rule in descriptor.TagMatchingRules)
            {
                var registrationKey =
                    string.Equals(rule.TagName, ElementCatchAllTarget, StringComparison.Ordinal) ?
                    ElementCatchAllTarget :
                    _tagHelperPrefix + rule.TagName;

                // Ensure there's a HashSet to add the descriptor to.
                if (!_registrations.TryGetValue(registrationKey, out HashSet<TagHelperDescriptor> descriptorSet))
                {
                    descriptorSet = new HashSet<TagHelperDescriptor>(TagHelperDescriptorComparer.Default);
                    _registrations[registrationKey] = descriptorSet;
                }

                descriptorSet.Add(descriptor);
            }
        }
    }
}
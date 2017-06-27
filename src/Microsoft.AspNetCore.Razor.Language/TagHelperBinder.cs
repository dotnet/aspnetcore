// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// Enables retrieval of <see cref="TagHelperBinding"/>'s.
    /// </summary>
    internal class TagHelperBinder
    {
        private IDictionary<string, HashSet<TagHelperDescriptor>> _registrations;
        private readonly string _tagHelperPrefix;

        /// <summary>
        /// Instantiates a new instance of the <see cref="TagHelperBinder"/>.
        /// </summary>
        /// <param name="tagHelperPrefix">The tag helper prefix being used by the document.</param>
        /// <param name="descriptors">The descriptors that the <see cref="TagHelperBinder"/> will pull from.</param>
        public TagHelperBinder(string tagHelperPrefix, IEnumerable<TagHelperDescriptor> descriptors)
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
        /// Gets all tag helpers that match the given HTML tag criteria.
        /// </summary>
        /// <param name="tagName">The name of the HTML tag to match. Providing a '*' tag name
        /// retrieves catch-all <see cref="TagHelperDescriptor"/>s (descriptors that target every tag).</param>
        /// <param name="attributes">Attributes on the HTML tag.</param>
        /// <param name="parentTagName">The parent tag name of the given <paramref name="tagName"/> tag.</param>
        /// <returns><see cref="TagHelperDescriptor"/>s that apply to the given HTML tag criteria.
        /// Will return <c>null</c> if no <see cref="TagHelperDescriptor"/>s are a match.</returns>
        public TagHelperBinding GetBinding(
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
            if (!_registrations.TryGetValue(TagHelperMatchingConventions.ElementCatchAllName, out HashSet<TagHelperDescriptor> catchAllDescriptors))
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
            Dictionary<TagHelperDescriptor, IEnumerable<TagMatchingRuleDescriptor>> applicableDescriptorMappings = null;
            foreach (var descriptor in descriptors)
            {
                var applicableRules = descriptor.TagMatchingRules.Where(
                    rule => TagHelperMatchingConventions.SatisfiesRule(tagNameWithoutPrefix, parentTagName, attributes, rule));

                if (applicableRules.Any())
                {
                    if (applicableDescriptorMappings == null)
                    {
                        applicableDescriptorMappings = new Dictionary<TagHelperDescriptor, IEnumerable<TagMatchingRuleDescriptor>>();
                    }

                    applicableDescriptorMappings[descriptor] = applicableRules;
                }
            }

            if (applicableDescriptorMappings == null)
            {
                return null;
            }

            var tagHelperBinding = new TagHelperBinding(
                tagName,
                attributes,
                parentTagName,
                applicableDescriptorMappings,
                _tagHelperPrefix);

            return tagHelperBinding;
        }

        private void Register(TagHelperDescriptor descriptor)
        {
            foreach (var rule in descriptor.TagMatchingRules)
            {
                var registrationKey =
                    string.Equals(rule.TagName, TagHelperMatchingConventions.ElementCatchAllName, StringComparison.Ordinal) ?
                    TagHelperMatchingConventions.ElementCatchAllName :
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
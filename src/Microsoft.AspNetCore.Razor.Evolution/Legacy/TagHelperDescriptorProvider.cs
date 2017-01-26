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
        private string _tagHelperPrefix;

        /// <summary>
        /// Instantiates a new instance of the <see cref="TagHelperDescriptorProvider"/>.
        /// </summary>
        /// <param name="descriptors">The descriptors that the <see cref="TagHelperDescriptorProvider"/> will pull from.</param>
        public TagHelperDescriptorProvider(IEnumerable<TagHelperDescriptor> descriptors)
        {
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
        public IEnumerable<TagHelperDescriptor> GetDescriptors(
            string tagName,
            IEnumerable<KeyValuePair<string, string>> attributes,
            string parentTagName)
        {
            if (!string.IsNullOrEmpty(_tagHelperPrefix) &&
                (tagName.Length <= _tagHelperPrefix.Length ||
                !tagName.StartsWith(_tagHelperPrefix, StringComparison.OrdinalIgnoreCase)))
            {
                // The tagName doesn't have the tag helper prefix, we can short circuit.
                return Enumerable.Empty<TagHelperDescriptor>();
            }

            HashSet<TagHelperDescriptor> catchAllDescriptors;
            IEnumerable<TagHelperDescriptor> descriptors;

            // Ensure there's a HashSet to use.
            if (!_registrations.TryGetValue(ElementCatchAllTarget, out catchAllDescriptors))
            {
                descriptors = new HashSet<TagHelperDescriptor>(TagHelperDescriptorComparer.Default);
            }
            else
            {
                descriptors = catchAllDescriptors;
            }

            // If we have a tag name associated with the requested name, we need to combine matchingDescriptors
            // with all the catch-all descriptors.
            HashSet<TagHelperDescriptor> matchingDescriptors;
            if (_registrations.TryGetValue(tagName, out matchingDescriptors))
            {
                descriptors = matchingDescriptors.Concat(descriptors);
            }

            var applicableDescriptors = new List<TagHelperDescriptor>();
            foreach (var descriptor in descriptors)
            {
                if (HasRequiredAttributes(descriptor, attributes) &&
                    HasRequiredParentTag(descriptor, parentTagName))
                {
                    applicableDescriptors.Add(descriptor);
                }
            }

            return applicableDescriptors.Distinct(TagHelperDescriptorComparer.TypeName);
        }

        private bool HasRequiredParentTag(
            TagHelperDescriptor descriptor,
            string parentTagName)
        {
            return descriptor.RequiredParent == null ||
                string.Equals(parentTagName, descriptor.RequiredParent, StringComparison.OrdinalIgnoreCase);
        }

        private bool HasRequiredAttributes(
            TagHelperDescriptor descriptor,
            IEnumerable<KeyValuePair<string, string>> attributes)
        {
            return descriptor.RequiredAttributes.All(
                requiredAttribute => attributes.Any(
                    attribute => requiredAttribute.IsMatch(attribute.Key, attribute.Value)));
        }

        private void Register(TagHelperDescriptor descriptor)
        {
            HashSet<TagHelperDescriptor> descriptorSet;

            if (_tagHelperPrefix == null)
            {
                _tagHelperPrefix = descriptor.Prefix;
            }

            var registrationKey =
                string.Equals(descriptor.TagName, ElementCatchAllTarget, StringComparison.Ordinal) ?
                ElementCatchAllTarget :
                descriptor.FullTagName;

            // Ensure there's a HashSet to add the descriptor to.
            if (!_registrations.TryGetValue(registrationKey, out descriptorSet))
            {
                descriptorSet = new HashSet<TagHelperDescriptor>(TagHelperDescriptorComparer.Default);
                _registrations[registrationKey] = descriptorSet;
            }

            descriptorSet.Add(descriptor);
        }
    }
}
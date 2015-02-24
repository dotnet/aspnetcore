// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// Enables retrieval of <see cref="TagHelperDescriptor"/>'s.
    /// </summary>
    public class TagHelperDescriptorProvider
    {
        private const string CatchAllDescriptorTarget = "*";

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
        /// <returns><see cref="TagHelperDescriptor"/>s that apply to the given <paramref name="tagName"/>.
        /// Will return an empty <see cref="Enumerable" /> if no <see cref="TagHelperDescriptor"/>s are
        /// found.</returns>
        public IEnumerable<TagHelperDescriptor> GetTagHelpers(string tagName)
        {
            HashSet<TagHelperDescriptor> descriptors;

            if (!string.IsNullOrEmpty(_tagHelperPrefix) &&
                (tagName.Length <= _tagHelperPrefix.Length ||
                !tagName.StartsWith(_tagHelperPrefix, StringComparison.OrdinalIgnoreCase)))
            {
                // The tagName doesn't have the tag helper prefix, we can short circuit.
                return Enumerable.Empty<TagHelperDescriptor>();
            }

            // Ensure there's a HashSet to use.
            if (!_registrations.TryGetValue(CatchAllDescriptorTarget, out descriptors))
            {
                descriptors = new HashSet<TagHelperDescriptor>(TagHelperDescriptorComparer.Default);
            }

            // If the requested tag name is the catch-all target, we should short circuit.
            if (tagName.Equals(CatchAllDescriptorTarget, StringComparison.OrdinalIgnoreCase))
            {
                return descriptors;
            }

            // If we have a tag name associated with the requested name, return the descriptors +
            // all of the catch-all descriptors.
            HashSet<TagHelperDescriptor> matchingDescriptors;
            if (_registrations.TryGetValue(tagName, out matchingDescriptors))
            {
                return matchingDescriptors.Concat(descriptors);
            }

            // We couldn't any descriptors associated with the requested tag name, return all
            // of the "catch-all" tag descriptors (there may not be any).
            return descriptors;
        }

        private void Register(TagHelperDescriptor descriptor)
        {
            HashSet<TagHelperDescriptor> descriptorSet;

            if (_tagHelperPrefix == null)
            {
                _tagHelperPrefix = descriptor.Prefix;
            }

            var registrationKey =
                string.Equals(descriptor.TagName, CatchAllDescriptorTarget, StringComparison.Ordinal) ?
                CatchAllDescriptorTarget :
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
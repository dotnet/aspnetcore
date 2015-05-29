// Copyright (c) .NET Foundation. All rights reserved.
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
        public const string ElementCatchAllTarget = "*";

        public static readonly string RequiredAttributeWildcardSuffix = "*";

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
        /// <param name="attributeNames">Attributes the HTML element must contain to match.</param>
        /// <returns><see cref="TagHelperDescriptor"/>s that apply to the given <paramref name="tagName"/>.
        /// Will return an empty <see cref="Enumerable" /> if no <see cref="TagHelperDescriptor"/>s are
        /// found.</returns>
        public IEnumerable<TagHelperDescriptor> GetDescriptors(string tagName, IEnumerable<string> attributeNames)
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

            var applicableDescriptors = ApplyRequiredAttributes(descriptors, attributeNames);

            return applicableDescriptors;
        }

        private IEnumerable<TagHelperDescriptor> ApplyRequiredAttributes(
            IEnumerable<TagHelperDescriptor> descriptors,
            IEnumerable<string> attributeNames)
        {
            return descriptors.Where(
                descriptor =>
                {
                    foreach (var requiredAttribute in descriptor.RequiredAttributes)
                    {
                        // '*' at the end of a required attribute indicates: apply to attributes prefixed with the
                        // required attribute value.
                        if (requiredAttribute.EndsWith(
                            RequiredAttributeWildcardSuffix,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            var prefix = requiredAttribute.Substring(0, requiredAttribute.Length - 1);

                            if (!attributeNames.Any(
                                attributeName =>
                                    attributeName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                                    !string.Equals(attributeName, prefix, StringComparison.OrdinalIgnoreCase)))
                            {
                                return false;
                            }
                        }
                        else if (!attributeNames.Contains(requiredAttribute, StringComparer.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }

                    return true;
                });
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
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [Export(typeof(TagHelperFactsService))]
    internal class DefaultTagHelperFactsService : TagHelperFactsService
    {
        public override TagHelperBinding GetTagHelperBinding(
            TagHelperDocumentContext documentContext,
            string tagName,
            IEnumerable<KeyValuePair<string, string>> attributes,
            string parentTag)
        {
            if (documentContext == null)
            {
                throw new ArgumentNullException(nameof(documentContext));
            }

            if (tagName == null)
            {
                throw new ArgumentNullException(nameof(tagName));
            }

            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            var descriptors = documentContext.TagHelpers;
            if (descriptors == null || descriptors.Count == 0)
            {
                return null;
            }

            var prefix = documentContext.Prefix;
            var tagHelperBinder = new TagHelperBinder(prefix, descriptors);
            var binding = tagHelperBinder.GetBinding(tagName, attributes, parentTag);

            return binding;
        }

        public override IEnumerable<BoundAttributeDescriptor> GetBoundTagHelperAttributes(
            TagHelperDocumentContext documentContext,
            string attributeName,
            TagHelperBinding binding)
        {
            if (documentContext == null)
            {
                throw new ArgumentNullException(nameof(documentContext));
            }

            if (attributeName == null)
            {
                throw new ArgumentNullException(nameof(attributeName));
            }

            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            var matchingBoundAttributes = new List<BoundAttributeDescriptor>();
            foreach (var descriptor in binding.Descriptors)
            {
                foreach (var boundAttributeDescriptor in descriptor.BoundAttributes)
                {
                    if (TagHelperMatchingConventions.CanSatisfyBoundAttribute(attributeName, boundAttributeDescriptor))
                    {
                        matchingBoundAttributes.Add(boundAttributeDescriptor);

                        // Only one bound attribute can match an attribute
                        break;
                    }
                }
            }

            return matchingBoundAttributes;
        }

        public override IReadOnlyList<TagHelperDescriptor> GetTagHelpersGivenTag(
            TagHelperDocumentContext documentContext,
            string tagName,
            string parentTag)
        {
            if (documentContext == null)
            {
                throw new ArgumentNullException(nameof(documentContext));
            }

            if (tagName == null)
            {
                throw new ArgumentNullException(nameof(tagName));
            }

            var matchingDescriptors = new List<TagHelperDescriptor>();
            var descriptors = documentContext?.TagHelpers;
            if (descriptors?.Count == 0)
            {
                return matchingDescriptors;
            }

            var prefix = documentContext.Prefix ?? string.Empty;
            if (!tagName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                // Can't possibly match TagHelpers, it doesn't start with the TagHelperPrefix.
                return matchingDescriptors;
            }

            var tagNameWithoutPrefix = tagName.Substring(prefix.Length);
            for (var i = 0; i < descriptors.Count; i++)
            {
                var descriptor = descriptors[i];
                foreach (var rule in descriptor.TagMatchingRules)
                {
                    if (TagHelperMatchingConventions.SatisfiesTagName(tagNameWithoutPrefix, rule) &&
                        TagHelperMatchingConventions.SatisfiesParentTag(parentTag, rule))
                    {
                        matchingDescriptors.Add(descriptor);
                        break;
                    }
                }
            }

            return matchingDescriptors;
        }

        public override IReadOnlyList<TagHelperDescriptor> GetTagHelpersGivenParent(TagHelperDocumentContext documentContext, string parentTag)
        {
            if (documentContext == null)
            {
                throw new ArgumentNullException(nameof(documentContext));
            }

            var matchingDescriptors = new List<TagHelperDescriptor>();
            var descriptors = documentContext?.TagHelpers;
            if (descriptors?.Count == 0)
            {
                return matchingDescriptors;
            }

            for (var i = 0; i < descriptors.Count; i++)
            {
                var descriptor = descriptors[i];
                foreach (var rule in descriptor.TagMatchingRules)
                {
                    if (TagHelperMatchingConventions.SatisfiesParentTag(parentTag, rule))
                    {
                        matchingDescriptors.Add(descriptor);
                        break;
                    }
                }
            }

            return matchingDescriptors;
        }
    }
}

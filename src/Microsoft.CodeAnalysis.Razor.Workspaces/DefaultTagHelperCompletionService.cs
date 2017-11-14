// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class DefaultTagHelperCompletionService : TagHelperCompletionService
    {
        private readonly TagHelperFactsServiceInternal _tagHelperFactsService;
        private static readonly HashSet<TagHelperDescriptor> _emptyHashSet = new HashSet<TagHelperDescriptor>();

        public DefaultTagHelperCompletionService(TagHelperFactsServiceInternal tagHelperFactsService)
        {
            _tagHelperFactsService = tagHelperFactsService;
        }

        /*
         * This API attempts to understand a users context as they're typing in a Razor file to provide TagHelper based attribute IntelliSense.
         * 
         * Scenarios for TagHelper attribute IntelliSense follows:
         * 1. TagHelperDescriptor's have matching required attribute names
         *  -> Provide IntelliSense for the required attributes of those descriptors to lead users towards a TagHelperified element.
         * 2. TagHelperDescriptor entirely applies to current element. Tag name, attributes, everything is fulfilled.
         *  -> Provide IntelliSense for the bound attributes for the applied descriptors.
         *  
         *  Within each of the above scenarios if an attribute completion has a corresponding bound attribute we associate it with the corresponding
         *  BoundAttributeDescriptor. By doing this a user can see what C# type a TagHelper expects for the attribute.
         */
        public override AttributeCompletionResult GetAttributeCompletions(AttributeCompletionContext completionContext)
        {
            if (completionContext == null)
            {
                throw new ArgumentNullException(nameof(completionContext));
            }

            var attributeCompletions = completionContext.ExistingCompletions.ToDictionary(
                completion => completion,
                _ => new HashSet<BoundAttributeDescriptor>(),
                StringComparer.OrdinalIgnoreCase);

            var documentContext = completionContext.DocumentContext;
            var descriptorsForTag = _tagHelperFactsService.GetTagHelpersGivenTag(documentContext, completionContext.CurrentTagName, completionContext.CurrentParentTagName);
            if (descriptorsForTag.Count == 0)
            {
                // If the current tag has no possible descriptors then we can't have any additional attributes.
                var defaultResult = AttributeCompletionResult.Create(attributeCompletions);
                return defaultResult;
            }

            var prefix = documentContext.Prefix ?? string.Empty;
            Debug.Assert(completionContext.CurrentTagName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            var applicableTagHelperBinding = _tagHelperFactsService.GetTagHelperBinding(
                documentContext,
                completionContext.CurrentTagName,
                completionContext.Attributes,
                completionContext.CurrentParentTagName,
                completionContext.CurrentParentIsTagHelper);

            var applicableDescriptors = applicableTagHelperBinding?.Descriptors ?? Enumerable.Empty<TagHelperDescriptor>();
            var unprefixedTagName = completionContext.CurrentTagName.Substring(prefix.Length);

            if (!completionContext.InHTMLSchema(unprefixedTagName) &&
                applicableDescriptors.All(descriptor => descriptor.TagOutputHint == null))
            {
                // This isn't a known HTML tag and no descriptor has an output element hint. Remove all previous completions.
                attributeCompletions.Clear();
            }

            for (var i = 0; i < descriptorsForTag.Count; i++)
            {
                var descriptor = descriptorsForTag[i];

                if (applicableDescriptors.Contains(descriptor))
                {
                    foreach (var attributeDescriptor in descriptor.BoundAttributes)
                    {
                        UpdateCompletions(attributeDescriptor.Name, attributeDescriptor);
                    }
                }
                else
                {
                    var htmlNameToBoundAttribute = descriptor.BoundAttributes.ToDictionary(attribute => attribute.Name, StringComparer.OrdinalIgnoreCase);

                    foreach (var rule in descriptor.TagMatchingRules)
                    {
                        foreach (var requiredAttribute in rule.Attributes)
                        {
                            if (htmlNameToBoundAttribute.TryGetValue(requiredAttribute.Name, out var attributeDescriptor))
                            {
                                UpdateCompletions(requiredAttribute.Name, attributeDescriptor);
                            }
                            else
                            {
                                UpdateCompletions(requiredAttribute.Name, possibleDescriptor: null);
                            }
                        }
                    }
                }
            }

            var completionResult = AttributeCompletionResult.Create(attributeCompletions);
            return completionResult;

            void UpdateCompletions(string attributeName, BoundAttributeDescriptor possibleDescriptor)
            {
                if (completionContext.Attributes.Any(attribute => string.Equals(attribute.Key, attributeName, StringComparison.OrdinalIgnoreCase)) &&
                    (completionContext.CurrentAttributeName == null ||
                    !string.Equals(attributeName, completionContext.CurrentAttributeName, StringComparison.OrdinalIgnoreCase)))
                {
                    // Attribute is already present on this element and it is not the attribute in focus.
                    // It shouldn't exist in the completion list.
                    return;
                }

                if (!attributeCompletions.TryGetValue(attributeName, out var rules))
                {
                    rules = new HashSet<BoundAttributeDescriptor>();
                    attributeCompletions[attributeName] = rules;
                }

                if (possibleDescriptor != null)
                {
                    rules.Add(possibleDescriptor);
                }
            }
        }

        public override ElementCompletionResult GetElementCompletions(ElementCompletionContext completionContext)
        {
            if (completionContext == null)
            {
                throw new ArgumentNullException(nameof(completionContext));
            }

            var elementCompletions = new Dictionary<string, HashSet<TagHelperDescriptor>>(StringComparer.OrdinalIgnoreCase);

            AddAllowedChildrenCompletions(completionContext, elementCompletions);

            if (elementCompletions.Count > 0)
            {
                // If the containing element is already a TagHelper and only allows certain children.
                var emptyResult = ElementCompletionResult.Create(elementCompletions);
                return emptyResult;
            }

            elementCompletions = completionContext.ExistingCompletions.ToDictionary(
                completion => completion,
                _ => new HashSet<TagHelperDescriptor>(),
                StringComparer.OrdinalIgnoreCase);

            var catchAllDescriptors = new HashSet<TagHelperDescriptor>();
            var prefix = completionContext.DocumentContext.Prefix ?? string.Empty;
            var possibleChildDescriptors = _tagHelperFactsService.GetTagHelpersGivenParent(completionContext.DocumentContext, completionContext.ContainingTagName);
            foreach (var possibleDescriptor in possibleChildDescriptors)
            {
                var addRuleCompletions = false;
                var outputHint = possibleDescriptor.TagOutputHint;

                foreach (var rule in possibleDescriptor.TagMatchingRules)
                {
                    if (rule.TagName == TagHelperMatchingConventions.ElementCatchAllName)
                    {
                        catchAllDescriptors.Add(possibleDescriptor);
                    }
                    else if (elementCompletions.ContainsKey(rule.TagName))
                    {
                        addRuleCompletions = true;
                    }
                    else if (outputHint != null)
                    {
                        // If the current descriptor has an output hint we need to make sure it shows up only when its output hint would normally show up.
                        // Example: We have a MyTableTagHelper that has an output hint of "table" and a MyTrTagHelper that has an output hint of "tr".
                        // If we try typing in a situation like this: <body > | </body>
                        // We'd expect to only get "my-table" as a completion because the "body" tag doesn't allow "tr" tags.
                        addRuleCompletions = elementCompletions.ContainsKey(outputHint);
                    }
                    else if (!completionContext.InHTMLSchema(rule.TagName))
                    {
                        // If there is an unknown HTML schema tag that doesn't exist in the current completion we should add it. This happens for
                        // TagHelpers that target non-schema oriented tags.
                        addRuleCompletions = true;
                    }

                    if (addRuleCompletions)
                    {
                        UpdateCompletions(prefix + rule.TagName, possibleDescriptor);
                    }
                }
            }

            // We needed to track all catch-alls and update their completions after all other completions have been completed.
            // This way, any TagHelper added completions will also have catch-alls listed under their entries.
            foreach (var catchAllDescriptor in catchAllDescriptors)
            {
                foreach (var completionTagName in elementCompletions.Keys)
                {
                    if (elementCompletions[completionTagName].Count > 0 || 
                        !string.IsNullOrEmpty(prefix) && completionTagName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        // The current completion either has other TagHelper's associated with it or is prefixed with a non-empty
                        // TagHelper prefix.
                        UpdateCompletions(completionTagName, catchAllDescriptor);
                    }
                }
            }

            var result = ElementCompletionResult.Create(elementCompletions);
            return result;

            void UpdateCompletions(string tagName, TagHelperDescriptor possibleDescriptor)
            {
                if (!elementCompletions.TryGetValue(tagName, out var existingRuleDescriptors))
                {
                    existingRuleDescriptors = new HashSet<TagHelperDescriptor>();
                    elementCompletions[tagName] = existingRuleDescriptors;
                }

                existingRuleDescriptors.Add(possibleDescriptor);
            }
        }

        private void AddAllowedChildrenCompletions(
            ElementCompletionContext completionContext,
            Dictionary<string, HashSet<TagHelperDescriptor>> elementCompletions)
        {
            if (completionContext.ContainingTagName == null)
            {
                // If we're at the root then there's no containing TagHelper to specify allowed children.
                return;
            }

            var prefix = completionContext.DocumentContext.Prefix ?? string.Empty;

            var binding = _tagHelperFactsService.GetTagHelperBinding(
                completionContext.DocumentContext,
                completionContext.ContainingTagName,
                completionContext.Attributes,
                completionContext.ContainingParentTagName,
                completionContext.ContainingParentIsTagHelper);

            if (binding == null)
            {
                // Containing tag is not a TagHelper; therefore, it allows any children.
                return;
            }

            foreach (var descriptor in binding.Descriptors)
            {
                foreach (var childTag in descriptor.AllowedChildTags)
                {
                    var prefixedName = string.Concat(prefix, childTag.Name);
                    var descriptors = _tagHelperFactsService.GetTagHelpersGivenTag(
                        completionContext.DocumentContext,
                        prefixedName,
                        completionContext.ContainingTagName);

                    if (descriptors.Count == 0)
                    {
                        if (!elementCompletions.ContainsKey(prefixedName))
                        {
                            elementCompletions[prefixedName] = _emptyHashSet;
                        }

                        continue;
                    }

                    if (!elementCompletions.TryGetValue(prefixedName, out var existingRuleDescriptors))
                    {
                        existingRuleDescriptors = new HashSet<TagHelperDescriptor>();
                        elementCompletions[prefixedName] = existingRuleDescriptors;
                    }

                    existingRuleDescriptors.UnionWith(descriptors);
                }
            }
        }
    }
}

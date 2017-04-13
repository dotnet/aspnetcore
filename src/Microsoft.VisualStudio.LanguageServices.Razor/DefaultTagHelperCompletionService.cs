// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [Export(typeof(TagHelperCompletionService))]
    internal class DefaultTagHelperCompletionService : TagHelperCompletionService
    {
        private readonly TagHelperFactsService _tagHelperFactsService;
        private static readonly HashSet<TagHelperDescriptor> _emptyHashSet = new HashSet<TagHelperDescriptor>();

        [ImportingConstructor]
        public DefaultTagHelperCompletionService(TagHelperFactsService tagHelperFactsService)
        {
            _tagHelperFactsService = tagHelperFactsService;
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
                    else if (outputHint != null && elementCompletions.ContainsKey(outputHint))
                    {
                        // If the possible descriptors final output tag already exists in our list of completions, we should add every representation
                        // of that descriptor to the possible element completions.
                        addRuleCompletions = true;
                    }
                    else if (!completionContext.InHTMLSchema(rule.TagName))
                    {
                        // If there is an unknown HTML schema tag that doesn't exist in the current completion we should add it. This happens for
                        // TagHelpers that target non-schema oriented tags.
                        addRuleCompletions = true;
                    }

                    if (addRuleCompletions)
                    {
                        UpdateCompletions(rule.TagName, possibleDescriptor);
                    }
                }
            }

            // We needed to track all catch-alls and update their completions after all other completions have been completed.
            // This way, any TagHelper added completions will also have catch-alls listed under their entries.
            foreach (var catchAllDescriptor in catchAllDescriptors)
            {
                foreach (var completionTagNames in elementCompletions.Keys)
                {
                    UpdateCompletions(completionTagNames, catchAllDescriptor);
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
                completionContext.ContainingParentTagName);

            if (binding == null)
            {
                // Containing tag is not a TagHelper; therefore, it allows any children.
                return;
            }

            foreach (var descriptor in binding.Descriptors)
            {
                if (descriptor.AllowedChildTags == null)
                {
                    continue;
                }

                foreach (var childTag in descriptor.AllowedChildTags)
                {
                    var prefixedName = string.Concat(prefix, childTag);
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

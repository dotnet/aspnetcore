// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class TagHelperBinding
{
    internal TagHelperBinding(
        string tagName,
        IReadOnlyList<KeyValuePair<string, string>> attributes,
        string parentTagName,
        IReadOnlyDictionary<TagHelperDescriptor, IReadOnlyList<TagMatchingRuleDescriptor>> mappings,
        string tagHelperPrefix)
    {
        TagName = tagName;
        Attributes = attributes;
        ParentTagName = parentTagName;
        Mappings = mappings;
        TagHelperPrefix = tagHelperPrefix;

    }

    public IEnumerable<TagHelperDescriptor> Descriptors => Mappings.Keys;

    /// <summary>
    /// Gets a value that indicates whether the the binding matched on attributes only.
    /// </summary>
    /// <returns><c>false</c> if the entire element should be classified as a tag helper.</returns>
    /// <remarks>
    /// If this returns <c>true</c>, use <c>TagHelperFactsService.GetBoundTagHelperAttributes</c> to find the
    /// set of attributes that should be considered part of the match.
    /// </remarks>
    public bool IsAttributeMatch
    {
        get
        {
            foreach (var descriptor in Mappings.Keys)
            {
                if (!descriptor.Metadata.TryGetValue(TagHelperMetadata.Common.ClassifyAttributesOnly, out var value) ||
                    !string.Equals(value, bool.TrueString, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // All the matching tag helpers want to be classified with **attributes only**.
            //
            // Ex: (components)
            //
            //      <button onclick="..." />
            return true;
        }
    }

    public string TagName { get; }

    public string ParentTagName { get; }

    public IReadOnlyList<KeyValuePair<string, string>> Attributes { get; }

    public IReadOnlyDictionary<TagHelperDescriptor, IReadOnlyList<TagMatchingRuleDescriptor>> Mappings { get; }

    public string TagHelperPrefix { get; }

    public IReadOnlyList<TagMatchingRuleDescriptor> GetBoundRules(TagHelperDescriptor descriptor)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return Mappings[descriptor];
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class TagHelperMatchingConventions
{
    public const string ElementCatchAllName = "*";

    public const char ElementOptOutCharacter = '!';

    public static bool SatisfiesRule(
        StringSegment tagNameWithoutPrefix,
        StringSegment parentTagNameWithoutPrefix,
        IReadOnlyList<KeyValuePair<string, string>> tagAttributes,
        TagMatchingRuleDescriptor rule)
    {
        var satisfiesTagName = SatisfiesTagName(tagNameWithoutPrefix, rule);
        if (!satisfiesTagName)
        {
            return false;
        }

        var satisfiesParentTag = SatisfiesParentTag(parentTagNameWithoutPrefix, rule);
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

    public static bool SatisfiesTagName(StringSegment tagNameWithoutPrefix, TagMatchingRuleDescriptor rule)
    {
        if (StringSegment.IsNullOrEmpty(tagNameWithoutPrefix))
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
            !tagNameWithoutPrefix.Equals(rule.TagName, rule.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    public static bool SatisfiesParentTag(StringSegment parentTagNameWithoutPrefix, TagMatchingRuleDescriptor rule)
    {
        if (rule.ParentTag != null &&
            !parentTagNameWithoutPrefix.Equals(rule.ParentTag, rule.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    public static bool SatisfiesAttributes(IReadOnlyList<KeyValuePair<string, string>> tagAttributes, TagMatchingRuleDescriptor rule)
    {
        if (!rule.Attributes.All(
            static (requiredAttribute, tagAttributes) => tagAttributes.Any(
                static (attribute, requiredAttribute) => SatisfiesRequiredAttribute(attribute.Key, attribute.Value, requiredAttribute),
                requiredAttribute),
            tagAttributes))
        {
            return false;
        }

        return true;
    }

    public static bool CanSatisfyBoundAttribute(string name, BoundAttributeDescriptor descriptor)
    {
        return SatisfiesBoundAttributeName(name, descriptor) ||
            SatisfiesBoundAttributeIndexer(name, descriptor) ||
            GetSatifyingBoundAttributeWithParameter(name, descriptor, descriptor.BoundAttributeParameters) is not null;
    }

    private static BoundAttributeParameterDescriptor GetSatifyingBoundAttributeWithParameter(string name, BoundAttributeDescriptor descriptor, IReadOnlyList<BoundAttributeParameterDescriptor> boundAttributeParameters)
    {
        var count = boundAttributeParameters.Count;
        for (var i = 0; i < count; i++)
        {
            if (SatisfiesBoundAttributeWithParameter(name, descriptor, boundAttributeParameters[i]))
            {
                return boundAttributeParameters[i];
            }
        }

        return null;
    }

    public static bool SatisfiesBoundAttributeIndexer(StringSegment name, BoundAttributeDescriptor descriptor)
    {
        return descriptor.IndexerNamePrefix != null &&
            !SatisfiesBoundAttributeName(name, descriptor) &&
            name.StartsWith(descriptor.IndexerNamePrefix, descriptor.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
    }

    public static bool SatisfiesBoundAttributeWithParameter(string name, BoundAttributeDescriptor parent, BoundAttributeParameterDescriptor descriptor)
    {
        if (TryGetBoundAttributeParameter(name, out var attributeName, out var parameterName))
        {
            var satisfiesBoundAttributeName = SatisfiesBoundAttributeName(attributeName, parent);
            var satisfiesBoundAttributeIndexer = SatisfiesBoundAttributeIndexer(attributeName, parent);
            var matchesParameter = parameterName.Equals(descriptor.Name, descriptor.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            return (satisfiesBoundAttributeName || satisfiesBoundAttributeIndexer) && matchesParameter;
        }

        return false;
    }

    public static bool TryGetBoundAttributeParameter(string fullAttributeName, out StringSegment boundAttributeName)
    {
        boundAttributeName = default;

        if (string.IsNullOrEmpty(fullAttributeName))
        {
            return false;
        }

        var index = fullAttributeName.IndexOf(':');
        if (index == -1)
        {
            return false;
        }

        boundAttributeName = new StringSegment(fullAttributeName, 0, index);
        return true;
    }

    public static bool TryGetBoundAttributeParameter(string fullAttributeName, out StringSegment boundAttributeName, out StringSegment parameterName)
    {
        boundAttributeName = default;
        parameterName = default;

        if (string.IsNullOrEmpty(fullAttributeName))
        {
            return false;
        }

        var index = fullAttributeName.IndexOf(':');
        if (index == -1)
        {
            return false;
        }

        boundAttributeName = new StringSegment(fullAttributeName, 0, index);
        parameterName = new StringSegment(fullAttributeName, index + 1);
        return true;
    }

    public static bool TryGetFirstBoundAttributeMatch(
        string name,
        TagHelperDescriptor descriptor,
        out BoundAttributeDescriptor boundAttribute,
        out bool indexerMatch,
        out bool parameterMatch,
        out BoundAttributeParameterDescriptor boundAttributeParameter)
    {
        indexerMatch = false;
        parameterMatch = false;
        boundAttribute = null;
        boundAttributeParameter = null;

        if (string.IsNullOrEmpty(name) || descriptor == null)
        {
            return false;
        }

        // First, check if we have a bound attribute descriptor that matches the parameter if it exists.
        foreach (var attribute in descriptor.BoundAttributes)
        {
            boundAttributeParameter = GetSatifyingBoundAttributeWithParameter(name, attribute, attribute.BoundAttributeParameters);

            if (boundAttributeParameter != null)
            {
                boundAttribute = attribute;
                indexerMatch = SatisfiesBoundAttributeIndexer(name, attribute);
                parameterMatch = true;
                return true;
            }
        }

        // If we reach here, either the attribute name doesn't contain a parameter portion or
        // the specified parameter isn't supported by any of the BoundAttributeDescriptors.
        foreach (var attribute in descriptor.BoundAttributes)
        {
            if (CanSatisfyBoundAttribute(name, attribute))
            {
                boundAttribute = attribute;
                indexerMatch = SatisfiesBoundAttributeIndexer(name, attribute);
                return true;
            }
        }

        // No matches found.
        return false;
    }

    private static bool SatisfiesBoundAttributeName(StringSegment name, BoundAttributeDescriptor descriptor)
    {
        return name.Equals(descriptor.Name, descriptor.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
    }

    // Internal for testing
    internal static bool SatisfiesRequiredAttribute(string attributeName, string attributeValue, RequiredAttributeDescriptor descriptor)
    {
        var nameMatches = false;
        if (descriptor.NameComparison == RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
        {
            nameMatches = string.Equals(descriptor.Name, attributeName, descriptor.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
        }
        else if (descriptor.NameComparison == RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch)
        {
            // attributeName cannot equal the Name if comparing as a PrefixMatch.
            nameMatches = attributeName.Length != descriptor.Name.Length &&
                attributeName.StartsWith(descriptor.Name, descriptor.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
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

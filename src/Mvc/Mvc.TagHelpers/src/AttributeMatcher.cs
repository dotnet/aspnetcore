// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// Methods for determining how an <see cref="ITagHelper"/> should run based on the attributes that were specified.
/// </summary>
internal static class AttributeMatcher
{
    /// <summary>
    /// Determines the most effective mode a <see cref="ITagHelper" /> can run in based on which modes have
    /// all their required attributes present.
    /// </summary>
    /// <typeparam name="TMode">The type representing the <see cref="ITagHelper" />'s modes.</typeparam>
    /// <param name="context">The <see cref="TagHelperContext"/>.</param>
    /// <param name="modeInfos">The modes and their required attributes.</param>
    /// <param name="compare">A comparer delegate.</param>
    /// <param name="result">The resulting most effective mode.</param>
    /// <returns><c>true</c> if a mode was determined, otherwise <c>false</c>.</returns>
    public static bool TryDetermineMode<TMode>(
        TagHelperContext context,
        ModeAttributes<TMode>[] modeInfos,
        Func<TMode, TMode, int> compare,
        out TMode result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(modeInfos);
        ArgumentNullException.ThrowIfNull(compare);

        var foundResult = false;
        result = default;

        // Perf: Avoid allocating enumerator
        var allAttributes = context.AllAttributes;
        // Read interface .Count once rather than per iteration
        var allAttributesCount = allAttributes.Count;
        foreach (var modeInfo in modeInfos)
        {
            var requiredAttributes = modeInfo.Attributes;
            // If there are fewer attributes present than required, one or more of them must be missing.
            if (allAttributesCount >= requiredAttributes.Length &&
                !HasMissingAttributes(allAttributes, requiredAttributes) &&
                compare(result, modeInfo.Mode) <= 0)
            {
                foundResult = true;
                result = modeInfo.Mode;
            }
        }

        return foundResult;
    }

    private static bool HasMissingAttributes(ReadOnlyTagHelperAttributeList allAttributes, string[] requiredAttributes)
    {
        // Check for all attribute values
        // Perf: Avoid allocating enumerator
        for (var i = 0; i < requiredAttributes.Length; i++)
        {
            if (!allAttributes.TryGetAttribute(requiredAttributes[i], out var attribute))
            {
                // Missing attribute.
                return true;
            }

            if (attribute.Value is string valueAsString && string.IsNullOrEmpty(valueAsString))
            {
                // Treat attributes with empty values as missing.
                return true;
            }
        }

        return false;
    }
}

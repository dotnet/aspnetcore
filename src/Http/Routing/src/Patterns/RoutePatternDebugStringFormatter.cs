// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Routing.Patterns;

internal static class RoutePatternDebugStringFormatter
{
    private const char Separator = '/';
    private const string SeparatorString = "/";

    public static string Format(RoutePattern pattern)
    {
        // If there are no required values that match parameters, use the simple approach
        if (pattern.RawText is { Length: > 0 } rawText && !HasMatchingRequiredValues(pattern))
        {
            return rawText;
        }

        // Build the string replacing parameters with their required values when available
        var segments = new string[pattern.PathSegments.Count];
        for (var i = 0; i < pattern.PathSegments.Count; i++)
        {
            var segment = pattern.PathSegments[i];
            var segmentString = GetSegmentDebuggerToString(pattern, segment);
            segments[i] = segmentString;
        }

        var result = string.Join(Separator, segments);

        // Preserve leading slash from raw text
        if (pattern.RawText is { Length: > 0 } rt && rt[0] == Separator)
        {
            result = Separator + result;
        }

        // Return "/" for empty results
        if (result.Length == 0)
        {
            return SeparatorString;
        }

        return result;
    }

    private static bool HasMatchingRequiredValues(RoutePattern pattern)
    {
        if (pattern.RequiredValues.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < pattern.Parameters.Count; i++)
        {
            if (TryGetRequiredValue(pattern, pattern.Parameters[i].Name, out _))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetSegmentDebuggerToString(RoutePattern pattern, RoutePatternPathSegment segment)
    {
        // Simple segment with single parameter that has a required value - just return the required value
        if (segment.IsSimple && segment.Parts[0] is RoutePatternParameterPart parameter)
        {
            if (TryGetRequiredValue(pattern, parameter.Name, out var requiredValue))
            {
                return requiredValue;
            }
        }

        // For complex segments, build the string part by part
        var parts = new string[segment.Parts.Count];
        for (var i = 0; i < segment.Parts.Count; i++)
        {
            var part = segment.Parts[i];
            parts[i] = part is RoutePatternParameterPart paramPart && TryGetRequiredValue(pattern, paramPart.Name, out var value)
                ? value
                : part.DebuggerToString();
        }

        return string.Join(string.Empty, parts);
    }

    private static bool TryGetRequiredValue(RoutePattern pattern, string parameterName, [NotNullWhen(true)] out string? value)
    {
        if (pattern.RequiredValues.TryGetValue(parameterName, out var requiredValue) &&
            requiredValue is not null &&
            !RoutePattern.IsRequiredValueAny(requiredValue) &&
            requiredValue.ToString() is { Length: > 0 } v)
        {
            value = v;
            return true;
        }

        value = null;
        return false;
    }
}

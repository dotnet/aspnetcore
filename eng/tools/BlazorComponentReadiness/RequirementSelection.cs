// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorComponentReadiness;

internal static class RequirementSelection
{
    internal static IReadOnlyList<string> CanonicalOverlayNames(
        IEnumerable<string> overlays)
    {
        return overlays.Order(StringComparer.Ordinal).ToArray();
    }

    internal static IReadOnlyList<string> OverlayNames(
        IEnumerable<string> requirementIdentifiers,
        SkillLayout layout)
    {
        var selected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var identifier in requirementIdentifiers)
        {
            foreach (var overlay in layout.OverlayPrefixes)
            {
                if (identifier.StartsWith(
                    overlay.Value + "-",
                    StringComparison.Ordinal))
                {
                    selected.Add(overlay.Key);
                }
            }
        }

        return CanonicalOverlayNames(selected);
    }
}

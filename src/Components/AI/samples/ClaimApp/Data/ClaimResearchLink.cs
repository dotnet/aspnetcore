// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ComponentsAIClaimApp.Data;

internal static class ClaimResearchLink
{
    internal static string? Normalize(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") ||
            string.IsNullOrEmpty(uri.Host))
        {
            return null;
        }

        return uri.AbsoluteUri;
    }

    internal static void Sanitize(ClaimDamageAnalysis analysis)
    {
        foreach (var part in analysis.ReplacementParts)
        {
            part.SourceUrl = Normalize(part.SourceUrl) ?? string.Empty;
        }

        analysis.ResearchSources = analysis.ResearchSources
            .Select(source => new ClaimResearchSource
            {
                Title = source.Title,
                Url = Normalize(source.Url) ?? string.Empty,
            })
            .Where(source => source.Url.Length > 0)
            .ToList();
    }
}

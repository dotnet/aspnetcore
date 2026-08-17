// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace BlazorComponentReadiness;

internal static class ValidationProvenance
{
    internal static IReadOnlyDictionary<string, ReadOnlyMemory<byte>>
        ReadOverlaySnapshots(
            SkillLayout layout,
            IEnumerable<string> overlayNames)
    {
        return overlayNames
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(
                name => name,
                name =>
                {
                    if (!layout.OverlayPaths.TryGetValue(name, out var path))
                    {
                        throw new InvalidDataException(
                            $"Unknown overlay '{name}'.");
                    }

                    return (ReadOnlyMemory<byte>)File.ReadAllBytes(path);
                },
                StringComparer.Ordinal);
    }

    internal static ValidationInputManifest BuildManifest(
        RubricSnapshot rubric,
        IEnumerable<string> selectedOverlays,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> overlaySnapshots,
        ReportSnapshot? sharedRowProjectionSnapshot = null)
    {
        var files = new List<ValidationInput>
        {
            new(
                "references/checklist.md",
                new Sha256Digest("sha256", rubric.Sha256)),
        };
        foreach (var overlay in selectedOverlays
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal))
        {
            if (!overlaySnapshots.TryGetValue(overlay, out var bytes))
            {
                throw new InvalidDataException(
                    $"Missing validation snapshot for overlay '{overlay}'.");
            }

            files.Add(new ValidationInput(
                OverlayRelativePath(overlay),
                new Sha256Digest(
                    "sha256",
                    CanonicalEvidenceJson.ComputeSha256(bytes.Span))));
        }

        if (sharedRowProjectionSnapshot is not null)
        {
            files.Add(new ValidationInput(
                "shared-row-projection.json",
                new Sha256Digest(
                    "sha256",
                    CanonicalEvidenceJson.ComputeSha256(
                        sharedRowProjectionSnapshot.Bytes.Span))));
        }

        return new ValidationInputManifest(
            CanonicalEvidenceJson.EvidenceSchemaVersion,
            files.OrderBy(file => file.Path, StringComparer.Ordinal).ToArray());
    }

    internal static string ComputeValidatorSha256()
    {
        var location = Assembly.GetExecutingAssembly().Location;
        if (string.IsNullOrWhiteSpace(location) || !File.Exists(location))
        {
            throw new InvalidDataException(
                "RECEIPT003: running validator assembly bytes are unavailable.");
        }

        using var stream = new FileStream(
            location,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        return Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(stream));
    }

    internal static string OverlayRelativePath(string overlay)
    {
        return overlay switch
        {
            "scaffolder" => "references/overlays/scaffolder.md",
            "ai-skill" => "references/overlays/ai-skill.md",
            _ => throw new InvalidDataException($"Unknown overlay '{overlay}'."),
        };
    }

    internal static string OverlayNameFromRelativePath(string path)
    {
        return path switch
        {
            "references/overlays/scaffolder.md" => "scaffolder",
            "references/overlays/ai-skill.md" => "ai-skill",
            _ => throw new InvalidDataException(
                $"RECEIPT005: unknown validation input path '{path}'."),
        };
    }
}
